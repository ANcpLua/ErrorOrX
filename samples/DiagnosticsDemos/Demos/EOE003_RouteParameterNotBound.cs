namespace DiagnosticsDemos.Demos;

/// <summary>
/// EOE003: Route parameter not bound — Route template has a parameter that no method parameter captures.
/// </summary>
/// <remarks>
/// When a route contains {paramName}, there must be a corresponding method
/// parameter that will receive the route value.
/// </remarks>
public static class EOE003_RouteParameterNotBound
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE003: Route has {id} but no method parameter captures it
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Get("/todos/{id}")]
    // public static ErrorOr<string> GetById() => "missing id parameter";

    // -------------------------------------------------------------------------
    // TRIGGERS EOE003: Route has {id:int} constraint but no parameter
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Get("/items/{id:int}")]
    // public static ErrorOr<string> GetItem() => "missing constrained parameter";

    // -------------------------------------------------------------------------
    // TRIGGERS EOE003: Multiple unbound route parameters
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Get("/users/{userId}/orders/{orderId}")]
    // public static ErrorOr<string> GetOrder() => "missing both parameters";

    // -------------------------------------------------------------------------
    // FIXED: Add method parameter matching route parameter name
    // -------------------------------------------------------------------------
    [Get("/api/eoe003/todos/{id}")]
    public static ErrorOr<string> GetById(int id)
    {
        return $"Todo {id}";
    }

    // -------------------------------------------------------------------------
    // FIXED: Parameter name must match (case-insensitive)
    // -------------------------------------------------------------------------
    [Get("/api/eoe003/users/{userId}/orders/{orderId}")]
    public static ErrorOr<string> GetOrder(int userId, int orderId)
    {
        return $"Order {orderId} for user {userId}";
    }

    // -------------------------------------------------------------------------
    // FIXED: Using [FromRoute] with different parameter name
    // -------------------------------------------------------------------------
    [Get("/api/eoe003/products/{productId}")]
    public static ErrorOr<string> GetProduct([FromRoute(Name = "productId")] int id)
    {
        return $"Product {id}";
    }
}
