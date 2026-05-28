using Microsoft.CodeAnalysis;

namespace ErrorOr.Analyzers;

public static partial class Descriptors
{
    /// <summary>EOE010 — [FromRoute] parameter type is not a primitive and has no TryParse.</summary>
    public static readonly DiagnosticDescriptor InvalidFromRouteType = new(
        "EOE010",
        "Invalid [FromRoute] type",
        "Parameter '{0}' with [FromRoute] must be a primitive type or implement TryParse. Type '{1}' is not supported.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>EOE011 — [FromQuery] parameter type is not a primitive or collection of primitives.</summary>
    public static readonly DiagnosticDescriptor InvalidFromQueryType = new(
        "EOE011",
        "Invalid [FromQuery] type",
        "Parameter '{0}' with [FromQuery] must be a primitive or collection of primitives. Type '{1}' is not supported.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>EOE012 — [AsParameters] used on a non-class/struct type.</summary>
    public static readonly DiagnosticDescriptor InvalidAsParametersType = new(
        "EOE012",
        "Invalid [AsParameters] type",
        "Parameter '{0}' with [AsParameters] must be a class or struct type, not '{1}'",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>EOE013 — [AsParameters] type has no accessible constructor.</summary>
    public static readonly DiagnosticDescriptor AsParametersNoConstructor = new(
        "EOE013",
        "[AsParameters] type has no constructor",
        "Type '{0}' used with [AsParameters] must have an accessible constructor",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>EOE014 — [FromHeader] type must be string, a primitive with TryParse, or a collection thereof.</summary>
    public static readonly DiagnosticDescriptor InvalidFromHeaderType = new(
        "EOE014",
        "Invalid [FromHeader] type",
        "Parameter '{0}' with [FromHeader] must be string, a primitive with TryParse, or a collection thereof. Type '{1}' is not supported.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>EOE016 — [AsParameters] type has a nested [AsParameters] property; recursive expansion not supported.</summary>
    public static readonly DiagnosticDescriptor NestedAsParametersNotSupported = new(
        "EOE016",
        "Nested [AsParameters] not supported",
        "Type '{0}' used with [AsParameters] has property '{1}' also marked [AsParameters]. Nested parameter expansion is not supported.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>EOE017 — [AsParameters] cannot be applied to a nullable parameter (expansion needs a concrete instance).</summary>
    public static readonly DiagnosticDescriptor NullableAsParametersNotSupported = new(
        "EOE017",
        "Nullable [AsParameters] not supported",
        "Parameter '{0}' with [AsParameters] cannot be nullable. Remove the '?' or use a non-nullable type.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    ///     EOE021 — complex-type parameter on a bodyless verb requires explicit [AsParameters], [FromBody], or
    ///     [FromServices].
    /// </summary>
    public static readonly DiagnosticDescriptor AmbiguousParameterBinding = new(
        "EOE021",
        "Ambiguous parameter binding",
        "Parameter '{0}' of type '{1}' on {2} endpoint requires explicit binding attribute. " +
        "Use [AsParameters] for query/route expansion, [FromBody] to force body binding, " +
        "or [FromServices] for DI injection.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
