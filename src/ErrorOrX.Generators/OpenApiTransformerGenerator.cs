using System.Collections.Immutable;
using System.Text;
using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ErrorOr.Generators;

/// <summary>
///     Generates OpenAPI transformers following the strict 1:1 mapping rule:
///     - 1 attribute → 1 transformer → 1 registration
///     - Generator is a transcriber, not a composer
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class OpenApiTransformerGenerator : IIncrementalGenerator
{
    // EPS06: Roslyn's readonly struct API causes unavoidable defensive copies.
#pragma warning disable EPS06

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Attributes are emitted by ErrorOrEndpointGenerator (shared across both generators)
        var endpoints = CombineHttpMethodProviders(context);

        var typeMetadata = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is TypeDeclarationSyntax,
                static (ctx, ct) => ExtractTypeMetadata(ctx, ct))
            .WhereNotNull()
            .CollectAsEquatableArray();

        context.RegisterSourceOutput(
            endpoints.Combine(typeMetadata),
            static (spc, data) => Emit(spc, data.Left.AsImmutableArray(), data.Right.AsImmutableArray()));
    }

    private static IncrementalValueProvider<EquatableArray<OpenApiEndpointInfo>> CombineHttpMethodProviders(
        IncrementalGeneratorInitializationContext context) =>
        IncrementalProviderExtensions.CombineNine(
            CreateEndpointProvider(context, WellKnownTypes.GetAttribute),
            CreateEndpointProvider(context, WellKnownTypes.PostAttribute),
            CreateEndpointProvider(context, WellKnownTypes.PutAttribute),
            CreateEndpointProvider(context, WellKnownTypes.DeleteAttribute),
            CreateEndpointProvider(context, WellKnownTypes.PatchAttribute),
            CreateEndpointProvider(context, WellKnownTypes.HeadAttribute),
            CreateEndpointProvider(context, WellKnownTypes.OptionsAttribute),
            CreateEndpointProvider(context, WellKnownTypes.TraceAttribute),
            CreateEndpointProvider(context, WellKnownTypes.ErrorOrEndpointAttribute));

    private static IncrementalValuesProvider<OpenApiEndpointInfo> CreateEndpointProvider(
        IncrementalGeneratorInitializationContext context,
        string attributeName)
    {
        return context.SyntaxProvider
            .ForAttributeWithMetadataName(
                attributeName,
                static (node, _) => node is MethodDeclarationSyntax,
                static (ctx, ct) => ExtractOpenApiMetadata(ctx, ct))
            .WhereNotNull();
    }

    private static OpenApiEndpointInfo? ExtractOpenApiMetadata(
        GeneratorAttributeSyntaxContext ctx,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ctx.TargetSymbol is not IMethodSymbol { IsStatic: true } method)
            return null;

        // Extract HTTP method and pattern from attribute
        // Combined null check: attr exists AND has a valid AttributeClass
        if (ctx.Attributes.FirstOrDefault() is not { AttributeClass: { } attrClass } attr)
            return null;

        var attrClassName = attrClass.ToDisplayString();

        var (httpMethod, pattern) = attrClassName switch
        {
            WellKnownTypes.GetAttribute => (WellKnownTypes.HttpMethod.Get, GetPattern(attr)),
            WellKnownTypes.PostAttribute => (WellKnownTypes.HttpMethod.Post, GetPattern(attr)),
            WellKnownTypes.PutAttribute => (WellKnownTypes.HttpMethod.Put, GetPattern(attr)),
            WellKnownTypes.DeleteAttribute => (WellKnownTypes.HttpMethod.Delete, GetPattern(attr)),
            WellKnownTypes.PatchAttribute => (WellKnownTypes.HttpMethod.Patch, GetPattern(attr)),
            WellKnownTypes.HeadAttribute => (WellKnownTypes.HttpMethod.Head, GetPattern(attr)),
            WellKnownTypes.OptionsAttribute => (WellKnownTypes.HttpMethod.Options, GetPattern(attr)),
            WellKnownTypes.TraceAttribute => (WellKnownTypes.HttpMethod.Trace, GetPattern(attr)),
            WellKnownTypes.ErrorOrEndpointAttribute => GetBaseAttributeInfo(attr),
            _ => (null, null)
        };

        if (httpMethod is null || pattern is null)
            return null;

        // Extract XML documentation
        var xmlDoc = method.GetDocumentationCommentXml();
        var (summary, description) = ParseXmlDoc(xmlDoc);

        // Extract containing type info for tag generation
        var containingType = method.ContainingType;
        var containingTypeFqn = containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var (tagName, operationId) = TypeNameHelper.GetEndpointIdentity(containingTypeFqn, method.Name);

        // Normalized pattern for matching (remove leading slash if present, handle duplicates logic)
        // But for OpenAPI context.Description.RelativePath usually has NO leading slash for route groups?
        // Actually context.Description.RelativePath usually matches the full route pattern.
        // We will store it exactly as extracted from attribute.
        // Note: HttpMethod needs to be UPPER CASE for matching.

        return new OpenApiEndpointInfo(
            operationId,
            tagName,
            summary,
            description,
            httpMethod.ToUpperInvariant(),
            pattern);
    }

    private static string GetPattern(AttributeData attr)
    {
        if (attr.ConstructorArguments.Length > 0 &&
            attr.ConstructorArguments[0].Value is string p &&
            !string.IsNullOrWhiteSpace(p))
            return p;
        return "/";
    }

    private static (string? httpMethod, string? pattern) GetBaseAttributeInfo(AttributeData attr)
    {
        if (attr.ConstructorArguments.Length < 2)
            return (null, null);

        var method = attr.ConstructorArguments[0].Value as string;
        var pattern = attr.ConstructorArguments[1].Value as string;

        return string.IsNullOrWhiteSpace(method)
            ? (null, null)
            : (method, string.IsNullOrWhiteSpace(pattern) ? "/" : pattern);
    }

    private static (string? summary, string? description) ParseXmlDoc(string? xml)
    {
        // Use pattern matching to establish non-null reference for compiler
        if (string.IsNullOrWhiteSpace(xml) || xml is null)
            return (null, null);

        string? summary = null;
        string? description = null;

        // Simple XML parsing for summary and remarks
        var summaryStart = xml.IndexOf("<summary>", StringComparison.Ordinal);
        var summaryEnd = xml.IndexOf("</summary>", StringComparison.Ordinal);
        if (summaryStart >= 0 && summaryEnd > summaryStart)
            summary = xml.Substring(summaryStart + 9, summaryEnd - summaryStart - 9)
                .Trim()
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Trim();

        var remarksStart = xml.IndexOf("<remarks>", StringComparison.Ordinal);
        var remarksEnd = xml.IndexOf("</remarks>", StringComparison.Ordinal);
        if (remarksStart >= 0 && remarksEnd > remarksStart)
            description = xml.Substring(remarksStart + 9, remarksEnd - remarksStart - 9)
                .Trim()
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Trim();

        return (summary, description);
    }

    private static string GetReflectionFullName(INamedTypeSymbol symbol)
    {
        var typeNames = new Stack<string>();
        for (var current = symbol; current is not null; current = current.ContainingType)
            typeNames.Push(current.MetadataName);

        var typeName = string.Join("+", typeNames);
        var ns = symbol.ContainingNamespace?.IsGlobalNamespace == true
            ? null
            : symbol.ContainingNamespace?.ToDisplayString();

        return string.IsNullOrEmpty(ns) ? typeName : $"{ns}.{typeName}";
    }

    private static TypeMetadataInfo? ExtractTypeMetadata(
        GeneratorSyntaxContext ctx,
        CancellationToken ct)
    {
        if (ctx.Node is not TypeDeclarationSyntax typeDecl)
            return null;

        // Skip null symbols and compiler-generated types
        if (ctx.SemanticModel.GetDeclaredSymbol(typeDecl, ct) is not INamedTypeSymbol symbol || symbol.IsImplicitlyDeclared)
            return null;

        // Skip types without XML docs
        var xmlDoc = symbol.GetDocumentationCommentXml();
        if (string.IsNullOrWhiteSpace(xmlDoc))
            return null;

        var (summary, _) = ParseXmlDoc(xmlDoc);
        if (summary is null)
            return null;

        var typeKey = GetReflectionFullName(symbol);

        return new TypeMetadataInfo(typeKey, summary);
    }

    private static void Emit(
        SourceProductionContext spc,
        ImmutableArray<OpenApiEndpointInfo> endpoints,
        ImmutableArray<TypeMetadataInfo> types)
    {
        if (endpoints.IsDefaultOrEmpty)
            return;

        var code = new StringBuilder();
        code.AppendLine("// <auto-generated/>");
        code.AppendLine("#nullable enable");
        code.AppendLine();
        code.AppendLine("using System;");
        code.AppendLine("using System.Collections.Frozen;");
        code.AppendLine("using System.Collections.Generic;");
        code.AppendLine("using System.Threading;");
        code.AppendLine("using System.Threading.Tasks;");
        code.AppendLine("using Microsoft.AspNetCore.OpenApi;");
        code.AppendLine("using Microsoft.AspNetCore.Routing;");
        code.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        code.AppendLine("using Microsoft.OpenApi;");
        code.AppendLine();
        code.AppendLine("namespace ErrorOr.Generated;");
        code.AppendLine();

        // Collect unique tags (1 attribute → 1 transformer)
        var tags = endpoints.Select(static e => e.TagName).Distinct(StringComparer.Ordinal)
            .OrderBy(static t => t, StringComparer.Ordinal).ToList();

        // Emit tag transformers (strict 1:1 - one transformer per unique tag)
        foreach (var tag in tags) EmitTagTransformer(code, tag);

        // Emit operation transformer (applies XML doc summaries)
        var hasOperationDocs = EmitOperationTransformer(code, endpoints);

        // Emit schema transformer (applies type descriptions)
        var hasTypeDocs = false;
        if (!types.IsDefaultOrEmpty) hasTypeDocs = EmitSchemaTransformer(code, types);

        // Emit registration extension
        EmitRegistrationExtension(code, tags, hasOperationDocs, hasTypeDocs);

        spc.AddSource("OpenApiTransformers.g.cs", SourceText.From(code.ToString(), Encoding.UTF8));
    }

    private static void EmitTagTransformer(StringBuilder code, string tagName)
    {
        var safeTagName = SanitizeIdentifier(tagName);
        code.AppendLine("/// <summary>");
        code.AppendLine($"/// Document transformer for tag: {tagName}");
        code.AppendLine($"/// Generated from: [ErrorOrEndpoint] attribute on *{tagName}Endpoints class");
        code.AppendLine("/// </summary>");
        code.AppendLine($"file sealed class Tag_{safeTagName}_Transformer : IOpenApiDocumentTransformer");
        code.AppendLine("{");
        code.AppendLine("    public Task TransformAsync(");
        code.AppendLine("        OpenApiDocument document,");
        code.AppendLine("        OpenApiDocumentTransformerContext context,");
        code.AppendLine("        CancellationToken cancellationToken)");
        code.AppendLine("    {");
        // OpenApiDocument.Tags setter auto-wraps with OpenApiTagComparer.Instance
        // which handles deduplication by Name - no manual .Any() check needed
        code.AppendLine("        document.Tags ??= new HashSet<OpenApiTag>();");
        code.AppendLine($"        document.Tags.Add(new OpenApiTag {{ Name = \"{tagName}\" }});");
        code.AppendLine("        return Task.CompletedTask;");
        code.AppendLine("    }");
        code.AppendLine("}");
        code.AppendLine();
    }

    private static bool EmitOperationTransformer(StringBuilder code, ImmutableArray<OpenApiEndpointInfo> endpoints)
    {
        // Collect operations with XML docs
        var opsWithDocs = endpoints
            .Where(static e => !string.IsNullOrEmpty(e.Summary) || !string.IsNullOrEmpty(e.Description))
            .OrderBy(static e => e.Pattern, StringComparer.Ordinal)
            .ThenBy(static e => e.HttpMethod, StringComparer.Ordinal).ToList();

        if (opsWithDocs.Count is 0)
            return false;

        code.AppendLine("/// <summary>");
        code.AppendLine("/// Operation transformer that applies XML documentation to operations.");
        code.AppendLine("/// Each entry is a strict 1:1 mapping from XML doc to operation metadata.");
        code.AppendLine("/// </summary>");
        code.AppendLine("file sealed class XmlDocOperationTransformer : IOpenApiOperationTransformer");
        code.AppendLine("{");
        code.AppendLine("    // Pre-computed metadata from XML docs (compile-time extraction)");
        code.AppendLine(
            "    private static readonly FrozenDictionary<string, (string? Summary, string? Description)> OperationDocs =");
        code.AppendLine("        new Dictionary<string, (string? Summary, string? Description)>");
        code.AppendLine("        {");

        foreach (var op in opsWithDocs)
        {
            var summary = op.Summary is not null ? $"\"{EscapeString(op.Summary)}\"" : "null";
            var description = op.Description is not null ? $"\"{EscapeString(op.Description)}\"" : "null";
            code.AppendLine($"            [\"{op.OperationId}\"] = ({summary}, {description}),");
        }

        code.AppendLine("        }.ToFrozenDictionary(StringComparer.Ordinal);");
        code.AppendLine();
        code.AppendLine("    public Task TransformAsync(");
        code.AppendLine("        OpenApiOperation operation,");
        code.AppendLine("        OpenApiOperationTransformerContext context,");
        code.AppendLine("        CancellationToken cancellationToken)");
        code.AppendLine("    {");
        code.AppendLine("        string? operationId = null;");
        code.AppendLine("        var metadata = context.Description.ActionDescriptor?.EndpointMetadata;");
        code.AppendLine("        if (metadata is not null)");
        code.AppendLine("        {");
        code.AppendLine("            for (var i = 0; i < metadata.Count; i++)");
        code.AppendLine("            {");
        code.AppendLine("                if (metadata[i] is IEndpointNameMetadata nameMetadata)");
        code.AppendLine("                {");
        code.AppendLine("                    operationId = nameMetadata.EndpointName;");
        code.AppendLine("                    break;");
        code.AppendLine("                }");
        code.AppendLine("            }");
        code.AppendLine("        }");
        code.AppendLine();
        code.AppendLine("        if (operationId is not null && OperationDocs.TryGetValue(operationId, out var docs))");
        code.AppendLine("        {");
        code.AppendLine("            if (docs.Summary is not null)");
        code.AppendLine("                operation.Summary ??= docs.Summary;");
        code.AppendLine("            if (docs.Description is not null)");
        code.AppendLine("                operation.Description ??= docs.Description;");
        code.AppendLine("        }");
        code.AppendLine("        return Task.CompletedTask;");
        code.AppendLine("    }");
        code.AppendLine("}");
        code.AppendLine();

        return true;
    }

    private static bool EmitSchemaTransformer(StringBuilder code, ImmutableArray<TypeMetadataInfo> types)
    {
        var typesWithDocs = types.OrderBy(static t => t.TypeKey, StringComparer.Ordinal).ToList();

        if (typesWithDocs.Count is 0)
            return false;

        code.AppendLine("/// <summary>");
        code.AppendLine("/// Schema transformer that applies type XML documentation to schemas.");
        code.AppendLine("/// Each entry is a strict 1:1 mapping from XML doc to schema description.");
        code.AppendLine("/// </summary>");
        code.AppendLine("file sealed class XmlDocSchemaTransformer : IOpenApiSchemaTransformer");
        code.AppendLine("{");
        code.AppendLine("    // Pre-computed type descriptions from XML docs");
        code.AppendLine("    private static readonly FrozenDictionary<string, string> TypeDescriptions =");
        code.AppendLine("        new Dictionary<string, string>");
        code.AppendLine("        {");

        foreach (var type in typesWithDocs)
            code.AppendLine($"            [\"{type.TypeKey}\"] = \"{EscapeString(type.Description)}\",");

        code.AppendLine("        }.ToFrozenDictionary(StringComparer.Ordinal);");
        code.AppendLine();
        code.AppendLine("    public Task TransformAsync(");
        code.AppendLine("        OpenApiSchema schema,");
        code.AppendLine("        OpenApiSchemaTransformerContext context,");
        code.AppendLine("        CancellationToken cancellationToken)");
        code.AppendLine("    {");
        // Use FullName for robust matching (matches generator's FQN key)
        code.AppendLine("        var type = context.JsonTypeInfo.Type;");
        code.AppendLine("        var typeName = type.IsGenericType ? type.GetGenericTypeDefinition().FullName : type.FullName;");
        code.AppendLine("        if (typeName is not null && TypeDescriptions.TryGetValue(typeName, out var description))");
        code.AppendLine("        {");
        code.AppendLine("            schema.Description ??= description;");
        code.AppendLine("        }");
        code.AppendLine("        return Task.CompletedTask;");
        code.AppendLine("    }");
        code.AppendLine("}");
        code.AppendLine();

        return true;
    }

    private static void EmitRegistrationExtension(
        StringBuilder code,
        List<string> tags,
        bool hasOperationDocs,
        bool hasTypeDocs)
    {
        code.AppendLine("/// <summary>");
        code.AppendLine("/// Extension methods for registering generated OpenAPI transformers.");
        code.AppendLine("/// </summary>");
        code.AppendLine("public static class GeneratedOpenApiExtensions");
        code.AppendLine("{");
        code.AppendLine("    /// <summary>");
        code.AppendLine("    /// Adds OpenAPI with generated transformers for ErrorOr endpoints.");
        code.AppendLine("    /// Each transformer is registered following the strict 1:1 mapping rule.");
        code.AppendLine("    /// </summary>");
        code.AppendLine("    public static IServiceCollection AddErrorOrOpenApi(");
        code.AppendLine("        this IServiceCollection services,");
        code.AppendLine("        string documentName = \"v1\")");
        code.AppendLine("    {");
        code.AppendLine("        services.AddOpenApi(documentName, options =>");
        code.AppendLine("        {");

        // Register tag transformers (1:1 - one per tag)
        foreach (var tag in tags)
        {
            var safeTagName = SanitizeIdentifier(tag);
            code.AppendLine($"            // Tag: {tag}");
            code.AppendLine($"            options.AddDocumentTransformer(new Tag_{safeTagName}_Transformer());");
        }

        // Register operation transformer if we have docs
        if (hasOperationDocs)
        {
            code.AppendLine();
            code.AppendLine("            // XML doc summaries → operation metadata");
            code.AppendLine("            options.AddOperationTransformer(new XmlDocOperationTransformer());");
        }

        // Register schema transformer if we have type docs
        if (hasTypeDocs)
        {
            code.AppendLine();
            code.AppendLine("            // XML doc summaries → schema descriptions");
            code.AppendLine("            options.AddSchemaTransformer(new XmlDocSchemaTransformer());");
        }

        code.AppendLine("        });");
        code.AppendLine();
        code.AppendLine("        return services;");
        code.AppendLine("    }");
        code.AppendLine("}");
    }

    private static string SanitizeIdentifier(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
            if (char.IsLetterOrDigit(c) || c == '_')
                sb.Append(c);
            else
                sb.Append('_');

        return sb.ToString();
    }

    private static string EscapeString(string s) =>
        s
            .Replace("\\", @"\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
#pragma warning restore EPS06
}
