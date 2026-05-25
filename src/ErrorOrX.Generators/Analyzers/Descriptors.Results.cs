using Microsoft.CodeAnalysis;

namespace ErrorOr.Analyzers;

public static partial class Descriptors
{
    /// <summary>
    ///     EOE022 — endpoint has more possible response types than Results&lt;...&gt; supports; OpenAPI may be
    ///     incomplete.
    /// </summary>
    public static readonly DiagnosticDescriptor TooManyResultTypes = new(
        "EOE022",
        "Too many result types",
        "Endpoint '{0}' has {1} possible response types, exceeding Results<...> max arity of {2}. OpenAPI documentation may be incomplete.",
        Category,
        DiagnosticSeverity.Info,
        true);

    /// <summary>EOE023 — Error factory method does not map to a known ErrorType.</summary>
    public static readonly DiagnosticDescriptor UnknownErrorFactory = new(
        "EOE023",
        "Unknown error factory",
        "Error.Or factory method '{0}' is not a known ErrorType. Supported types: Failure, Unexpected, Validation, Conflict, NotFound, Unauthorized, Forbidden.",
        Category,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     EOE024 — endpoint calls an interface/abstract method returning ErrorOr&lt;T&gt; without
    ///     [ProducesError]/[ReturnsError]; OpenAPI cannot infer errors through interfaces.
    /// </summary>
    public static readonly DiagnosticDescriptor UndocumentedInterfaceCall = new(
        "EOE024",
        "Undocumented interface call",
        "Endpoint '{0}' calls '{1}' which returns ErrorOr<T> but has no error documentation. " +
        "Add [ProducesError(...)] to the endpoint or [ReturnsError(...)] to the interface method. " +
        "OpenAPI cannot infer errors through interfaces.",
        Category,
        DiagnosticSeverity.Error,
        true);
}
