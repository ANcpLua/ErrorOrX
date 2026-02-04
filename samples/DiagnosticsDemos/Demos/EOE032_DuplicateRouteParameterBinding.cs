// EOE032: Duplicate route parameter binding
// ===========================================
// Multiple method parameters bind to the same route parameter name.
// Only the first parameter will be used for route binding.
//
// When two parameters have the same name (case-insensitive) and both could
// bind to a route parameter, only the first one is used.

namespace DiagnosticsDemos.Demos;

public static class EOE032_DuplicateRouteParameterBinding
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE032: Two parameters with same name
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Get("/items/{id}")]
    // public static ErrorOr<string> GetItem(int id, [FromQuery] string id) // Duplicate 'id'
    //     => $"Item {id}";

    // -------------------------------------------------------------------------
    // TRIGGERS EOE032: Case-insensitive match
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Get("/users/{userId}")]
    // public static ErrorOr<string> GetUser(int userId, [FromQuery] string UserId) // Same name
    //     => $"User {userId}";

    // -------------------------------------------------------------------------
    // FIXED: Use distinct parameter names
    // -------------------------------------------------------------------------
    [Get("/api/eoe032/items/{id}")]
    public static ErrorOr<string> GetItem(int id, [FromQuery] string? filter)
    {
        return $"Item {id}, filter: {filter ?? "none"}";
    }

    // -------------------------------------------------------------------------
    // FIXED: Use [FromRoute] with explicit name for clarity
    // -------------------------------------------------------------------------
    [Get("/api/eoe032/users/{userId}/orders/{orderId}")]
    public static ErrorOr<string> GetUserOrder(
        [FromRoute] int userId,
        [FromRoute] int orderId,
        [FromQuery] bool includeDetails = false)
    {
        return $"User {userId}, Order {orderId}, Details: {includeDetails}";
    }

    // -------------------------------------------------------------------------
    // FIXED: Different sources are OK even if parameter names "look" similar
    // -------------------------------------------------------------------------
    [Get("/api/eoe032/products/{productId}")]
    public static ErrorOr<string> GetProduct(
        int productId, // From route
        [FromQuery] int? categoryId, // From query
        [FromHeader(Name = "X-User-Id")] string? userId) // From header
    {
        return $"Product {productId}, Category: {categoryId}, User: {userId}";
    }

    // -------------------------------------------------------------------------
    // TIP: Parameter naming conventions
    // -------------------------------------------------------------------------
    // - Route: Use specific names (userId, orderId, productId)
    // - Query: Use descriptive names (filter, page, sortBy)
    // - Header: Standard header names with [FromHeader(Name = "...")]
    // - Service: Use interface type for self-documentation
}
