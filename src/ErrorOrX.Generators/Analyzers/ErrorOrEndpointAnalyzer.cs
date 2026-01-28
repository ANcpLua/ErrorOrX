using System.Collections.Immutable;
using ANcpLua.Roslyn.Utilities.Matching;
using ErrorOr.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SymbolMatch = ANcpLua.Roslyn.Utilities.Matching.Match;

namespace ErrorOr.Analyzers;

/// <summary>
///     Real-time Roslyn analyzer for ErrorOr.Endpoints endpoints.
///     Provides immediate IDE feedback for common issues.
/// </summary>
/// <remarks>
///     This analyzer handles single-method diagnostics that can run fast.
///     Cross-file diagnostics (EOE004, EOE007, EOE008) remain in the generator.
///     Route constraint mappings are shared via RouteValidator to avoid duplication.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ErrorOrEndpointAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        Descriptors.InvalidReturnType,
        Descriptors.NonStaticHandler,
        Descriptors.RouteParameterNotBound,
        Descriptors.InvalidRoutePattern,
        Descriptors.MultipleBodySources,
        Descriptors.BodyOnReadOnlyMethod,
        Descriptors.AcceptedOnReadOnlyMethod,
        Descriptors.RouteConstraintTypeMismatch
    ];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol method)
            return;

        // Find ErrorOr endpoint attributes
        var endpointAttributes = GetEndpointAttributes(method);
        if (endpointAttributes.Count is 0)
            return;

        // EOE002: Handler must be static
        if (!method.IsStatic)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.NonStaticHandler,
                method.Locations.FirstOrDefault(),
                method.Name));
            return; // Don't report more diagnostics for invalid handler
        }

        // EOE001: Invalid return type
        if (!IsValidReturnType(method.ReturnType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.InvalidReturnType,
                method.Locations.FirstOrDefault(),
                method.Name));
            return; // Don't report more diagnostics for invalid handler
        }

        // Analyze each endpoint attribute
        foreach (var (httpMethod, pattern, attributeLocation) in endpointAttributes)
            AnalyzeEndpoint(context, method, httpMethod, pattern, attributeLocation);
    }

    private static void AnalyzeEndpoint(
        SymbolAnalysisContext context,
        IMethodSymbol method,
        string httpMethod,
        string pattern,
        Location attributeLocation)
    {
        // EOE005: Invalid route pattern
        var patternDiagnostics = ValidateRoutePattern(pattern);
        foreach (var message in patternDiagnostics)
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.InvalidRoutePattern,
                attributeLocation,
                pattern,
                message));

        // If pattern is invalid, skip further route analysis
        if (patternDiagnostics.Count > 0)
            return;

        // Extract route parameters with constraints
        var routeParams = ExtractRouteParametersWithConstraints(pattern);
        if (!routeParams.IsDefaultOrEmpty)
        {
            var routeParamNames = routeParams
                .Select(static r => r.Name)
                .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

            var errorOrContext = new ErrorOrContext(context.Compilation);

            var bindingFlow =
                RouteBindingHelper.BindRouteParameters(method, routeParamNames, errorOrContext, httpMethod);
            if (bindingFlow.IsSuccess)
            {
                var bindingAnalysis = bindingFlow.ValueOrDefault();
                var methodParamsByRouteName = RouteValidator.BuildRouteParameterLookup(
                    bindingAnalysis.RouteParameters.AsImmutableArray());

                // EOE003: Route parameter not bound
                foreach (var routeParam in routeParams)
                    if (!methodParamsByRouteName.ContainsKey(routeParam.Name))
                        context.ReportDiagnostic(Diagnostic.Create(
                            Descriptors.RouteParameterNotBound,
                            attributeLocation,
                            pattern,
                            routeParam.Name));

                // EOE023: Route constraint type mismatch
                ValidateConstraintTypes(context, routeParams, methodParamsByRouteName, attributeLocation);
            }
        }

        // EOE006: Multiple body sources
        var bodyCount = CountBodySources(method);
        if (bodyCount > 1)
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.MultipleBodySources,
                method.Locations.FirstOrDefault(),
                method.Name));

        // EOE009: Body on read-only HTTP method
        var hasBody = bodyCount > 0;
        if (hasBody && WellKnownTypes.HttpMethod.IsBodyless(httpMethod))
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.BodyOnReadOnlyMethod,
                attributeLocation,
                method.Name,
                httpMethod.ToUpperInvariant()));

        // EOE010: [AcceptedResponse] on read-only method
        if (HasAcceptedResponseAttribute(method, null) &&
            WellKnownTypes.HttpMethod.IsBodyless(httpMethod))
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.AcceptedOnReadOnlyMethod,
                attributeLocation,
                method.Name,
                httpMethod.ToUpperInvariant()));
    }

    /// <summary>
    ///     Validates route constraint types match method parameter types (EOE023).
    /// </summary>
    private static void ValidateConstraintTypes(
        SymbolAnalysisContext context,
        ImmutableArray<RouteParameterInfo> routeParams,
        IReadOnlyDictionary<string, RouteMethodParameterInfo> methodParamsByRouteName,
        Location attributeLocation)
    {
        foreach (var rp in routeParams)
            ValidateSingleRouteConstraint(context, rp, methodParamsByRouteName, attributeLocation);
    }

    /// <summary>
    ///     Validates a single route parameter constraint against its bound method parameter.
    /// </summary>
    private static void ValidateSingleRouteConstraint(
        SymbolAnalysisContext context,
        RouteParameterInfo rp,
        IReadOnlyDictionary<string, RouteMethodParameterInfo> methodParamsByRouteName,
        Location attributeLocation)
    {
        // Skip if no constraint or not bound to a method parameter
        if (rp.Constraint is not { } constraint ||
            !methodParamsByRouteName.TryGetValue(rp.Name, out var mp))
            return;

        if (mp.TypeFqn is not { } typeFqn)
            return;

        // Skip format-only constraints
        if (IsFormatOnlyConstraint(constraint))
            return;

        // Validate based on constraint type
        if (rp.IsCatchAll)
            ValidateCatchAllConstraint(context, rp, mp, typeFqn, attributeLocation);
        else
            ValidateTypedConstraint(context, rp, constraint, mp, typeFqn, attributeLocation);
    }

    /// <summary>
    ///     Checks if a constraint is format-only and doesn't constrain the CLR type.
    ///     Delegates to shared RouteValidator to avoid duplication.
    /// </summary>
    private static bool IsFormatOnlyConstraint(string constraint)
    {
        return RouteValidator.FormatOnlyConstraints.Contains(constraint);
    }

    /// <summary>
    ///     Validates that a catch-all parameter is bound to a string type.
    /// </summary>
    private static void ValidateCatchAllConstraint(
        SymbolAnalysisContext context,
        RouteParameterInfo rp,
        RouteMethodParameterInfo mp,
        string typeFqn,
        Location attributeLocation)
    {
        if (!IsStringType(typeFqn))
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.RouteConstraintTypeMismatch,
                attributeLocation,
                rp.Name,
                "*",
                "string",
                mp.Name,
                NormalizeTypeName(typeFqn)));
    }

    /// <summary>
    ///     Validates that a typed constraint matches the bound parameter type.
    ///     Uses shared RouteValidator.ConstraintToTypes to avoid duplication.
    /// </summary>
    private static void ValidateTypedConstraint(
        SymbolAnalysisContext context,
        RouteParameterInfo rp,
        string constraint,
        RouteMethodParameterInfo mp,
        string typeFqn,
        Location attributeLocation)
    {
        // Look up expected types for this constraint using shared RouteValidator
        if (!RouteValidator.ConstraintToTypes.TryGetValue(constraint, out var expectedTypes))
            return; // Unknown constraint (e.g., custom) - skip validation

        // Get the actual type, unwrapping Nullable<T> for optional parameters
        var actualTypeFqn = typeFqn.UnwrapNullable(rp.IsOptional || mp.IsNullable);

        // Check if actual type matches any expected type
        if (!DoesTypeMatchConstraint(actualTypeFqn, expectedTypes))
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.RouteConstraintTypeMismatch,
                attributeLocation,
                rp.Name,
                constraint,
                expectedTypes[0],
                mp.Name,
                NormalizeTypeName(typeFqn)));
    }

    /// <summary>
    ///     Checks if an actual type matches any of the expected types for a constraint.
    /// </summary>
    private static bool DoesTypeMatchConstraint(string actualTypeFqn, IEnumerable<string> expectedTypes)
    {
        foreach (var expected in expectedTypes)
            if (TypeNamesMatch(actualTypeFqn, expected))
                return true;
        return false;
    }


    /// <summary>
    ///     Type matchers for body source detection using ANcpLua.Roslyn.Utilities.
    /// </summary>
    private static readonly TypeMatcher StreamMatcher = SymbolMatch.Type().NameContains("Stream");

    private static readonly TypeMatcher PipeReaderMatcher = SymbolMatch.Type().Named("PipeReader");
    private static readonly TypeMatcher FormFileMatcher = SymbolMatch.Type().Named("IFormFile");
    private static readonly TypeMatcher FormFileCollectionMatcher = SymbolMatch.Type().Named("IFormFileCollection");
    private static readonly TypeMatcher FormCollectionMatcher = SymbolMatch.Type().Named("IFormCollection");

    private static bool IsErrorOr(ITypeSymbol type)
    {
        return type is INamedTypeSymbol { Name: "ErrorOr", IsGenericType: true } named &&
               named.ContainingNamespace?.ToDisplayString() is "ErrorOr" or "ErrorOr.Core.ErrorOr";
    }

    /// <summary>
    ///     Type detection using fluent matchers from ANcpLua.Roslyn.Utilities.
    /// </summary>
    private static bool IsStream(ITypeSymbol type)
    {
        return StreamMatcher.Matches(type);
    }

    private static bool IsPipeReader(ITypeSymbol type)
    {
        return PipeReaderMatcher.Matches(type);
    }

    private static bool IsFormFile(ITypeSymbol type)
    {
        return FormFileMatcher.Matches(type);
    }

    private static bool IsFormFileCollection(ITypeSymbol type)
    {
        return FormFileCollectionMatcher.Matches(type);
    }

    private static bool IsFormCollection(ITypeSymbol type)
    {
        return FormCollectionMatcher.Matches(type);
    }

    private static List<(string HttpMethod, string Pattern, Location Location)> GetEndpointAttributes(
        ISymbol method)
    {
        var results = new List<(string, string, Location)>();

        foreach (var attr in method.GetAttributes())
        {
            if (attr.AttributeClass?.Name is not { } attrName)
                continue;

            string? httpMethod = null;
            var pattern = "/";

            switch (attrName)
            {
                // Check for specific HTTP method attributes
                case "GetAttribute" or "Get":
                    httpMethod = WellKnownTypes.HttpMethod.Get;
                    break;
                case "PostAttribute" or "Post":
                    httpMethod = WellKnownTypes.HttpMethod.Post;
                    break;
                case "PutAttribute" or "Put":
                    httpMethod = WellKnownTypes.HttpMethod.Put;
                    break;
                case "DeleteAttribute" or "Delete":
                    httpMethod = WellKnownTypes.HttpMethod.Delete;
                    break;
                case "PatchAttribute" or "Patch":
                    httpMethod = WellKnownTypes.HttpMethod.Patch;
                    break;
                // Generic endpoint attribute - extract HTTP method from first constructor arg
                case "ErrorOrEndpointAttribute" or "ErrorOrEndpoint":
                {
                    if (attr.ConstructorArguments is [{ Value: string m } _, ..])
                        httpMethod = m.ToUpperInvariant();
                    break;
                }
            }

            if (httpMethod is null)
                continue;

            // Extract pattern from constructor arguments
            var patternIndex = attrName.Contains("ErrorOrEndpoint") ? 1 : 0;
            if (attr.ConstructorArguments.Length > patternIndex &&
                attr.ConstructorArguments[patternIndex].Value is string p)
                pattern = p;

            var location = attr.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                           ?? method.Locations.FirstOrDefault()
                           ?? Location.None;

            results.Add((httpMethod, pattern, location));
        }

        return results;
    }

    private static bool IsValidReturnType(ITypeSymbol returnType)
    {
        if (returnType is not INamedTypeSymbol named)
            return false;

        // Direct ErrorOr<T>
        if (IsErrorOr(named))
            return true;

        // Task<ErrorOr<T>> or ValueTask<ErrorOr<T>>
        if (named.Name is "Task" or "ValueTask" && named is { IsGenericType: true, TypeArguments.Length: > 0 })
            return IsErrorOr(named.TypeArguments[0]);

        return false;
    }

    /// <summary>
    ///     Extracts route parameters with their constraints from a route pattern.
    ///     Delegates to RouteValidator which is the single source of truth for route parsing.
    /// </summary>
    private static ImmutableArray<RouteParameterInfo> ExtractRouteParametersWithConstraints(string pattern)
    {
        return RouteValidator.ExtractRouteParameters(pattern);
    }

    private static int CountBodySources(IMethodSymbol method)
    {
        var bodyCount = 0;
        var hasFromForm = false;
        var hasStream = false;

        foreach (var param in method.Parameters)
        {
            // Check for body-related attributes using HasAttribute
            if (param.HasAttribute(WellKnownTypes.FromBodyAttribute))
            {
                bodyCount++;
                continue;
            }

            if (param.HasAttribute(WellKnownTypes.FromFormAttribute))
            {
                hasFromForm = true;
                continue;
            }

            // Check for body-related types using pattern matchers
            if (IsStream(param.Type) || IsPipeReader(param.Type))
                hasStream = true;
            else if (IsFormFile(param.Type) ||
                     IsFormFileCollection(param.Type) ||
                     IsFormCollection(param.Type))
                hasFromForm = true;
        }

        // Multiple [FromBody] is always an error
        if (bodyCount > 1) return bodyCount;

        // Otherwise return number of distinct body source buckets used
        return (bodyCount > 0 ? 1 : 0) + (hasFromForm ? 1 : 0) + (hasStream ? 1 : 0);
    }

    private static bool HasAcceptedResponseAttribute(ISymbol method, INamedTypeSymbol? acceptedResponseAttribute)
    {
        return acceptedResponseAttribute is not null
            ? method.HasAttribute(acceptedResponseAttribute)
            : method.HasAttribute(WellKnownTypes.AcceptedResponseAttribute);
    }

    private static List<string> ValidateRoutePattern(string pattern)
    {
        var issues = new List<string>();

        if (string.IsNullOrWhiteSpace(pattern))
        {
            issues.Add("Route pattern cannot be empty");
            return issues;
        }

        // Strip escaped braces before validation (matches RouteValidator behavior)
        // This prevents false positives for routes like /api/{{version}}/users
        var escapedStripped = pattern.Replace("{{", "").Replace("}}", "");

        // Check for empty parameter names: {}
        if (escapedStripped.Contains("{}"))
            issues.Add("Route contains empty parameter '{}'. Parameter names are required");

        // Check for unclosed braces
        var openCount = escapedStripped.Count(static c => c == '{');
        var closeCount = escapedStripped.Count(static c => c == '}');
        if (openCount != closeCount) issues.Add($"Route has mismatched braces: {openCount} '{{' and {closeCount} '}}'");

        // Check for duplicate parameter names using RouteValidator (single source of truth)
        var paramNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rp in RouteValidator.ExtractRouteParameters(pattern))
            if (!paramNames.Add(rp.Name))
                issues.Add($"Route contains duplicate parameter '{{{rp.Name}}}'");

        return issues;
    }

    private static bool IsStringType(string typeFqn)
    {
        return typeFqn.IsStringType();
    }

    private static bool TypeNamesMatch(string actualFqn, string expected)
    {
        return actualFqn.TypeNamesEqual(expected);
    }

    private static string NormalizeTypeName(string typeFqn)
    {
        return typeFqn.NormalizeTypeName();
    }

}
