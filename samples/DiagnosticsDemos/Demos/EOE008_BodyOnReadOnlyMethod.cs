namespace DiagnosticsDemos.Demos;

public record SearchCriteria(string Query, int Page);

/// <summary>
/// EOE008: Body on read-only HTTP method — GET, HEAD, DELETE, OPTIONS should not have request bodies per HTTP semantics.
/// </summary>
/// <remarks>
/// While technically possible, using request bodies with these methods is discouraged
/// as some proxies and clients may strip or ignore them. This is a warning, not an error.
/// </remarks>
public static class EOE008_BodyOnReadOnlyMethod
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE008: GET with [FromBody]
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Get("/search")]
    // public static ErrorOr<string> SearchWithBody([FromBody] SearchCriteria criteria)
    //     => $"Searching: {criteria.Query}";

    // -------------------------------------------------------------------------
    // TRIGGERS EOE008: DELETE with [FromBody]
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Delete("/items")]
    // public static ErrorOr<Deleted> DeleteWithBody([FromBody] SearchCriteria criteria)
    //     => Result.Deleted;

    // -------------------------------------------------------------------------
    // FIXED: Use query parameters for GET
    // -------------------------------------------------------------------------
    [Get("/api/eoe008/search")]
    public static ErrorOr<string> Search(
        [FromQuery] string query,
        [FromQuery] int page = 1)
    {
        return $"Searching '{query}' on page {page}";
    }

    [Get("/api/eoe008/advanced-search")]
    public static ErrorOr<string> AdvancedSearch([AsParameters] SearchParams @params)
    {
        return $"Searching '{@params.Query}' page {@params.Page}";
    }

    // -------------------------------------------------------------------------
    // FIXED: Use route parameters for DELETE
    // -------------------------------------------------------------------------
    [Delete("/api/eoe008/items/{id}")]
    public static ErrorOr<Deleted> DeleteItem(int id)
    {
        return Result.Deleted;
    }

    // -------------------------------------------------------------------------
    // FIXED: Use POST/PUT for operations that need request bodies
    // -------------------------------------------------------------------------
    [Post("/api/eoe008/search")]
    public static ErrorOr<string> SearchWithPost([FromBody] SearchCriteria criteria)
    {
        return $"Searching: {criteria.Query}, page {criteria.Page}";
    }

    // -------------------------------------------------------------------------
    // NOTE: HEAD follows GET semantics (no body)
    // ErrorOrX does not have a [Head] attribute - use [Get] with appropriate response
    // -------------------------------------------------------------------------
    [Get("/api/eoe008/check/{id}")]
    public static ErrorOr<Success> CheckExists(int id)
    {
        return Result.Success;
    }

    // -------------------------------------------------------------------------
    // FIXED: Use [AsParameters] for complex query objects on GET
    // -------------------------------------------------------------------------
    public record SearchParams(string Query, int Page, int PageSize);
}
