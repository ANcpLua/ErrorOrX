using Microsoft.CodeAnalysis;

namespace ErrorOr.Analyzers;

public static partial class Descriptors
{
    /// <summary>
    ///     EOE001 — handler method must return ErrorOr&lt;T&gt;, Task&lt;ErrorOr&lt;T&gt;&gt;, or ValueTask&lt;ErrorOr
    ///     &lt;T&gt;&gt;.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidReturnType = new(
        "EOE001",
        "Invalid return type",
        "Method '{0}' must return ErrorOr<T>, Task<ErrorOr<T>>, or ValueTask<ErrorOr<T>>",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>EOE002 — handler methods must be static for source generation.</summary>
    public static readonly DiagnosticDescriptor NonStaticHandler = new(
        "EOE002",
        "Handler must be static",
        "Method '{0}' must be static. Instance methods cannot be used with ErrorOr.Endpoints source generation.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>EOE033 — handler method name should follow PascalCase convention.</summary>
    public static readonly DiagnosticDescriptor MethodNameNotPascalCase = new(
        "EOE033",
        "Handler method name not PascalCase",
        "Method '{0}' should follow PascalCase naming convention. Consider renaming to '{1}'.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
