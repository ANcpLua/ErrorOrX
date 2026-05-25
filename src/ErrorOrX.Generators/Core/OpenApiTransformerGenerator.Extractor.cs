using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ErrorOr.Generators;

/// <summary>
///     Metadata extraction logic for the OpenAPI transformer generator: pulls XML doc,
///     parameter definitions, and type descriptions out of the compilation.
/// </summary>
public sealed partial class OpenApiTransformerGenerator
{
    private static OpenApiEndpointInfo? ExtractOpenApiMetadata(
        GeneratorAttributeSyntaxContext ctx,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ctx.TargetSymbol is not IMethodSymbol { IsStatic: true } method) return null;

        // Extract HTTP method and pattern from attribute
        // Combined null check: attr exists AND has a valid AttributeClass
        if (ctx.Attributes.FirstOrDefault() is not { AttributeClass: { } attrClass } attr) return null;

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

        if (httpMethod is null || pattern is null) return null;

        // Extract XML documentation
        var xmlDoc = method.GetDocumentationCommentXml(cancellationToken: ct);
        var (summary, description) = ParseXmlDoc(xmlDoc);
        var parameterDocs = ParseParamTags(xmlDoc);

        // Extract containing type info for tag generation
        var containingType = method.ContainingType;
        var containingTypeFqn = containingType.GetFullyQualifiedName();
        var (tagName, operationId) = EndpointNameHelper.GetEndpointIdentity(containingTypeFqn, method.Name);

        var parameters = ExtractParameterDefinitions(method, pattern);

