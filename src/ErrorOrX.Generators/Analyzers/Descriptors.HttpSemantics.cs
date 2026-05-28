using Microsoft.CodeAnalysis;

namespace ErrorOr.Analyzers;

public static partial class Descriptors
{
    /// <summary>EOE006 — endpoint has multiple body sources; only one of [FromBody]/[FromForm]/Stream/PipeReader is allowed.</summary>
    public static readonly DiagnosticDescriptor MultipleBodySources = new(
        "EOE006",
        "Multiple body sources",
        "Endpoint '{0}' has multiple body sources. Use only one of: [FromBody], [FromForm], Stream, or PipeReader.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>EOE008 — GET/HEAD/DELETE/OPTIONS should not carry a request body per HTTP semantics.</summary>
    public static readonly DiagnosticDescriptor BodyOnReadOnlyMethod = new(
        "EOE008",
        "Body on read-only HTTP method",
        "Endpoint '{0}' uses {1} with a request body. Consider using POST/PUT/PATCH instead.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>EOE009 — [AcceptedResponse] (HTTP 202) is for async POST/PUT operations, not GET/DELETE.</summary>
    public static readonly DiagnosticDescriptor AcceptedOnReadOnlyMethod = new(
        "EOE009",
        "[AcceptedResponse] on read-only method",
        "Endpoint '{0}' uses [AcceptedResponse] with {1}. 202 Accepted is typically for async POST/PUT operations.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
