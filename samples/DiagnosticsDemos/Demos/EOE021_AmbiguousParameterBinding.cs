// EOE021: Ambiguous parameter binding
// =====================================
// Complex type parameter on bodyless/custom method requires explicit binding attribute.
//
// GET, DELETE, HEAD, and OPTIONS methods cannot have implicit body binding.
// When you have a complex type parameter on these methods, you must specify
// how to bind it: [AsParameters], [FromBody], or [FromServices].

namespace DiagnosticsDemos.Demos;

// Complex types used in examples
public class SearchFilter
{
    public string? Query { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class DeleteOptions
{
    public bool Force { get; set; }
    public bool Cascade { get; set; }
}

// Service interface - will be auto-detected
public interface ISearchService
{
    string Search(string query);
}

public static class EOE021_AmbiguousParameterBinding
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE021: Complex type on GET without binding attribute
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Get("/search")]
    // public static ErrorOr<string> Search(SearchFilter filter)
    //     => $"Query: {filter.Query}";

    // -------------------------------------------------------------------------
    // TRIGGERS EOE021: Complex type on DELETE without binding attribute
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Delete("/items/{id}")]
    // public static ErrorOr<Deleted> DeleteItem(int id, DeleteOptions options)
    //     => Result.Deleted;

    // -------------------------------------------------------------------------
    // FIXED: Use [AsParameters] to expand properties as query parameters
    // -------------------------------------------------------------------------
    [Get("/api/eoe021/search")]
    public static ErrorOr<string> SearchWithAsParameters([AsParameters] SearchFilter filter)
    {
        return $"Query: {filter.Query}, Page: {filter.Page}, Size: {filter.PageSize}";
    }

    // -------------------------------------------------------------------------
    // FIXED: Use [FromServices] for dependency injection
    // -------------------------------------------------------------------------
    [Get("/api/eoe021/search-service")]
    public static ErrorOr<string> SearchWithService(
        [FromQuery] string query,
        [FromServices] ISearchService service)
    {
        return service.Search(query);
    }

    // -------------------------------------------------------------------------
    // FIXED: Use individual [FromQuery] parameters
    // -------------------------------------------------------------------------
    [Get("/api/eoe021/search-explicit")]
    public static ErrorOr<string> SearchExplicit(
        [FromQuery] string? query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        return $"Query: {query}, Page: {page}, Size: {pageSize}";
    }

    // -------------------------------------------------------------------------
    // FIXED: DELETE with individual parameters
    // -------------------------------------------------------------------------
    [Delete("/api/eoe021/items/{id}")]
    public static ErrorOr<Deleted> DeleteItemFixed(
        int id,
        [FromQuery] bool force = false,
        [FromQuery] bool cascade = false)
    {
        // Delete logic here
        return Result.Deleted;
    }

    // -------------------------------------------------------------------------
    // NOTE: POST/PUT/PATCH can have implicit body binding
    // -------------------------------------------------------------------------
    [Post("/api/eoe021/items")]
    public static ErrorOr<string> CreateItem(SearchFilter request) // Implicitly [FromBody]
    {
        return $"Created with query: {request.Query}";
    }

    [Put("/api/eoe021/items/{id}")]
    public static ErrorOr<string> UpdateItem(int id, SearchFilter request) // Implicitly [FromBody]
    {
        return $"Updated {id} with query: {request.Query}";
    }

    // -------------------------------------------------------------------------
    // NOTE: Interface types are auto-detected as services
    // -------------------------------------------------------------------------
    [Get("/api/eoe021/auto-service")]
    public static ErrorOr<string> AutoService(
        [FromQuery] string query,
        ISearchService service) // Auto-detected as [FromServices]
    {
        return service.Search(query);
    }
}
