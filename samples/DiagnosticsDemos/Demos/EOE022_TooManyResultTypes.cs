// EOE022: Too many result types
// ================================
// Endpoint has too many result types for Results<...> union.
//
// ASP.NET Core's Results<T1, T2, ...> supports up to 6 type parameters.
// If an endpoint can produce more than 6 different response types,
// OpenAPI documentation may be incomplete. This is an informational diagnostic.

namespace DiagnosticsDemos.Demos;

public static class EOE022_TooManyResultTypes
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE022: Endpoint with all 7 error types plus success
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (info):
    //
    // [Get("/many-errors/{id}")]
    // public static ErrorOr<string> GetWithManyErrors(int id)
    // {
    //     if (id == 0) return Error.NotFound("Item.NotFound", "Not found");
    //     if (id == 1) return Error.Validation("Item.Invalid", "Invalid");
    //     if (id == 2) return Error.Conflict("Item.Conflict", "Conflict");
    //     if (id == 3) return Error.Unauthorized("Item.Unauthorized", "Unauthorized");
    //     if (id == 4) return Error.Forbidden("Item.Forbidden", "Forbidden");
    //     if (id == 5) return Error.Failure("Item.Failure", "Failure");
    //     if (id == 6) return Error.Unexpected("Item.Unexpected", "Unexpected");
    //     return $"Item {id}";
    // }

    // -------------------------------------------------------------------------
    // FIXED: Limit error types to what's actually needed
    // -------------------------------------------------------------------------
    // Most endpoints only need 2-3 error types:

    [Get("/api/eoe022/items/{id}")]
    public static ErrorOr<string> GetItem(int id)
    {
        if (id <= 0)
            return Error.Validation("Item.InvalidId", "ID must be positive");

        if (id > 1000)
            return Error.NotFound("Item.NotFound", $"Item {id} not found");

        return $"Item {id}";
    }

    [Post("/api/eoe022/items")]
    public static ErrorOr<string> CreateItem([FromBody] CreateItemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Error.Validation("Item.NameRequired", "Name is required");

        // Simulated duplicate check
        if (request.Name == "existing")
            return Error.Conflict("Item.Duplicate", "Item already exists");

        return $"Created: {request.Name}";
    }

    [Delete("/api/eoe022/items/{id}")]
    public static ErrorOr<Deleted> DeleteItem(int id)
    {
        if (id <= 0)
            return Error.Validation("Item.InvalidId", "ID must be positive");

        if (id > 1000)
            return Error.NotFound("Item.NotFound", $"Item {id} not found");

        return Result.Deleted;
    }

    // -------------------------------------------------------------------------
    // TIP: Group related errors into fewer categories
    // -------------------------------------------------------------------------
    // Instead of different error types for each validation failure,
    // use Validation for all input errors:

    [Put("/api/eoe022/items/{id}")]
    public static ErrorOr<string> UpdateItem(int id, [FromBody] CreateItemRequest request)
    {
        // All validation errors use the same error type
        if (id <= 0)
            return Error.Validation("Item.InvalidId", "ID must be positive");
        if (string.IsNullOrWhiteSpace(request.Name))
            return Error.Validation("Item.NameRequired", "Name is required");
        if (request.Name.Length > 100)
            return Error.Validation("Item.NameTooLong", "Name cannot exceed 100 characters");

        // Business logic errors
        if (id > 1000)
            return Error.NotFound("Item.NotFound", $"Item {id} not found");

        return $"Updated {id}: {request.Name}";
    }

    // -------------------------------------------------------------------------
    // FIXED: Typical CRUD operations need few error types
    // -------------------------------------------------------------------------
    public record CreateItemRequest(string Name);
}
