using Microsoft.CodeAnalysis;

namespace ErrorOr.Analyzers;

public static partial class Descriptors
{
    /// <summary>
    ///     EOE015 — endpoint returns <c>ErrorOr&lt;object&gt;</c> or <c>ErrorOr&lt;dynamic&gt;</c>.
    ///     Users typically reach this by upcasting an anonymous expression
    ///     (<c>ErrorOr&lt;object&gt; Get() =&gt; new { ... }</c>); the result cannot be serialized
    ///     under Native AOT because <c>object</c> has no JsonTypeInfo unless explicitly registered.
    /// </summary>
    public static readonly DiagnosticDescriptor ObjectReturnTypeNotSupported = new(
        "EOE015",
        "ErrorOr<object> not supported",
        "Method '{0}' returns ErrorOr<object> (or ErrorOr<dynamic>), which cannot be serialized under Native AOT. " +
        "Use a concrete payload type, or register typeof(object) in your JsonSerializerContext and suppress this warning.",
        Category,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>EOE018 — private/protected types cannot appear in endpoint signatures (generated code cannot access them).</summary>
    public static readonly DiagnosticDescriptor InaccessibleTypeNotSupported = new(
        "EOE018",
        "Inaccessible type in endpoint",
        "Type '{0}' used by endpoint '{1}' is {2} and cannot be accessed by generated code. Make it internal or public.",
        Category,
        DiagnosticSeverity.Error,
        true);

    /// <summary>EOE019 — open generic type parameters cannot appear in endpoint return types.</summary>
    public static readonly DiagnosticDescriptor TypeParameterNotSupported = new(
        "EOE019",
        "Type parameter not supported",
        "Method '{0}' uses type parameter '{1}' in return type. Generic type parameters cannot be used with ErrorOr endpoints.",
        Category,
        DiagnosticSeverity.Error,
        true);
}
