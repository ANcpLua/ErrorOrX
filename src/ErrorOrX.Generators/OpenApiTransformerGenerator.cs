using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ErrorOr.Generators;

/// <summary>
///     Generates OpenAPI transformers from XML documentation on ErrorOr endpoints.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class OpenApiTransformerGenerator : IIncrementalGenerator
{
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
        IncrementalGeneratorInitializationContext context)
    {
        return IncrementalProviderExtensions.CombineNine(
            CreateEndpointProvider(context, WellKnownTypes.GetAttribute),
            CreateEndpointProvider(context, WellKnownTypes.PostAttribute),
            CreateEndpointProvider(context, WellKnownTypes.PutAttribute),
            CreateEndpointProvider(context, WellKnownTypes.DeleteAttribute),
            CreateEndpointProvider(context, WellKnownTypes.PatchAttribute),
            CreateEndpointProvider(context, WellKnownTypes.HeadAttribute),
            CreateEndpointProvider(context, WellKnownTypes.OptionsAttribute),
            CreateEndpointProvider(context, WellKnownTypes.TraceAttribute),
            CreateEndpointProvider(context, WellKnownTypes.ErrorOrEndpointAttribute));
    }

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
        var parameterDocs = ParseParamTags(xmlDoc);

        // Extract containing type info for tag generation
        var containingType = method.ContainingType;
        var containingTypeFqn = containingType.GetFullyQualifiedName();
        var (tagName, operationId) = EndpointNameHelper.GetEndpointIdentity(containingTypeFqn, method.Name);

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
            pattern,
            new EquatableArray<(string, string)>(parameterDocs));
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
        var summaryStart = xml.IndexOfOrdinal("<summary>");
        var summaryEnd = xml.IndexOfOrdinal("</summary>");
        if (summaryStart >= 0 && summaryEnd > summaryStart)
            summary = xml.Substring(summaryStart + 9, summaryEnd - summaryStart - 9)
                .Trim()
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Trim();

        var remarksStart = xml.IndexOfOrdinal("<remarks>");
        var remarksEnd = xml.IndexOfOrdinal("</remarks>");
        if (remarksStart >= 0 && remarksEnd > remarksStart)
            description = xml.Substring(remarksStart + 9, remarksEnd - remarksStart - 9)
                .Trim()
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Trim();

        return (summary, description);
    }

    private static ImmutableArray<(string ParamName, string Description)> ParseParamTags(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml) || xml is null)
            return ImmutableArray<(string, string)>.Empty;

        var parameters = new List<(string, string)>();
        var searchPos = 0;

        while (true)
        {
            var paramStart = xml.IndexOf("<param name=\"", searchPos, StringComparison.Ordinal);
            if (paramStart < 0) break;

            var nameStart = paramStart + 13;
            var nameEnd = xml.IndexOf("\"", nameStart, StringComparison.Ordinal);
            if (nameEnd < 0) break;

            var paramName = xml.Substring(nameStart, nameEnd - nameStart);

            var contentStart = xml.IndexOf(">", nameEnd, StringComparison.Ordinal);
            if (contentStart < 0) break;
            contentStart++;

            var contentEnd = xml.IndexOf("</param>", contentStart, StringComparison.Ordinal);
            if (contentEnd < 0) break;

            var description = xml.Substring(contentStart, contentEnd - contentStart)
                .Trim()
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Trim();
            if (!string.IsNullOrWhiteSpace(description))
                parameters.Add((paramName, description));

            searchPos = contentEnd + 8;
        }

        return [..parameters];
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
        if (ctx.SemanticModel.GetDeclaredSymbol(typeDecl, ct) is not INamedTypeSymbol symbol ||
            symbol.IsImplicitlyDeclared)
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
        var safeTagName = tagName.SanitizeIdentifier();
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
        // Collect operations with XML docs (summary/description OR parameter docs)
        var opsWithDocs = endpoints
            .Where(static e => !string.IsNullOrEmpty(e.Summary) || !string.IsNullOrEmpty(e.Description) ||
                               !e.ParameterDocs.IsDefaultOrEmpty)
            .OrderBy(static e => e.Pattern, StringComparer.Ordinal)
            .ThenBy(static e => e.HttpMethod, StringComparer.Ordinal).ToList();

        if (opsWithDocs.Count is 0)
            return false;

        // Collect operations with parameter docs
        var opsWithParamDocs = opsWithDocs
            .Where(static e => !e.ParameterDocs.IsDefaultOrEmpty)
            .ToList();

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

        foreach (var op in opsWithDocs.Where(static e =>
                     !string.IsNullOrEmpty(e.Summary) || !string.IsNullOrEmpty(e.Description)))
        {
            var summary = op.Summary is not null ? $"\"{op.Summary.EscapeCSharpString()}\"" : "null";
            var description = op.Description is not null ? $"\"{op.Description.EscapeCSharpString()}\"" : "null";
            code.AppendLine($"            [\"{op.OperationId}\"] = ({summary}, {description}),");
        }

        code.AppendLine("        }.ToFrozenDictionary(StringComparer.Ordinal);");
        code.AppendLine();

        // Emit parameter docs dictionary
        code.AppendLine("    // Pre-computed parameter descriptions from XML <param> tags");
        code.AppendLine(
            "    private static readonly FrozenDictionary<string, FrozenDictionary<string, string>> ParameterDocs =");
        code.AppendLine("        new Dictionary<string, FrozenDictionary<string, string>>");
        code.AppendLine("        {");

        foreach (var op in opsWithParamDocs)
        {
            code.AppendLine($"            [\"{op.OperationId}\"] = new Dictionary<string, string>");
            code.AppendLine("            {");
            foreach (var (paramName, paramDesc) in op.ParameterDocs.AsImmutableArray())
                code.AppendLine(
                    $"                [\"{paramName.EscapeCSharpString()}\"] = \"{paramDesc.EscapeCSharpString()}\",");

            code.AppendLine("            }.ToFrozenDictionary(StringComparer.Ordinal),");
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
        code.AppendLine("        if (operationId is null)");
        code.AppendLine("            return Task.CompletedTask;");
        code.AppendLine();
        code.AppendLine("        // Apply summary and description");
        code.AppendLine("        if (OperationDocs.TryGetValue(operationId, out var docs))");
        code.AppendLine("        {");
        code.AppendLine("            if (docs.Summary is not null)");
        code.AppendLine("                operation.Summary ??= docs.Summary;");
        code.AppendLine("            if (docs.Description is not null)");
        code.AppendLine("                operation.Description ??= docs.Description;");
        code.AppendLine("        }");
        code.AppendLine();
        code.AppendLine("        // Apply parameter descriptions");
        code.AppendLine(
            "        if (ParameterDocs.TryGetValue(operationId, out var paramDocs) && operation.Parameters is not null)");
        code.AppendLine("        {");
        code.AppendLine("            foreach (var param in operation.Parameters)");
        code.AppendLine("            {");
        code.AppendLine(
            "                if (param.Name is not null && paramDocs.TryGetValue(param.Name, out var paramDesc))");
        code.AppendLine("                {");
        code.AppendLine("                    param.Description ??= paramDesc;");
        code.AppendLine("                }");
        code.AppendLine("            }");
        code.AppendLine("        }");
        code.AppendLine();
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
        code.AppendLine("/// AOT-safe: Uses Type as dictionary key (no runtime reflection).");
        code.AppendLine("/// </summary>");
        code.AppendLine("file sealed class XmlDocSchemaTransformer : IOpenApiSchemaTransformer");
        code.AppendLine("{");
        code.AppendLine(
            "    // Pre-computed type descriptions from XML docs (AOT-safe: Type keys resolved at compile-time)");
        code.AppendLine("    private static readonly FrozenDictionary<Type, string> TypeDescriptions =");
        code.AppendLine("        new Dictionary<Type, string>");
        code.AppendLine("        {");

        foreach (var type in typesWithDocs)
        {
            // Convert reflection-style name (Namespace.Outer+Inner) to C# typeof expression (global::Namespace.Outer.Inner)
            var typeofExpr = ConvertToTypeofExpression(type.TypeKey);
            code.AppendLine($"            [typeof({typeofExpr})] = \"{type.Description.EscapeCSharpString()}\",");
        }

        code.AppendLine("        }.ToFrozenDictionary();");
        code.AppendLine();
        code.AppendLine("    public Task TransformAsync(");
        code.AppendLine("        OpenApiSchema schema,");
        code.AppendLine("        OpenApiSchemaTransformerContext context,");
        code.AppendLine("        CancellationToken cancellationToken)");
        code.AppendLine("    {");
        // AOT-safe: Direct Type lookup without reflection
        code.AppendLine("        var type = context.JsonTypeInfo.Type;");
        code.AppendLine("        // For generic types, lookup the generic type definition");
        code.AppendLine("        var lookupType = type.IsGenericType ? type.GetGenericTypeDefinition() : type;");
        code.AppendLine("        if (TypeDescriptions.TryGetValue(lookupType, out var description))");
        code.AppendLine("        {");
        code.AppendLine("            schema.Description ??= description;");
        code.AppendLine("        }");
        code.AppendLine("        return Task.CompletedTask;");
        code.AppendLine("    }");
        code.AppendLine("}");
        code.AppendLine();

        return true;
    }

    /// <summary>
    ///     Converts a reflection-style type name to a C# typeof expression.
    ///     Example: "Namespace.Outer+Inner" → "global::Namespace.Outer.Inner"
    /// </summary>
    private static string ConvertToTypeofExpression(string reflectionName)
    {
        // Replace nested type separator (+) with C# dot notation
        var csharpName = reflectionName.Replace('+', '.');
        return $"global::{csharpName}";
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
            var safeTagName = tag.SanitizeIdentifier();
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
}
