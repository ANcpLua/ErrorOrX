using ANcpLua.Roslyn.Utilities;
using ErrorOr.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ErrorOr.Analyzers;

/// <summary>
///     Route pattern validation and route-constraint type checking. Hosts <c>EOE005</c>
///     (pattern syntax) and <c>EOE020</c> (constraint vs. CLR-type mismatch) for the
///     <see cref="ErrorOrEndpointAnalyzer"/>.
/// </summary>
public sealed partial class ErrorOrEndpointAnalyzer
{
    /// <summary>
    ///     Validates route constraint types match method parameter types (EOE020).
    /// </summary>
    private static void ValidateConstraintTypes(
        in SymbolAnalysisContext context,
        ImmutableArray<RouteParameterInfo> routeParams,
        IReadOnlyDictionary<string, RouteMethodParameterInfo> methodParamsByRouteName,
        Location attributeLocation)
    {
        foreach (var rp in routeParams)
            ValidateSingleRouteConstraint(in context, rp, methodParamsByRouteName, attributeLocation);
    }

    /// <summary>
    ///     Validates a single route parameter constraint against its bound method parameter.
    /// </summary>
    private static void ValidateSingleRouteConstraint(
        in SymbolAnalysisContext context,
        RouteParameterInfo rp,
        IReadOnlyDictionary<string, RouteMethodParameterInfo> methodParamsByRouteName,
        Location attributeLocation)
    {
        // Skip if no constraint or not bound to a method parameter
        if (rp.Constraint is not { } constraint ||
            !methodParamsByRouteName.TryGetValue(rp.Name, out var mp))
        {
            return;
        }

        if (mp.TypeFqn is not { } typeFqn) return;

        // Skip format-only constraints
        if (IsFormatOnlyConstraint(constraint)) return;

        // Validate based on constraint type
        if (rp.IsCatchAll)
            ValidateCatchAllConstraint(in context, rp, mp, typeFqn, attributeLocation);
        else
            ValidateTypedConstraint(in context, rp, constraint, mp, typeFqn, attributeLocation);
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
        in SymbolAnalysisContext context,
        RouteParameterInfo rp,
        RouteMethodParameterInfo mp,
        string typeFqn,
        Location attributeLocation)
    {
        if (!IsStringType(typeFqn))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.RouteConstraintTypeMismatch,
                attributeLocation,
                rp.Name,
                "*",
                "string",
                mp.Name,
                NormalizeTypeName(typeFqn)));
        }
    }

    /// <summary>
    ///     Validates that a typed constraint matches the bound parameter type.
    ///     Uses shared RouteValidator.ConstraintToTypes to avoid duplication.
    /// </summary>
    private static void ValidateTypedConstraint(
        in SymbolAnalysisContext context,
        RouteParameterInfo rp,
        string constraint,
        RouteMethodParameterInfo mp,
        string typeFqn,
        Location attributeLocation)
    {
        // Look up expected types for this constraint using shared RouteValidator
        if (!RouteValidator.ConstraintToTypes.TryGetValue(constraint,
                out var expectedTypes))
        {
            return; // Unknown constraint (e.g., custom) - skip validation
        }

        // Get the actual type, unwrapping Nullable<T> for optional parameters
        var actualTypeFqn = typeFqn.UnwrapNullable(rp.IsOptional || mp.IsNullable);

        // Check if actual type matches any expected type
        if (!DoesTypeMatchConstraint(actualTypeFqn, expectedTypes))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.RouteConstraintTypeMismatch,
                attributeLocation,
                rp.Name,
                constraint,
                expectedTypes[0],
                mp.Name,
                NormalizeTypeName(typeFqn)));
        }
    }

    /// <summary>
    ///     Checks if an actual type matches any of the expected types for a constraint.
    /// </summary>
    private static bool DoesTypeMatchConstraint(string actualTypeFqn, IEnumerable<string> expectedTypes)
    {
        foreach (var expected in expectedTypes)
        {
            if (TypeNamesMatch(actualTypeFqn, expected))
                return true;
        }

        return false;
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
        {
            if (!paramNames.Add(rp.Name))
                issues.Add($"Route contains duplicate parameter '{{{rp.Name}}}'");
        }

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
