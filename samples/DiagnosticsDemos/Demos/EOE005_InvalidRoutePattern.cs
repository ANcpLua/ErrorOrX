// EOE005: Invalid route pattern
// ===============================
// Route pattern syntax is invalid.
//
// Route patterns must follow ASP.NET Core routing conventions:
// - Parameters use {name} or {name:constraint} syntax
// - Braces must be balanced
// - Parameter names cannot be empty

namespace DiagnosticsDemos.Demos;

public static class EOE005_InvalidRoutePattern
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE005: Unclosed brace in route
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Get("/todos/{id")]
    // public static ErrorOr<string> GetUnclosed(int id) => $"todo {id}";

    // -------------------------------------------------------------------------
    // TRIGGERS EOE005: Unmatched close brace
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Get("/todos/id}")]
    // public static ErrorOr<string> GetUnmatched(int id) => $"todo {id}";

    // -------------------------------------------------------------------------
    // TRIGGERS EOE005: Empty parameter name
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Get("/todos/{}")]
    // public static ErrorOr<string> GetEmpty() => "empty param";

    // -------------------------------------------------------------------------
    // TRIGGERS EOE005: Nested braces
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Get("/todos/{{id}}")]
    // public static ErrorOr<string> GetNested(int id) => $"todo {id}";

    // -------------------------------------------------------------------------
    // FIXED: Valid route patterns
    // -------------------------------------------------------------------------
    [Get("/api/eoe005/todos/{id}")]
    public static ErrorOr<string> GetById(int id) => $"todo {id}";

    [Get("/api/eoe005/items/{id:int}")]
    public static ErrorOr<string> GetWithConstraint(int id) => $"item {id}";

    [Get("/api/eoe005/products/{id:guid}")]
    public static ErrorOr<string> GetWithGuid(Guid id) => $"product {id}";

    // -------------------------------------------------------------------------
    // FIXED: Multiple parameters
    // -------------------------------------------------------------------------
    [Get("/api/eoe005/users/{userId}/orders/{orderId}")]
    public static ErrorOr<string> GetUserOrder(int userId, int orderId)
        => $"Order {orderId} for user {userId}";

    // -------------------------------------------------------------------------
    // FIXED: Optional parameters (use ? suffix)
    // -------------------------------------------------------------------------
    [Get("/api/eoe005/search/{query?}")]
    public static ErrorOr<string> Search(string? query = null)
        => $"Searching for: {query ?? "all"}";

    // -------------------------------------------------------------------------
    // FIXED: Catch-all parameters (use * or ** prefix)
    // -------------------------------------------------------------------------
    [Get("/api/eoe005/files/{*path}")]
    public static ErrorOr<string> GetFile(string path) => $"File: {path}";
}
