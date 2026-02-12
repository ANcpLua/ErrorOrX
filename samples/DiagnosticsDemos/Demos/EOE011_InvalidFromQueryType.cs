namespace DiagnosticsDemos.Demos;

// Complex filter class - cannot be used with [FromQuery]
public class NestedFilter
{
    public string Name { get; set; } = string.Empty;
    public SubFilter Sub { get; set; } = new();
}

public class SubFilter
{
    public int Min { get; set; }
    public int Max { get; set; }
}

// Flat filter class - CAN be used with [AsParameters]
public class FlatFilter
{
    public string? Name { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// EOE011: Invalid [FromQuery] type — [FromQuery] parameter type is not a supported primitive or collection of primitives.
/// </summary>
/// <remarks>
/// Query string values can only bind to primitive types or arrays/lists of primitives.
/// Complex nested objects cannot be bound from query strings without [AsParameters].
/// </remarks>
public static class EOE011_InvalidFromQueryType
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE011: Complex nested type with [FromQuery]
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Get("/todos")]
    // public static ErrorOr<string> Search([FromQuery] NestedFilter filter)
    //     => $"Filter: {filter.Name}";

    // -------------------------------------------------------------------------
    // FIXED: Use primitive types with [FromQuery]
    // -------------------------------------------------------------------------
    [Get("/api/eoe011/search")]
    public static ErrorOr<string> Search(
        [FromQuery] string? query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        return $"Searching '{query}' - page {page}, size {pageSize}";
    }

    // -------------------------------------------------------------------------
    // FIXED: Arrays of primitives work with [FromQuery]
    // -------------------------------------------------------------------------
    [Get("/api/eoe011/filter")]
    public static ErrorOr<string> Filter(
        [FromQuery] int[] ids,
        [FromQuery] string[] tags)
    {
        return $"Filtering {ids.Length} ids with {tags.Length} tags";
    }

    // -------------------------------------------------------------------------
    // FIXED: Use [AsParameters] for flat objects
    // -------------------------------------------------------------------------
    [Get("/api/eoe011/items")]
    public static ErrorOr<string> GetItems([AsParameters] FlatFilter filter)
    {
        return $"Getting items: name={filter.Name}, page={filter.Page}, size={filter.PageSize}";
    }

    // -------------------------------------------------------------------------
    // FIXED: Nullable primitives work fine
    // -------------------------------------------------------------------------
    [Get("/api/eoe011/optional")]
    public static ErrorOr<string> Optional(
        [FromQuery] int? minPrice,
        [FromQuery] int? maxPrice,
        [FromQuery] DateTime? since)
    {
        return $"Min: {minPrice}, Max: {maxPrice}, Since: {since}";
    }

    // -------------------------------------------------------------------------
    // NOTE: Enums need explicit string parsing in route/query
    // -------------------------------------------------------------------------
    // Enums are not automatically bindable in Minimal APIs without custom binder.
    // Use string parameters with Enum.TryParse if needed:
    //
    // [Get("/api/eoe011/sorted")]
    // public static ErrorOr<string> Sorted([FromQuery] string order = "Ascending")
    // {
    //     if (!Enum.TryParse<SortOrder>(order, out var parsed))
    //         return Error.Validation("Invalid.Order", "Invalid sort order");
    //     return $"Order: {parsed}";
    // }
}
