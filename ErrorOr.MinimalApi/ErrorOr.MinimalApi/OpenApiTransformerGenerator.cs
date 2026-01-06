using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ErrorOr.MinimalApi;

/// <summary>
///     Generates OpenAPI transformers following the strict 1:1 mapping rule:
///     - 1 attribute → 1 transformer → 1 registration
///     - Generator is a transcriber, not a composer
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class OpenApiTransformerGenerator : IIncrementalGenerator
{
    // EPS06: Defensive copies are unavoidable with Roslyn's incremental generator API.
    // IncrementalValuesProvider<T> and IncrementalValueProvider<T> are readonly structs,
    // and extension methods like Where(), Select(), Collect(), Combine() inherently copy them.
    // This is by design in the Roslyn API and has negligible performance impact.
#pragma warning disable EPS06

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Create providers for each HTTP method attribute
        var getProvider = CreateEndpointProvider(context, WellKnownTypes.GetAttribute);
        var postProvider = CreateEndpointProvider(context, WellKnownTypes.PostAttribute);
        var putProvider = CreateEndpointProvider(context, WellKnownTypes.PutAttribute);
        var deleteProvider = CreateEndpointProvider(context, WellKnownTypes.DeleteAttribute);
        var patchProvider = CreateEndpointProvider(context, WellKnownTypes.PatchAttribute);
        var baseProvider = CreateEndpointProvider(context, WellKnownTypes.ErrorOrEndpointAttribute);

        // Combine all endpoint providers
        var endpoints = CombineEndpointProviders(
            getProvider, postProvider, putProvider,
            deleteProvider, patchProvider, baseProvider);

        // Collect type metadata for schema transformers
        var typeDecls = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is TypeDeclarationSyntax,
                static (ctx, ct) => ExtractTypeMetadata(ctx, ct));
        // Use Where + Select pattern - the null-forgiving is safe after Where filters nulls
        var types = typeDecls
            .Where(static x => x.HasValue)
            .Select(static (x, _) => x!.Value)
            .Collect();

        // Combine and emit
        var combined = endpoints.Combine(types);
        context.RegisterSourceOutput(
            combined,
            static (spc, data) => Emit(spc, data.Left, data.Right));
    }

    private static IncrementalValuesProvider<OpenApiEndpointInfo> CreateEndpointProvider(
        IncrementalGeneratorInitializationContext context,
        string attributeName)
    {
        // Use local vars to avoid EPS06 hidden struct copies
        var methods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                attributeName,
                static (node, _) => node is MethodDeclarationSyntax,
                static (ctx, ct) => ExtractOpenApiMetadata(ctx, ct));
        // Use Where + Select pattern - the null-forgiving is safe after Where filters nulls
        return methods
            .Where(static x => x.HasValue)
            .Select(static (x, _) => x!.Value);
    }

    private static IncrementalValueProvider<ImmutableArray<OpenApiEndpointInfo>> CombineEndpointProviders(
        IncrementalValuesProvider<OpenApiEndpointInfo> p1,
        IncrementalValuesProvider<OpenApiEndpointInfo> p2,
        IncrementalValuesProvider<OpenApiEndpointInfo> p3,
        IncrementalValuesProvider<OpenApiEndpointInfo> p4,
        IncrementalValuesProvider<OpenApiEndpointInfo> p5,
        IncrementalValuesProvider<OpenApiEndpointInfo> p6)
    {
        // Use local vars to avoid EPS06 hidden struct copies
        var c1 = p1.Collect();
        var c2 = p2.Collect();
        var c3 = p3.Collect();
        var c4 = p4.Collect();
        var c5 = p5.Collect();
        var c6 = p6.Collect();

        var combined1 = c1.Combine(c2);
        var combined2 = combined1.Combine(c3);
        var combined3 = combined2.Combine(c4);
        var combined4 = combined3.Combine(c5);
        var combined5 = combined4.Combine(c6);

        return combined5.Select(static (combined, _) =>
        {
            var (((((e0, e1), e2), e3), e4), e5) = combined;
            var builder = ImmutableArray.CreateBuilder<OpenApiEndpointInfo>(
                e0.Length + e1.Length + e2.Length + e3.Length + e4.Length + e5.Length);
            builder.AddRange(e0);
            builder.AddRange(e1);
            builder.AddRange(e2);
            builder.AddRange(e3);
            builder.AddRange(e4);
            builder.AddRange(e5);
            return builder.ToImmutable();
        });
    }

    private static OpenApiEndpointInfo? ExtractOpenApiMetadata(
        GeneratorAttributeSyntaxContext ctx,
        CancellationToken ct)
    {
        if (ctx.TargetSymbol is not IMethodSymbol method)
            return null;

        // Skip non-static methods (they'll be reported as errors by the endpoint generator)
        if (!method.IsStatic)
            return null;

        // Extract HTTP method and pattern from attribute
        var attr = ctx.Attributes.FirstOrDefault();
        if (attr is null)
            return null;

        var attrClass = attr.AttributeClass?.ToDisplayString();
        if (attrClass is null)
            return null;

        var (httpMethod, pattern) = attrClass switch
        {
            WellKnownTypes.GetAttribute => (WellKnownTypes.HttpMethod.Get, GetPattern(attr)),
            WellKnownTypes.PostAttribute => (WellKnownTypes.HttpMethod.Post, GetPattern(attr)),
            WellKnownTypes.PutAttribute => (WellKnownTypes.HttpMethod.Put, GetPattern(attr)),
            WellKnownTypes.DeleteAttribute => (WellKnownTypes.HttpMethod.Delete, GetPattern(attr)),
            WellKnownTypes.PatchAttribute => (WellKnownTypes.HttpMethod.Patch, GetPattern(attr)),
            WellKnownTypes.ErrorOrEndpointAttribute => GetBaseAttributeInfo(attr),
            _ => (null, null)
        };

        if (httpMethod is null)
            return null;

        // Extract XML documentation
        var xmlDoc = method.GetDocumentationCommentXml();
        var (summary, description) = ParseXmlDoc(xmlDoc);

        // Extract containing type info for tag generation
        // CRITICAL: operationId algorithm MUST match Emitter.cs exactly
        var containingType = method.ContainingType;
        var className = containingType.Name;
        var tagName = className.EndsWith("Endpoints")
            ? className[..^"Endpoints".Length]
            : className;

        // Create unique operation ID - uses tagName (with "Endpoints" stripped), not className
        // Must match: ErrorOrEndpointGenerator.Emitter.cs EmitMapCall line ~83
        var operationId = $"{tagName}_{method.Name}";

        return new OpenApiEndpointInfo(
            httpMethod,
            pattern ?? "/",
            operationId,
            tagName,
            summary,
            description,
            method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            method.Name);
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

        if (string.IsNullOrWhiteSpace(method))
            return (null, null);

        return (method, string.IsNullOrWhiteSpace(pattern) ? "/" : pattern);
    }

    private static (string? summary, string? description) ParseXmlDoc(string? xml)
    {
        // Use pattern matching to establish non-null reference for compiler
        if (string.IsNullOrWhiteSpace(xml) || xml is not { } doc)
            return (null, null);

        string? summary = null;
        string? description = null;

        // Simple XML parsing for summary and remarks
        var summaryStart = doc.IndexOf("<summary>", StringComparison.Ordinal);
        var summaryEnd = doc.IndexOf("</summary>", StringComparison.Ordinal);
        if (summaryStart >= 0 && summaryEnd > summaryStart)
        {
            summary = doc.Substring(summaryStart + 9, summaryEnd - summaryStart - 9)
                .Trim()
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Trim();
        }

        var remarksStart = doc.IndexOf("<remarks>", StringComparison.Ordinal);
        var remarksEnd = doc.IndexOf("</remarks>", StringComparison.Ordinal);
        if (remarksStart >= 0 && remarksEnd > remarksStart)
        {
            description = doc.Substring(remarksStart + 9, remarksEnd - remarksStart - 9)
                .Trim()
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Trim();
        }

        return (summary, description);
    }

    private static TypeMetadataInfo? ExtractTypeMetadata(
        GeneratorSyntaxContext ctx,
        CancellationToken ct)
    {
        if (ctx.Node is not TypeDeclarationSyntax typeDecl)
            return null;

        var symbol = ctx.SemanticModel.GetDeclaredSymbol(typeDecl, ct);
        if (symbol is null)
            return null;

        // Skip compiler-generated types
        if (symbol.IsImplicitlyDeclared)
            return null;

        // Skip types without XML docs
        var xmlDoc = symbol.GetDocumentationCommentXml();
        if (string.IsNullOrWhiteSpace(xmlDoc))
            return null;

        var (summary, _) = ParseXmlDoc(xmlDoc);
        if (summary is null)
            return null;

        return new TypeMetadataInfo(
            symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            symbol.Name,
            summary);
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
        code.AppendLine("using System.Collections.Generic;");
        code.AppendLine("using System.Linq;");
        code.AppendLine("using System.Threading;");
        code.AppendLine("using System.Threading.Tasks;");
        code.AppendLine("using Microsoft.AspNetCore.OpenApi;");
        code.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        code.AppendLine("using Microsoft.OpenApi;");
        code.AppendLine();
        code.AppendLine("namespace ErrorOr.Http.Generated;");
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
        code.AppendLine("        document.Tags ??= new HashSet<OpenApiTag>();");
        code.AppendLine($"        if (!document.Tags.Any(t => t.Name == \"{tagName}\"))");
        code.AppendLine("        {");
        code.AppendLine($"            document.Tags.Add(new OpenApiTag {{ Name = \"{tagName}\" }});");
        code.AppendLine("        }");
        code.AppendLine("        return Task.CompletedTask;");
        code.AppendLine("    }");
        code.AppendLine("}");
        code.AppendLine();
    }

    private static bool EmitOperationTransformer(StringBuilder code, ImmutableArray<OpenApiEndpointInfo> endpoints)
    {
        // Collect operations with XML docs
        var opsWithDocs = endpoints.Where(static e => !string.IsNullOrEmpty(e.Summary) || !string.IsNullOrEmpty(e.Description))
            .OrderBy(static e => e.OperationId, StringComparer.Ordinal).ToList();

        if (opsWithDocs.Count == 0)
            return false;

        code.AppendLine("/// <summary>");
        code.AppendLine("/// Operation transformer that applies XML documentation to operations.");
        code.AppendLine("/// Each entry is a strict 1:1 mapping from XML doc to operation metadata.");
        code.AppendLine("/// </summary>");
        code.AppendLine("file sealed class XmlDocOperationTransformer : IOpenApiOperationTransformer");
        code.AppendLine("{");
        code.AppendLine("    // Pre-computed metadata from XML docs (compile-time extraction)");
        code.AppendLine(
            "    private static readonly Dictionary<string, (string? Summary, string? Description)> OperationDocs = new()");
        code.AppendLine("    {");

        foreach (var op in opsWithDocs)
        {
            var summary = op.Summary is not null ? $"\"{EscapeString(op.Summary)}\"" : "null";
            var description = op.Description is not null ? $"\"{EscapeString(op.Description)}\"" : "null";
            code.AppendLine($"        [\"{op.OperationId}\"] = ({summary}, {description}),");
        }

        code.AppendLine("    };");
        code.AppendLine();
        code.AppendLine("    public Task TransformAsync(");
        code.AppendLine("        OpenApiOperation operation,");
        code.AppendLine("        OpenApiOperationTransformerContext context,");
        code.AppendLine("        CancellationToken cancellationToken)");
        code.AppendLine("    {");
        code.AppendLine("        var operationId = operation.OperationId;");
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
        var typesWithDocs = types.OrderBy(static t => t.TypeName, StringComparer.Ordinal).ToList();

        if (typesWithDocs.Count == 0)
            return false;

        code.AppendLine("/// <summary>");
        code.AppendLine("/// Schema transformer that applies type XML documentation to schemas.");
        code.AppendLine("/// Each entry is a strict 1:1 mapping from XML doc to schema description.");
        code.AppendLine("/// </summary>");
        code.AppendLine("file sealed class XmlDocSchemaTransformer : IOpenApiSchemaTransformer");
        code.AppendLine("{");
        code.AppendLine("    // Pre-computed type descriptions from XML docs");
        code.AppendLine("    private static readonly Dictionary<string, string> TypeDescriptions = new()");
        code.AppendLine("    {");

        foreach (var type in typesWithDocs)
            code.AppendLine($"        [\"{type.TypeName}\"] = \"{EscapeString(type.Description)}\",");

        code.AppendLine("    };");
        code.AppendLine();
        code.AppendLine("    public Task TransformAsync(");
        code.AppendLine("        OpenApiSchema schema,");
        code.AppendLine("        OpenApiSchemaTransformerContext context,");
        code.AppendLine("        CancellationToken cancellationToken)");
        code.AppendLine("    {");
        code.AppendLine("        var typeName = context.JsonTypeInfo.Type.Name;");
        code.AppendLine("        if (TypeDescriptions.TryGetValue(typeName, out var description))");
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
        {
            if (char.IsLetterOrDigit(c) || c == '_')
                sb.Append(c);
            else
                sb.Append('_');
        }

        return sb.ToString();
    }

    private static string EscapeString(string s)
    {
        return s
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }
#pragma warning restore EPS06
}

/// <summary>
///     Immutable endpoint info for OpenAPI generation.
/// </summary>
internal readonly record struct OpenApiEndpointInfo(
    string HttpMethod,
    string Pattern,
    string OperationId,
    string TagName,
    string? Summary,
    string? Description,
    string ContainingTypeFqn,
    string MethodName);

/// <summary>
///     Immutable type metadata for schema generation.
/// </summary>
internal readonly record struct TypeMetadataInfo(
    string TypeFqn,
    string TypeName,
    string Description);
