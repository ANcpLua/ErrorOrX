// EOE014: Invalid [FromHeader] type
// ====================================
// [FromHeader] with non-string type requires TryParse.
//
// Header values come as strings. To bind to other types, the type must
// be string, a primitive with TryParse, or a collection thereof.

namespace DiagnosticsDemos.Demos;

// Complex type without TryParse - cannot be used with [FromHeader]
public class ComplexHeader
{
    public string Value { get; set; } = string.Empty;
    public int Priority { get; set; }
}

public static class EOE014_InvalidFromHeaderType
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE014: Complex type with [FromHeader]
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Get("/request")]
    // public static ErrorOr<string> GetWithHeader([FromHeader] ComplexHeader header)
    //     => $"Header: {header.Value}";

    // -------------------------------------------------------------------------
    // FIXED: Use string for headers
    // -------------------------------------------------------------------------
    [Get("/api/eoe014/string-header")]
    public static ErrorOr<string> GetWithStringHeader(
        [FromHeader(Name = "X-Request-Id")] string? requestId)
    {
        return $"Request ID: {requestId ?? "none"}";
    }

    // -------------------------------------------------------------------------
    // FIXED: Primitive types with TryParse work
    // -------------------------------------------------------------------------
    [Get("/api/eoe014/int-header")]
    public static ErrorOr<string> GetWithIntHeader(
        [FromHeader(Name = "X-Page-Size")] int pageSize = 10)
    {
        return $"Page size: {pageSize}";
    }

    [Get("/api/eoe014/bool-header")]
    public static ErrorOr<string> GetWithBoolHeader(
        [FromHeader(Name = "X-Include-Metadata")]
        bool includeMetadata = false)
    {
        return $"Include metadata: {includeMetadata}";
    }

    // -------------------------------------------------------------------------
    // FIXED: Collections of strings for multi-value headers
    // -------------------------------------------------------------------------
    [Get("/api/eoe014/array-header")]
    public static ErrorOr<string> GetWithArrayHeader(
        [FromHeader(Name = "Accept-Language")] string[]? languages)
    {
        return $"Languages: {string.Join(", ", languages ?? [])}";
    }

    // -------------------------------------------------------------------------
    // FIXED: Common header patterns
    // -------------------------------------------------------------------------
    [Get("/api/eoe014/common-headers")]
    public static ErrorOr<string> GetWithCommonHeaders(
        [FromHeader(Name = "Authorization")] string? authorization,
        [FromHeader(Name = "X-Correlation-Id")]
        string? correlationId,
        [FromHeader(Name = "Accept")] string? accept,
        [FromHeader(Name = "Content-Type")] string? contentType)
    {
        return $"Auth: {authorization?.Substring(0, 10) ?? "none"}..., Correlation: {correlationId}";
    }

    // -------------------------------------------------------------------------
    // FIXED: Nullable primitive headers with defaults
    // -------------------------------------------------------------------------
    [Get("/api/eoe014/optional-headers")]
    public static ErrorOr<string> GetWithOptionalHeaders(
        [FromHeader(Name = "X-Priority")] int? priority,
        [FromHeader(Name = "X-Timeout")] int timeout = 30)
    {
        return $"Priority: {priority?.ToString() ?? "default"}, Timeout: {timeout}s";
    }
}
