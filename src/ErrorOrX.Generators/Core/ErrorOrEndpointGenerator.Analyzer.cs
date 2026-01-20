using System.Collections.Immutable;
using ANcpLua.Roslyn.Utilities;
using ErrorOr.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ErrorOr.Generators;

/// <summary>
///     Analyzer partial for OpenAPI and AOT validation (EOE021-EOE022).
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
            if (!string.IsNullOrEmpty(ep.SuccessTypeFqn))
            {
                var successInfo = ResultsUnionTypeBuilder.GetSuccessResponseInfo(
                    ep.SuccessTypeFqn,
                    ep.SuccessKind,
                    ep.IsAcceptedResponse);

                if (successInfo.HasBody && neededTypes.Add(ep.SuccessTypeFqn))
                    typeToEndpoint[ep.SuccessTypeFqn] = ep.HandlerMethodName;
            }

            foreach (var param in ep.HandlerParameters)
                if (param.Source == EndpointParameterSource.Body)
                    if (neededTypes.Add(param.TypeFqn))
                        typeToEndpoint[param.TypeFqn] = ep.HandlerMethodName;
        }

        neededTypes.Add(WellKnownTypes.Fqn.ProblemDetails);
        neededTypes.Add(WellKnownTypes.Fqn.HttpValidationProblemDetails);

        if (userContexts.IsDefaultOrEmpty)
            return;

        var registeredTypes = new HashSet<string>();
        foreach (var ctx in userContexts)
        foreach (var typeFqn in ctx.SerializableTypes)
            registeredTypes.Add(typeFqn);

        foreach (var neededType in neededTypes)
        {
            if (TypeNameHelper.IsPrimitiveJsonType(neededType))
                continue;

            var isRegistered = registeredTypes.Any(rt => TypeNameHelper.TypeNamesMatch(neededType, rt));
            if (!isRegistered)
            {
                var displayType = TypeNameHelper.StripGlobalPrefix(neededType);
                var endpointName = typeToEndpoint.TryGetValue(neededType, out var epName)
                    ? epName
                    : "ErrorOr endpoints";

                spc.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.TypeNotInJsonContext,
                    Location.None,
                    displayType,
                    endpointName));
            }
        }
    }

    /// <summary>
    ///     Analyzes endpoints for union type arity violations (EOE030).
    ///     Reports Info diagnostic when endpoint exceeds Results&lt;...&gt; max arity.
    /// </summary>
    private static void AnalyzeUnionTypeArity(
        SourceProductionContext spc,
        ImmutableArray<EndpointDescriptor> endpoints,
        int maxArity)
    {
        foreach (var ep in endpoints)
        {
            // Count possible response types:
            // 1. Success type (1)
            // 2. BadRequest for binding (1)
            // 3. InternalServerError safety net (1)
            // 4. UnsupportedMediaType if body present (1)
            var hasBodyBinding = HasBodyParam(ep) || HasFormParams(ep);
            var baseCount = 3 + (hasBodyBinding ? 1 : 0);

            var errorTypeCount = 0;
            if (!ep.InferredErrorTypeNames.IsDefaultOrEmpty)
                foreach (var type in ep.InferredErrorTypeNames.AsImmutableArray().Distinct())
                {
                    // Failure/Unexpected map to InternalServerError (500), which is already in baseCount
                    if (type is ErrorMapping.Failure or ErrorMapping.Unexpected)
                        continue;

                    // All others (Validation, NotFound, Conflict, etc.) map to distinct types
                    errorTypeCount++;
                }

            // Total unique types
            var totalTypes = baseCount + errorTypeCount;

            if (totalTypes > maxArity || !ep.InferredCustomErrors.IsDefaultOrEmpty)
                spc.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.TooManyResultTypes,
                    Location.None,
                    $"{ep.HandlerContainingTypeFqn}.{ep.HandlerMethodName}",
                    totalTypes,
                    maxArity));
        }
    }
}

internal static class JsonContextProvider
{
    public static IncrementalValuesProvider<JsonContextInfo> Create(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
                static (ctx, _) => TransformJsonContext(ctx));

        return provider.SelectMany(static (info, _) => info);
    }

    private static ImmutableArray<JsonContextInfo> TransformJsonContext(GeneratorSyntaxContext ctx)
    {
        if (ctx.Node is not ClassDeclarationSyntax classDecl ||
            ctx.SemanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol classSymbol ||
            !InheritsFromJsonSerializerContext(classSymbol))
            return ImmutableArray<JsonContextInfo>.Empty;

        var serializableTypes = new List<string>();
        var hasCamelCasePolicy = false;

        foreach (var attr in classSymbol.GetAttributes())
        {
            var attrName = attr.AttributeClass?.ToDisplayString();

            if (attrName == WellKnownTypes.JsonSerializableAttribute)
            {
                if (attr.ConstructorArguments is [{ Value: ITypeSymbol typeArg } _, ..])
                {
                    var typeFqn = "global::" + typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                        .Replace("global::", "");
                    serializableTypes.Add(typeFqn);
                }
            }
            else if (attrName == WellKnownTypes.JsonSourceGenerationOptionsAttribute)
                // Check for PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase
                foreach (var namedArg in attr.NamedArguments)
                {
                    if (namedArg.Key != "PropertyNamingPolicy") continue;

                    var enumValue = namedArg.Value.Value;
                    if (namedArg.Value.Type is INamedTypeSymbol enumType &&
                        enumType.GetMembers("CamelCase").FirstOrDefault() is IFieldSymbol camelCaseField &&
                        camelCaseField.ConstantValue?.Equals(enumValue) == true)
                    {
                        hasCamelCasePolicy = true;
                        break;
                    }
                }
        }

        if (serializableTypes.Count is 0)
            return ImmutableArray<JsonContextInfo>.Empty;

        var className = classSymbol.Name;
        var namespaceName = classSymbol.ContainingNamespace?.IsGlobalNamespace == true
            ? null
            : classSymbol.ContainingNamespace?.ToDisplayString();

        return ImmutableArray.Create(new JsonContextInfo(
            className,
            namespaceName,
            new EquatableArray<string>([.. serializableTypes]),
            hasCamelCasePolicy));
    }

    /// <summary>
    ///     Checks if a type inherits from JsonSerializerContext by walking the base type chain.
    /// </summary>
    private static bool InheritsFromJsonSerializerContext(ITypeSymbol symbol)
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
