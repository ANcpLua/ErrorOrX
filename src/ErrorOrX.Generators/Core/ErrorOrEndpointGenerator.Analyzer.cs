using System.Collections.Immutable;
using ErrorOr.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ErrorOr.Generators;

/// <summary>
///     Analyzer partial for OpenAPI and AOT validation (EOE022-EOE026).
/// </summary>
public sealed partial class ErrorOrEndpointGenerator
{
    private static void AnalyzeJsonContextCoverage(
        SourceProductionContext spc,
        ImmutableArray<EndpointDescriptor> endpoints,
        ImmutableArray<JsonContextInfo> userContexts,
        bool publishAot)
    {
        if (endpoints.IsDefaultOrEmpty)
            return;

        // Collect body types and response types separately
        var bodyTypes = new Dictionary<string, string>(); // typeFqn -> endpointName
        var responseTypes = new Dictionary<string, string>();

        foreach (var ep in endpoints)
        {
            // Collect body parameter types
            foreach (var param in ep.HandlerParameters)
            {
                if (param.Source == ParameterSource.Body)
                    if (!bodyTypes.ContainsKey(param.TypeFqn))
                        bodyTypes[param.TypeFqn] = ep.HandlerMethodName;
            }

            // Collect response types
            if (!string.IsNullOrEmpty(ep.SuccessTypeFqn))
            {
                var successInfo = ResultsUnionTypeBuilder.GetSuccessResponseInfo(
                    ep.SuccessTypeFqn,
                    ep.SuccessKind,
                    ep.IsAcceptedResponse);

                if (successInfo.HasBody && !responseTypes.ContainsKey(ep.SuccessTypeFqn))
                    responseTypes[ep.SuccessTypeFqn] = ep.HandlerMethodName;
            }
        }

        // Always need ProblemDetails for error responses
        if (!responseTypes.ContainsKey(WellKnownTypes.Fqn.ProblemDetails))
            responseTypes[WellKnownTypes.Fqn.ProblemDetails] = "ErrorOr endpoints";
        if (!responseTypes.ContainsKey(WellKnownTypes.Fqn.HttpValidationProblemDetails))
            responseTypes[WellKnownTypes.Fqn.HttpValidationProblemDetails] = "ErrorOr endpoints";

        // CRITICAL: If no user-defined JsonSerializerContext exists but we have body parameters,
        // this is an ERROR in AOT mode. The generated ErrorOrJsonContext cannot be used by
        // System.Text.Json source generator because Roslyn generators cannot see output from
        // other generators.
        // Only emit EOE026 when PublishAot=true to avoid false positives in non-AOT builds.
        if (userContexts.IsDefaultOrEmpty)
        {
            // Report EOE026 for each body type - but only if PublishAot is enabled
            if (publishAot)
            {
                foreach (var kvp in bodyTypes)
                {
                    if (kvp.Key.IsPrimitiveJsonType())
                        continue;

                    var displayType = kvp.Key.StripGlobalPrefix();
                    spc.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.MissingJsonContextForBody,
                        Location.None,
                        kvp.Value,
                        displayType));
                }
            }

            // No point checking type registration if there's no context at all
            return;
        }

        // User has a JsonSerializerContext - check if all needed types are registered
        var registeredTypes = new HashSet<string>();
        foreach (var ctx in userContexts)
        {
            foreach (var typeFqn in ctx.SerializableTypes)
            registeredTypes.Add(typeFqn);
        }

        // Combine all needed types
        var allNeededTypes = new Dictionary<string, string>();
        foreach (var kvp in bodyTypes)
        {
            if (!allNeededTypes.ContainsKey(kvp.Key))
                allNeededTypes[kvp.Key] = kvp.Value;
        }

        foreach (var kvp in responseTypes)
        {
            if (!allNeededTypes.ContainsKey(kvp.Key))
                allNeededTypes[kvp.Key] = kvp.Value;
        }

        foreach (var kvp in allNeededTypes)
        {
            if (kvp.Key.IsPrimitiveJsonType())
                continue;

            var isRegistered = registeredTypes.Any(rt => kvp.Key.TypeNamesEqual(rt));
            if (!isRegistered)
            {
                var displayType = kvp.Key.StripGlobalPrefix();

                spc.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.TypeNotInJsonContext,
                    Location.None,
                    displayType,
                    kvp.Value));
            }
        }
    }

    /// <summary>
    ///     Analyzes endpoints for union type arity violations (EOE022).
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
            var hasBodyBinding = ep.HasBodyOrFormBinding;
            var baseCount = 3 + (hasBodyBinding ? 1 : 0);

            var errorTypeCount = 0;
            if (!ep.InferredErrorTypeNames.IsDefaultOrEmpty)
            {
                foreach (var type in ep.InferredErrorTypeNames.AsImmutableArray().Distinct())
                {
                    // Failure/Unexpected map to InternalServerError (500), which is already in baseCount
                    if (type is ErrorMapping.Failure or ErrorMapping.Unexpected)
                        continue;

                    // All others (Validation, NotFound, Conflict, etc.) map to distinct types
                    errorTypeCount++;
                }
            }

            // Total unique types
            var totalTypes = baseCount + errorTypeCount;

            if (totalTypes > maxArity || !ep.InferredCustomErrors.IsDefaultOrEmpty)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.TooManyResultTypes,
                    Location.None,
                    $"{ep.HandlerContainingTypeFqn}.{ep.HandlerMethodName}",
                    totalTypes,
                    maxArity));
            }
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
        {
            return ImmutableArray<JsonContextInfo>.Empty;
        }

        // Cache attributes to avoid multiple GetAttributes() calls
        var attributes = classSymbol.GetAttributes();

        // Extract all JsonSerializable types
        var serializableTypes = new List<string>();
        foreach (var attr in attributes)
        {
            if (attr.AttributeClass?.ToDisplayString() != WellKnownTypes.JsonSerializableAttribute)
                continue;

            if (attr.ConstructorArguments is [{ Value: ITypeSymbol typeArg }, ..])
                serializableTypes.Add(typeArg.GetFullyQualifiedName());
        }

        // Check for PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase
        var hasCamelCasePolicy = false;
        foreach (var attr in attributes)
        {
            if (attr.AttributeClass?.ToDisplayString() != WellKnownTypes.JsonSourceGenerationOptionsAttribute)
                continue;

            foreach (var namedArg in attr.NamedArguments)
            {
                if (namedArg.Key != "PropertyNamingPolicy")
                    continue;

                var enumValue = namedArg.Value.Value;
                if (namedArg.Value.Type is INamedTypeSymbol enumType &&
                    enumType.GetMembers("CamelCase").FirstOrDefault() is IFieldSymbol camelCaseField &&
                    camelCaseField.ConstantValue?.Equals(enumValue) == true)
                {
                    hasCamelCasePolicy = true;
                    break;
                }
            }

            if (hasCamelCasePolicy)
                break;
        }

        if (serializableTypes.Count is 0)
            return ImmutableArray<JsonContextInfo>.Empty;

        var className = classSymbol.Name;
        var namespaceName = classSymbol.ContainingNamespace?.IsGlobalNamespace == true
            ? null
            : classSymbol.ContainingNamespace?.ToDisplayString();

        return
        [
            new JsonContextInfo(
                className,
                namespaceName,
                new EquatableArray<string>([.. serializableTypes]),
                hasCamelCasePolicy)
        ];
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