        return new OpenApiEndpointInfo(
            operationId,
            tagName,
            summary,
            description,
            httpMethod.ToUpperInvariant(),
            pattern,
            new EquatableArray<(string, string)>(parameterDocs),
            new EquatableArray<OpenApiParameterInfo>(parameters));
    }

    private static (string? summary, string? description) ParseXmlDoc(string? xml)
    {
        if (xml is null || string.IsNullOrWhiteSpace(xml)) return (null, null);

        string? summary = null;
        string? description = null;

        // Simple XML parsing for summary and remarks
        var summaryStart = xml.IndexOfOrdinal("<summary>");
        var summaryEnd = xml.IndexOfOrdinal("</summary>");
        if (summaryStart >= 0 && summaryEnd > summaryStart)
            summary = xml.Substring(summaryStart + 9, summaryEnd - summaryStart - 9)
                .Trim()
                .Replace("\r\n", " ")
                .Replace('\n', ' ')
                .Trim();

        var remarksStart = xml.IndexOfOrdinal("<remarks>");
        var remarksEnd = xml.IndexOfOrdinal("</remarks>");
        if (remarksStart >= 0 && remarksEnd > remarksStart)
            description = xml.Substring(remarksStart + 9, remarksEnd - remarksStart - 9)
                .Trim()
                .Replace("\r\n", " ")
                .Replace('\n', ' ')
                .Trim();

        return (summary, description);
    }

    private static ImmutableArray<(string ParamName, string Description)> ParseParamTags(string? xml)
    {
        if (xml is null || string.IsNullOrWhiteSpace(xml)) return ImmutableArray<(string, string)>.Empty;

        var parameters = new List<(string, string)>();
        var searchPos = 0;

        while (true)
        {
            var paramStart = xml.IndexOf("<param name=\"", searchPos, StringComparison.Ordinal);
            if (paramStart < 0) break;

            var nameStart = paramStart + 13;
            var nameEnd = xml.IndexOf('"', nameStart);
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
            if (!string.IsNullOrWhiteSpace(description)) parameters.Add((paramName, description));

            searchPos = contentEnd + 8;
        }

        return [.. parameters];
    }

    private static ImmutableArray<OpenApiParameterInfo> ExtractParameterDefinitions(
        IMethodSymbol method, string pattern)
    {
        var routeParams = RouteValidator.ExtractRouteParameters(pattern);
        var routeParamNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rp in routeParams)
            routeParamNames.Add(rp.Name);

        var parameters = new List<OpenApiParameterInfo>();

        foreach (var param in method.Parameters)
        {
            var typeFqn = param.Type.ToDisplayString();

            // Skip special types (services, context, etc.)
            if (IsSkippedParameterType(param, typeFqn)) continue;

            // Check explicit binding attributes
            var (explicitLocation, explicitName) = GetExplicitBinding(param);

            string name;
            string location;
            bool required;

            if (explicitLocation is not null)
            {
                // Explicit attribute wins
                name = explicitName ?? param.Name;
                location = explicitLocation;
                required = location == "path" ||
                           (param.Type.NullableAnnotation != NullableAnnotation.Annotated &&
                            !param.HasExplicitDefaultValue);
            }
            else if (routeParamNames.Contains(param.Name))
            {
                // Route parameter
                name = param.Name;
                location = "path";
                required = true;

                // Check if optional in route template
                foreach (var rp in routeParams)
                    if (string.Equals(rp.Name, param.Name, StringComparison.OrdinalIgnoreCase) && rp.IsOptional)
                    {
                        required = false;
                        break;
                    }
            }
            else if (IsPrimitiveType(typeFqn))
            {
                // Primitive not in route = query
                name = param.Name;
                location = "query";
                required = param.Type.NullableAnnotation != NullableAnnotation.Annotated &&
                           !param.HasExplicitDefaultValue;
            }
            else
            {
                // Complex type without explicit binding - skip (it's body or service)
                continue;
            }

            var (schemaType, schemaFormat) = GetOpenApiSchema(typeFqn);
            parameters.Add(new OpenApiParameterInfo(name, location, required, schemaType, schemaFormat));
        }

        return [.. parameters];
    }

    private static bool IsSkippedParameterType(IParameterSymbol param, string typeFqn)
    {
        // Skip special framework types
        if (typeFqn is WellKnownTypes.HttpContext or WellKnownTypes.CancellationToken
            or WellKnownTypes.FormFile or WellKnownTypes.FormFileCollection
            or WellKnownTypes.Stream or WellKnownTypes.PipeReader or WellKnownTypes.FormCollection)
            return true;

        // Skip interface types (services)
        if (param.Type.TypeKind == TypeKind.Interface) return true;

        // Skip abstract types (services)
        if (param.Type is { IsAbstract: true, TypeKind: TypeKind.Class }) return true;

        // Skip [FromServices] / [FromKeyedServices] / [FromBody] / [FromForm]
        foreach (var attr in param.GetAttributes())
        {
            var attrName = attr.AttributeClass?.ToDisplayString();
            if (attrName is WellKnownTypes.FromServicesAttribute or WellKnownTypes.FromBodyAttribute
                or WellKnownTypes.FromFormAttribute or WellKnownTypes.FromKeyedServicesAttribute)
                return true;
        }

        return false;
    }

    private static (string? Location, string? Name) GetExplicitBinding(ISymbol param)
    {
        foreach (var attr in param.GetAttributes())
        {
            var attrName = attr.AttributeClass?.ToDisplayString();
            switch (attrName)
            {
                case WellKnownTypes.FromRouteAttribute:
                {
                    var name = GetAttributeStringArg(attr, "Name");
                    return ("path", name);
                }
                case WellKnownTypes.FromQueryAttribute:
                {
                    var name = GetAttributeStringArg(attr, "Name");
                    return ("query", name);
                }
                case WellKnownTypes.FromHeaderAttribute:
                {
                    var name = GetAttributeStringArg(attr, "Name");
                    return ("header", name);
                }
            }
        }

        return (null, null);
    }

    private static string? GetAttributeStringArg(AttributeData attr, string propName)
    {
        foreach (var kvp in attr.NamedArguments)
            if (kvp.Key == propName && kvp.Value.Value is string s && !string.IsNullOrWhiteSpace(s))
                return s;

        return null;
    }

    private static bool IsPrimitiveType(string typeFqn)
    {
        // Strip nullable wrapper
        var type = typeFqn.EndsWithOrdinal("?") ? typeFqn.Substring(0, typeFqn.Length - 1) : typeFqn;

        return type is "int" or "System.Int32"
            or "long" or "System.Int64"
            or "short" or "System.Int16"
            or "uint" or "System.UInt32"
            or "ulong" or "System.UInt64"
            or "ushort" or "System.UInt16"
            or "byte" or "System.Byte"
            or "sbyte" or "System.SByte"
            or "bool" or "System.Boolean"
            or "decimal" or "System.Decimal"
            or "double" or "System.Double"
            or "float" or "System.Single"
            or "string" or "System.String"
            or "System.Guid"
            or "System.DateTime"
            or "System.DateTimeOffset"
            or "System.DateOnly"
            or "System.TimeOnly"
            or "System.TimeSpan";
    }

    private static (string SchemaType, string? SchemaFormat) GetOpenApiSchema(string typeFqn)
    {
        // Strip nullable wrapper
        var type = typeFqn.EndsWithOrdinal("?") ? typeFqn.Substring(0, typeFqn.Length - 1) : typeFqn;

        return type switch
        {
            "int" or "System.Int32" => ("integer", "int32"),
            "long" or "System.Int64" => ("integer", "int64"),
            "short" or "System.Int16" => ("integer", "int16"),
            "uint" or "System.UInt32" => ("integer", "int32"),
            "ulong" or "System.UInt64" => ("integer", "int64"),
            "ushort" or "System.UInt16" => ("integer", "int16"),
            "byte" or "System.Byte" => ("integer", "int32"),
            "sbyte" or "System.SByte" => ("integer", "int32"),
            "bool" or "System.Boolean" => ("boolean", null),
            "decimal" or "System.Decimal" => ("number", "double"),
            "double" or "System.Double" => ("number", "double"),
            "float" or "System.Single" => ("number", "float"),
            "System.Guid" => ("string", "uuid"),
            "System.DateTime" => ("string", "date-time"),
            "System.DateTimeOffset" => ("string", "date-time"),
            "System.DateOnly" => ("string", "date"),
            "System.TimeOnly" => ("string", "time"),
            "System.TimeSpan" => ("string", "duration"),
            _ => ("string", null)
        };
    }

    /// <summary>
    ///     Maps internal schema type string to OpenApi v2.0 JsonSchemaType enum name for emission.
    /// </summary>
    private static string ToJsonSchemaTypeEnum(string schemaType)
    {
        return schemaType switch
        {
            "integer" => "JsonSchemaType.Integer",
            "number" => "JsonSchemaType.Number",
            "boolean" => "JsonSchemaType.Boolean",
            _ => "JsonSchemaType.String"
        };
    }

    private static string GetReflectionFullName(ISymbol symbol)
    {
        var fqn = ((ITypeSymbol)symbol).GetFullyQualifiedName();
        return fqn.StartsWithOrdinal("global::") ? fqn.Substring("global::".Length) : fqn;
    }

    private static TypeMetadataInfo? ExtractTypeMetadata(
        GeneratorSyntaxContext ctx,
        CancellationToken ct)
    {
        if (ctx.Node is not TypeDeclarationSyntax typeDecl) return null;

        // Skip null symbols and compiler-generated types
        if (ctx.SemanticModel.GetDeclaredSymbol(typeDecl, ct) is not INamedTypeSymbol symbol ||
            symbol.IsImplicitlyDeclared)
            return null;

        // Skip types without XML docs
        var xmlDoc = symbol.GetDocumentationCommentXml(cancellationToken: ct);
        if (string.IsNullOrWhiteSpace(xmlDoc)) return null;

        var (summary, _) = ParseXmlDoc(xmlDoc);
        if (summary is null) return null;

        var typeKey = GetReflectionFullName(symbol);

        return new TypeMetadataInfo(typeKey, summary);
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
}
