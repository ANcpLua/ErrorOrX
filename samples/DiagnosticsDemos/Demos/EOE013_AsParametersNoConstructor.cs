// EOE013: [AsParameters] type has no constructor
// ================================================
// [AsParameters] type has no accessible constructor.
//
// The model binder needs to instantiate the [AsParameters] type, which requires
// a public parameterless constructor, or a constructor where all parameters
// can be bound from the request.

namespace DiagnosticsDemos.Demos;

// Invalid: Only private constructor
public class PrivateConstructorParams
{
    private PrivateConstructorParams() { }
    public string? Query { get; set; }
}

// Invalid: Constructor with unbindable parameter
public class UnbindableConstructorParams
{
    private readonly object _internalState;

    public UnbindableConstructorParams(object internalState)
    {
        _internalState = internalState;
    }

    public string? Query { get; set; }
}

// Valid: Public parameterless constructor
public class PublicConstructorParams
{
    public string? Query { get; set; }
    public int Page { get; set; } = 1;
}

// Valid: Primary constructor record (parameters match property names)
public record RecordParams(string? Query, int Page = 1);

public static class EOE013_AsParametersNoConstructor
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE013: Type with only private constructor
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Get("/search")]
    // public static ErrorOr<string> Search([AsParameters] PrivateConstructorParams p)
    //     => $"Query: {p.Query}";

    // -------------------------------------------------------------------------
    // FIXED: Use types with public parameterless constructors
    // -------------------------------------------------------------------------
    [Get("/api/eoe013/search")]
    public static ErrorOr<string> Search([AsParameters] PublicConstructorParams p)
        => $"Query: {p.Query}, Page: {p.Page}";

    // -------------------------------------------------------------------------
    // FIXED: Records work great with [AsParameters]
    // -------------------------------------------------------------------------
    [Get("/api/eoe013/items")]
    public static ErrorOr<string> GetItems([AsParameters] RecordParams p)
        => $"Query: {p.Query}, Page: {p.Page}";

    // -------------------------------------------------------------------------
    // FIXED: Struct with parameterless constructor
    // -------------------------------------------------------------------------
    public struct StructParams
    {
        public string? Query { get; set; }
        public int Page { get; set; }
    }

    [Get("/api/eoe013/struct-search")]
    public static ErrorOr<string> StructSearch([AsParameters] StructParams p)
        => $"Query: {p.Query}, Page: {p.Page}";

    // -------------------------------------------------------------------------
    // TIP: Records with default values work well
    // -------------------------------------------------------------------------
    public record PaginationParams(
        string? Query = null,
        int Page = 1,
        int PageSize = 10,
        string SortBy = "Id",
        bool Descending = false);

    [Get("/api/eoe013/paginated")]
    public static ErrorOr<string> GetPaginated([AsParameters] PaginationParams p)
        => $"Query={p.Query}, Page={p.Page}, Size={p.PageSize}, Sort={p.SortBy}, Desc={p.Descending}";
}
