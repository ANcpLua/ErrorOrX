namespace DiagnosticsDemos.Demos;

public class SearchParams
{
    public string? Query { get; set; }
    public int Page { get; set; } = 1;
}

public sealed record FilterParams(string? Name, int? MinPrice, int? MaxPrice);

/// <summary>
///     EOE017: Nullable [AsParameters] not supported — [AsParameters] cannot be applied to nullable types
///     because parameter expansion requires a concrete instance.
/// </summary>
/// <remarks>
///     The model binder cannot expand a null object's properties, so [AsParameters]
///     types must be non-nullable.
/// </remarks>
public static class EOE017_NullableAsParameters
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE017: Nullable class with [AsParameters]
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Get("/search")]
    // public static ErrorOr<string> Search([AsParameters] SearchParams? p)
    //     => $"Query: {p?.Query}";

    // -------------------------------------------------------------------------
    // TRIGGERS EOE017: Nullable record with [AsParameters]
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Get("/filter")]
    // public static ErrorOr<string> Filter([AsParameters] FilterParams? f)
    //     => $"Name: {f?.Name}";

    // -------------------------------------------------------------------------
    // FIXED: Use non-nullable [AsParameters] type
    // -------------------------------------------------------------------------
    [Get("/api/eoe017/search")]
    public static ErrorOr<string> Search([AsParameters] SearchParams p)
    {
        return $"Query: {p.Query ?? "all"}, Page: {p.Page}";
    }

    // -------------------------------------------------------------------------
    // FIXED: Non-nullable record with nullable properties
    // -------------------------------------------------------------------------
    [Get("/api/eoe017/filter")]
    public static ErrorOr<string> Filter([AsParameters] FilterParams f)
    {
        return $"Name: {f.Name ?? "any"}, Price: {f.MinPrice}-{f.MaxPrice}";
    }

    [Get("/api/eoe017/optional")]
    public static ErrorOr<string> Optional([AsParameters] OptionalParams p)
    {
        return $"Query={p.Query ?? "all"}, Page={p.Page ?? 1}, Size={p.PageSize}";
    }

    [Get("/api/eoe017/defaults")]
    public static ErrorOr<string> WithDefaults([AsParameters] DefaultParams p)
    {
        return $"Query={p.Query}, Page={p.Page}, Size={p.PageSize}";
    }

    // -------------------------------------------------------------------------
    // TIP: Make properties nullable, not the container
    // -------------------------------------------------------------------------
    public sealed record OptionalParams(
        string? Query = null, // Property is nullable (optional)
        int? Page = null, // Property is nullable with null default
        int PageSize = 10); // Property has non-null default

    // -------------------------------------------------------------------------
    // TIP: Use default values in records for optional parameters
    // -------------------------------------------------------------------------
    public sealed record DefaultParams(
        string Query = "default",
        int Page = 1,
        int PageSize = 10);
}
