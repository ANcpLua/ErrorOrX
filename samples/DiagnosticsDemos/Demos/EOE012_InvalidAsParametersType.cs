// EOE012: Invalid [AsParameters] type
// =====================================
// [AsParameters] used on non-class/struct type.
//
// [AsParameters] expands a class or struct's properties into individual parameters.
// It cannot be used with primitive types or interfaces.

namespace DiagnosticsDemos.Demos;

// Valid: Class type for [AsParameters]
public class SearchParamsClass
{
    public string? Query { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

// Valid: Struct type for [AsParameters]
public struct SearchParamsStruct
{
    public string? Query { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

// Valid: Record type for [AsParameters]
public record SearchParamsRecord(string? Query, int Page = 1, int PageSize = 10);

public static class EOE012_InvalidAsParametersType
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE012: [AsParameters] on primitive type
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Get("/search")]
    // public static ErrorOr<string> Search([AsParameters] int page)
    //     => $"Page {page}";

    // -------------------------------------------------------------------------
    // TRIGGERS EOE012: [AsParameters] on string
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Get("/find")]
    // public static ErrorOr<string> Find([AsParameters] string query)
    //     => $"Query: {query}";

    // -------------------------------------------------------------------------
    // FIXED: Use [AsParameters] with class types
    // -------------------------------------------------------------------------
    [Get("/api/eoe012/search-class")]
    public static ErrorOr<string> SearchWithClass([AsParameters] SearchParamsClass @params)
        => $"Searching '{@params.Query}' - page {@params.Page}, size {@params.PageSize}";

    // -------------------------------------------------------------------------
    // FIXED: Use [AsParameters] with struct types
    // -------------------------------------------------------------------------
    [Get("/api/eoe012/search-struct")]
    public static ErrorOr<string> SearchWithStruct([AsParameters] SearchParamsStruct @params)
        => $"Searching '{@params.Query}' - page {@params.Page}, size {@params.PageSize}";

    // -------------------------------------------------------------------------
    // FIXED: Use [AsParameters] with record types
    // -------------------------------------------------------------------------
    [Get("/api/eoe012/search-record")]
    public static ErrorOr<string> SearchWithRecord([AsParameters] SearchParamsRecord @params)
        => $"Searching '{@params.Query}' - page {@params.Page}, size {@params.PageSize}";

    // -------------------------------------------------------------------------
    // FIXED: For primitive parameters, use [FromQuery] or [FromRoute]
    // -------------------------------------------------------------------------
    [Get("/api/eoe012/paged")]
    public static ErrorOr<string> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        => $"Page {page}, size {pageSize}";
}
