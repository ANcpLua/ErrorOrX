using Microsoft.CodeAnalysis;

namespace ErrorOr.Analyzers;

public static partial class Descriptors
{
    /// <summary>EOE003 — route template has a parameter that no method parameter captures.</summary>
    public static readonly DiagnosticDescriptor RouteParameterNotBound = new(
        "EOE003",
        "Route parameter not bound",
        "Route '{0}' has parameter '{{{1}}}' but no method parameter captures it",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>EOE004 — same route + HTTP method registered by multiple handlers (generator-only, cross-file).</summary>
    public static readonly DiagnosticDescriptor DuplicateRoute = new(
        "EOE004",
        "Duplicate route",
        "Route '{0} {1}' is already registered by '{2}.{3}'",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>EOE005 — route pattern syntax is invalid.</summary>
    public static readonly DiagnosticDescriptor InvalidRoutePattern = new(
        "EOE005",
        "Invalid route pattern",
        "Route pattern '{0}' is invalid: {1}",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>EOE020 — route constraint type does not match the bound method parameter (e.g., {id:int} bound to a Guid).</summary>
    public static readonly DiagnosticDescriptor RouteConstraintTypeMismatch = new(
        "EOE020",
        "Route constraint type mismatch",
        "Route parameter '{{{0}:{1}}}' expects {2}, but method parameter '{3}' is {4}",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>EOE032 — multiple method parameters bind to the same route parameter; only the first is used.</summary>
    public static readonly DiagnosticDescriptor DuplicateRouteParameterBinding = new(
        "EOE032",
        "Duplicate route parameter binding",
        "Multiple parameters bind to route parameter '{0}'. Only the first parameter ('{1}') will be bound; '{2}' will be ignored.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
