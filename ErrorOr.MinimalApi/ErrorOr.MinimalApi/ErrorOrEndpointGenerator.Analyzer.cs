using System.Collections.Immutable;
using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ErrorOr.MinimalApi;

/// <summary>
///     Analyzer partial for OpenAPI & AOT validation (EOE021-EOE022).
/// </summary>
public sealed partial class ErrorOrEndpointGenerator
{
    private static void AnalyzeJsonContextCoverage(
        SourceProductionContext spc,
        ImmutableArray<EndpointDescriptor> endpoints,
        ImmutableArray<JsonContextInfo> userContexts)
    {
        if (endpoints.IsDefaultOrEmpty)
            return;

        var neededTypes = new HashSet<string>();
        var typeToEndpoint = new Dictionary<string, string>();

        foreach (var ep in endpoints)
        {
            if (!string.IsNullOrEmpty(ep.SuccessTypeFqn) &&
                !IsNoContentType(ep.SuccessTypeFqn))
            {
                if (neededTypes.Add(ep.SuccessTypeFqn))
                    typeToEndpoint[ep.SuccessTypeFqn] = ep.HandlerMethodName;
            }

            foreach (var param in ep.HandlerParameters)
            {
                if (param.Source == EndpointParameterSource.Body)
                {
                    if (neededTypes.Add(param.TypeFqn))
                        typeToEndpoint[param.TypeFqn] = ep.HandlerMethodName;
                }
            }
        }

        neededTypes.Add(WellKnownTypes.Fqn.ProblemDetails);
        neededTypes.Add(WellKnownTypes.Fqn.HttpValidationProblemDetails);

        if (userContexts.IsDefaultOrEmpty)
            return;

        var registeredTypes = new HashSet<string>();
        foreach (var ctx in userContexts)
        {
            foreach (var typeFqn in ctx.SerializableTypes)
                registeredTypes.Add(typeFqn);
        }

        foreach (var neededType in neededTypes)
        {
            if (IsPrimitiveJsonType(neededType))
                continue;

            var isRegistered = registeredTypes.Any(rt => TypeNamesMatch(neededType, rt));
            if (!isRegistered)
            {
                var displayType = neededType.Replace("global::", "");
                var endpointName = typeToEndpoint.TryGetValue(neededType, out var epName)
                    ? epName
                    : "ErrorOr endpoints";

                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.TypeNotInJsonContext,
                    Location.None,
                    displayType,
                    endpointName));
            }
        }
    }

    private static bool IsPrimitiveJsonType(string typeFqn)
    {
        var normalized = typeFqn.Replace("global::", "");
        return normalized is "System.String" or "string" or
            "System.Int32" or "int" or
            "System.Int64" or "long" or
            "System.Boolean" or "bool" or
            "System.Double" or "double" or
            "System.Decimal" or "decimal";
    }

    private static bool TypeNamesMatch(string needed, string registered)
    {
        var normalizedNeeded = needed.Replace("global::", "").Trim();
        var normalizedRegistered = registered.Replace("global::", "").Trim();

        if (normalizedNeeded == normalizedRegistered)
            return true;

        var neededShort = GetShortTypeName(normalizedNeeded);
        var registeredShort = GetShortTypeName(normalizedRegistered);

        return neededShort == registeredShort ||
               normalizedNeeded.EndsWith(registeredShort) ||
               normalizedRegistered.EndsWith(neededShort);
    }

    private static string GetShortTypeName(string typeName)
    {
        var isArray = typeName.EndsWith("[]");
        var baseName = isArray ? typeName[..^2] : typeName;
        var lastDot = baseName.LastIndexOf('.');
        var shortName = lastDot >= 0 ? baseName[(lastDot + 1)..] : baseName;
        return isArray ? shortName + "[]" : shortName;
    }

    private static bool IsNoContentType(string typeFqn)
    {
        return typeFqn.EndsWith("Deleted", StringComparison.Ordinal) ||
               typeFqn.EndsWith("Success", StringComparison.Ordinal) ||
               typeFqn.EndsWith("Created", StringComparison.Ordinal) ||
               typeFqn.EndsWith("Updated", StringComparison.Ordinal);
    }
}

internal readonly record struct JsonContextInfo(
    EquatableArray<string> SerializableTypes);

internal static class JsonContextProvider
{
    // EPS06: Defensive copies are unavoidable with Roslyn's incremental generator API
#pragma warning disable EPS06
    public static IncrementalValuesProvider<JsonContextInfo> Create(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
                static (ctx, _) => TransformJsonContext(ctx));
        // Use Where + Select pattern - the null-forgiving is safe after Where filters nulls
        return provider
            .Where(static info => info.HasValue)
            .Select(static (info, _) => info!.Value);
    }
#pragma warning restore EPS06

    private static JsonContextInfo? TransformJsonContext(GeneratorSyntaxContext ctx)
    {
        if (ctx.Node is not ClassDeclarationSyntax classDecl)
            return null;

        if (ctx.SemanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol classSymbol)
            return null;

        if (!InheritsFromJsonSerializerContext(classSymbol))
            return null;

        var serializableTypes = new List<string>();

        foreach (var attr in classSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() != WellKnownTypes.JsonSerializableAttribute)
                continue;

            if (attr.ConstructorArguments is { Length: >= 1 } args &&
                args[0].Value is ITypeSymbol typeArg)
            {
                var typeFqn = "global::" + typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    .Replace("global::", "");
                serializableTypes.Add(typeFqn);
            }
        }

        if (serializableTypes.Count == 0)
            return null;

        return new JsonContextInfo(
            new EquatableArray<string>([.. serializableTypes]));
    }

    private static bool InheritsFromJsonSerializerContext(INamedTypeSymbol symbol)
    {
        var current = symbol.BaseType;
        while (current is not null)
        {
            if (current.ToDisplayString() == WellKnownTypes.JsonSerializerContext)
                return true;
            current = current.BaseType;
        }

        return false;
    }
}
