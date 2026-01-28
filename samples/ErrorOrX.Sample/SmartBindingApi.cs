namespace ErrorOrX.Sample;

/// <summary>
///     Demonstrates Smart Parameter Binding - the generator infers [FromBody], [FromRoute], [FromServices]
///     automatically based on HTTP method and parameter types.
/// </summary>
public static class SmartBindingApi
{
    /// <summary>
    ///     GET with route parameter - 'id' automatically bound from route.
    /// </summary>
    [Get("/api/smart/todos/{id:guid}")]
    public static Task<ErrorOr<Todo>> GetTodo(Guid id, ITodoService svc, CancellationToken ct)
    {
        return svc.GetByIdAsync(id, ct);
        // ✅ id → Route, svc → Service, ct → Auto-detected
    }

    /// <summary>
    ///     POST with complex type - 'request' automatically bound from body.
    /// </summary>
    [Post("/api/smart/todos")]
    public static Task<ErrorOr<Todo>> CreateTodo(CreateTodoRequest request, ITodoService svc, CancellationToken ct)
    {
        return svc.CreateAsync(request, ct);
        // ✅ request → Body (POST + complex type)
    }

    /// <summary>
    ///     PUT with route parameter and body - both inferred automatically.
    /// </summary>
    [Put("/api/smart/todos/{id:guid}")]
    public static Task<ErrorOr<Updated>> UpdateTodo(Guid id, UpdateTodoRequest request, ITodoService svc,
        CancellationToken ct)
    {
        return svc.UpdateAsync(id, request, ct);
        // ✅ id → Route, request → Body, svc → Service
    }

    /// <summary>
    ///     DELETE with route parameter.
    /// </summary>
    [Delete("/api/smart/todos/{id:guid}")]
    public static Task<ErrorOr<Deleted>> DeleteTodo(Guid id, ITodoService svc, CancellationToken ct)
    {
        return svc.DeleteAsync(id, ct);
        // ✅ All parameters inferred
    }

    /// <summary>
    ///     PATCH method - partially updates a todo.
    ///     Demonstrates that PATCH works identically to PUT for smart binding.
    /// </summary>
    [Patch("/api/smart/todos/{id:guid}/title")]
    public static async Task<ErrorOr<Updated>> PatchTodoTitle(
        Guid id,
        CreateTodoRequest request, // Only Title field will be used
        ITodoService svc,
        CancellationToken ct)
    {
        var result = await svc.GetByIdAsync(id, ct);
        if (result.IsError)
            return result.Errors.ToArray();

        var updated = new UpdateTodoRequest(request.Title, result.Value.IsComplete);
        return await svc.UpdateAsync(id, updated, ct);
    }
}

/*
 * Why Smart Binding Works:
 *
 * 1. Route Parameters:
 *    - Parameter name matches route template {id} → Inferred as [FromRoute]
 *
 * 2. Service Types:
 *    - Interface types (ITodoService) → Inferred as [FromServices]
 *    - Special types (CancellationToken, HttpContext) → Auto-detected
 *
 * 3. Body Parameters:
 *    - POST/PUT/PATCH + complex type → Inferred as [FromBody]
 *    - GET/DELETE + complex type → ERROR (ambiguous, requires explicit attribute)
 *
 * 4. Primitives:
 *    - Not in route → Inferred as [FromQuery]
 */