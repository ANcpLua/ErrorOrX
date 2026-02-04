// EOE023: Unknown error factory
// ================================
// Error factory method is not a known ErrorType.
//
// The generator recognizes these Error factory methods:
// - Error.Failure, Error.Unexpected, Error.Validation
// - Error.Conflict, Error.NotFound, Error.Unauthorized, Error.Forbidden
//
// Error.Custom() creates errors with custom types that may not map to
// standard HTTP status codes, so a warning is issued.

namespace DiagnosticsDemos.Demos;

public static class EOE023_UnknownErrorFactory
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE023: Using Error.Custom with unknown type
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Get("/custom/{id}")]
    // public static ErrorOr<string> GetWithCustomError(int id)
    // {
    //     if (id < 0)
    //         return Error.Custom(999, "Custom.Error", "Custom error description");
    //     return $"Item {id}";
    // }

    // -------------------------------------------------------------------------
    // TRIGGERS EOE023: Custom type that doesn't map to known HTTP status
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Get("/special/{id}")]
    // public static ErrorOr<string> GetWithSpecialError(int id)
    // {
    //     if (id == 42)
    //         return Error.Custom(418, "Teapot", "I'm a teapot");
    //     return $"Item {id}";
    // }

    // -------------------------------------------------------------------------
    // FIXED: Use known error factory methods
    // -------------------------------------------------------------------------
    [Get("/api/eoe023/items/{id}")]
    public static ErrorOr<string> GetItem(int id)
    {
        if (id < 0)
            return Error.Validation("Item.InvalidId", "ID cannot be negative");

        if (id == 0)
            return Error.NotFound("Item.NotFound", "Item not found");

        return $"Item {id}";
    }

    [Post("/api/eoe023/items")]
    public static ErrorOr<string> CreateItem([FromBody] CreateRequest request)
    {
        // Input validation -> Validation
        if (string.IsNullOrWhiteSpace(request.Name))
            return Error.Validation("Item.NameRequired", "Name is required");

        // Duplicate check -> Conflict
        if (request.Email == "existing@example.com")
            return Error.Conflict("Item.EmailExists", "Email already registered");

        return $"Created: {request.Name}";
    }

    [Delete("/api/eoe023/items/{id}")]
    public static ErrorOr<Deleted> DeleteItem(int id, [FromHeader(Name = "X-User-Id")] string? userId)
    {
        // Authentication check -> Unauthorized
        if (string.IsNullOrEmpty(userId))
            return Error.Unauthorized("Auth.Required", "Authentication required");

        // Authorization check -> Forbidden
        if (userId != "admin")
            return Error.Forbidden("Auth.AdminOnly", "Admin access required");

        // Resource check -> NotFound
        if (id > 1000)
            return Error.NotFound("Item.NotFound", $"Item {id} not found");

        return Result.Deleted;
    }

    // -------------------------------------------------------------------------
    // FIXED: Use standard error types for common scenarios
    // -------------------------------------------------------------------------
    public record CreateRequest(string Name, string Email);

    // -------------------------------------------------------------------------
    // Known ErrorType to HTTP status mapping:
    // -------------------------------------------------------------------------
    // Error.Validation   -> 400 Bad Request
    // Error.Unauthorized -> 401 Unauthorized
    // Error.Forbidden    -> 403 Forbidden
    // Error.NotFound     -> 404 Not Found
    // Error.Conflict     -> 409 Conflict
    // Error.Failure      -> 500 Internal Server Error
    // Error.Unexpected   -> 500 Internal Server Error
    // Error.Custom(*)    -> Warning: unknown mapping
}
