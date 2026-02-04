// EOE016: Nested [AsParameters] not supported
// =============================================
// [AsParameters] types cannot contain nested [AsParameters] properties.
// Recursive parameter expansion is not supported.
//
// The model binder cannot recursively expand nested [AsParameters] types.
// Flatten your parameter types or use separate parameters.

namespace DiagnosticsDemos.Demos;

// Invalid: Inner type with [AsParameters]
public class InnerParams
{
    public int Page { get; set; }
    public int PageSize { get; set; }
}

// Invalid: Outer type that nests [AsParameters]
// Note: [AsParameters] is not valid on properties, only parameters
// This demonstrates the concept - the diagnostic is triggered when
// the endpoint parameter type contains a property that the binder
// would try to recursively expand
public class NestedOuterParams
{
    public string? Query { get; set; }

    // In practice, nested expansion would need custom model binding
    public InnerParams Pagination { get; set; } = new();
}

// Valid: Flattened params
public class FlattenedParams
{
    public string? Query { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public static class EOE016_NestedAsParameters
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE016: Nested [AsParameters] property
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Get("/search")]
    // public static ErrorOr<string> Search([AsParameters] NestedOuterParams p)
    //     => $"Query: {p.Query}, Page: {p.Pagination.Page}";

    // -------------------------------------------------------------------------
    // FIXED: Flatten the parameter type
    // -------------------------------------------------------------------------
    [Get("/api/eoe016/search")]
    public static ErrorOr<string> Search([AsParameters] FlattenedParams p)
    {
        return $"Query: {p.Query}, Page: {p.Page}, Size: {p.PageSize}";
    }

    // -------------------------------------------------------------------------
    // FIXED: Use separate parameters
    // -------------------------------------------------------------------------
    [Get("/api/eoe016/search-separate")]
    public static ErrorOr<string> SearchSeparate(
        [FromQuery] string? query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        return $"Query: {query}, Page: {page}, Size: {pageSize}";
    }

    [Get("/api/eoe016/composed")]
    public static ErrorOr<string> Composed([AsParameters] ComposedParams p)
    {
        return $"Query: {p.Query}, Page: {p.Page}, Size: {p.PageSize}";
    }

    [Get("/api/eoe016/record")]
    public static ErrorOr<string> SearchWithRecord([AsParameters] SearchParams p)
    {
        return $"Query={p.Query}, Page={p.Page}, Size={p.PageSize}, Sort={p.SortBy}, Desc={p.Descending}";
    }

    // -------------------------------------------------------------------------
    // FIXED: Use composition WITHOUT [AsParameters] on nested type
    // -------------------------------------------------------------------------
    public class ComposedParams
    {
        public string? Query { get; set; }

        // This is fine - InnerParams is NOT marked with [AsParameters]
        // But note: these won't bind automatically from query string
        // You'd need to manually construct or use FromQuery on individual props
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    // -------------------------------------------------------------------------
    // TIP: Records make flattened params easy to define
    // -------------------------------------------------------------------------
    public record SearchParams(
        string? Query = null,
        int Page = 1,
        int PageSize = 10,
        string? SortBy = null,
        bool Descending = false);
}
