// EOE004: Duplicate route
// ========================
// Same route + HTTP method registered by multiple handlers.
//
// Each combination of HTTP method and route pattern must be unique.
// This diagnostic is reported by the generator (not the analyzer) because
// it requires cross-file analysis.

namespace DiagnosticsDemos.Demos;

public static class EOE004_DuplicateRoute
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE004: Two GET handlers with same route
    // -------------------------------------------------------------------------
    // Uncomment both to see the diagnostic:
    //
    // [Get("/duplicate")]
    // public static ErrorOr<string> GetFirst() => "first";
    //
    // [Get("/duplicate")]
    // public static ErrorOr<string> GetSecond() => "second";

    // -------------------------------------------------------------------------
    // FIXED: Use different routes for each handler
    // -------------------------------------------------------------------------
    [Get("/api/eoe004/items")]
    public static ErrorOr<string> GetAllItems() => "all items";

    [Get("/api/eoe004/items/{id}")]
    public static ErrorOr<string> GetItemById(int id) => $"item {id}";

    // -------------------------------------------------------------------------
    // FIXED: Same route with different HTTP methods is OK
    // -------------------------------------------------------------------------
    [Get("/api/eoe004/resources")]
    public static ErrorOr<string> GetResources() => "get resources";

    [Post("/api/eoe004/resources")]
    public static ErrorOr<string> CreateResource() => "created resource";

    [Put("/api/eoe004/resources/{id}")]
    public static ErrorOr<string> UpdateResource(int id) => $"updated {id}";

    [Delete("/api/eoe004/resources/{id}")]
    public static ErrorOr<Deleted> DeleteResource(int id) => Result.Deleted;
}
