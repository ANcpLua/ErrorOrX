using Microsoft.CodeAnalysis;

namespace ErrorOr.Analyzers;

public static partial class Descriptors
{
    /// <summary>
    ///     EOE022 — endpoint exceeds <c>Results&lt;T1..Tn&gt;</c> arity limit (or uses <c>Error.Custom(...)</c>),
    ///     forcing the generator to emit untyped <c>Task&lt;IResult&gt;</c>. The endpoint still works at
    ///     runtime, but OpenAPI metadata for the additional outcomes is lost — clients cannot see the
    ///     status codes or response shapes the handler can return. Severity is Warning (was Info, which
    ///     was invisible in normal builds) so users notice when their endpoint loses typed OpenAPI.
    /// </summary>
    public static readonly DiagnosticDescriptor TooManyResultTypes = new(
        "EOE022",
        "Endpoint exceeds typed Results arity — OpenAPI metadata is incomplete",
        "Endpoint '{0}' has {1} response outcomes (Results<...> supports {2}) or uses Error.Custom(...). " +
        "Generator emits Task<IResult> instead of the typed union — OpenAPI documentation will be missing " +
        "status codes and response shapes for this endpoint. Fix by: (a) reducing the variety of errors the " +
        "handler returns, (b) replacing Error.Custom with one of the built-in ErrorTypes, or " +
        "(c) accepting the loss and adding [ProducesResponseType] attributes manually if OpenAPI matters.",
        Category,
        DiagnosticSeverity.Warning,
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
