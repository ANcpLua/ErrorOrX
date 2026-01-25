using Microsoft.AspNetCore.Mvc;

namespace ErrorOrX.Sample;

/// <summary>
///     Todo API demonstrating ErrorOr endpoints with automatic error inference.
/// </summary>
public static class TodoApi
{
    /// <summary>
    ///     Get all todos.
    /// </summary>
    [Get("/api/todos")]
    public static Task<ErrorOr<List<Todo>>> GetAll([FromServices] ITodoService svc, CancellationToken ct) => svc.GetAllAsync(ct);

    /// <summary>
    ///     Get todo by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the todo item.</param>
    [Get("/api/todos/{id:guid}")]
    public static Task<ErrorOr<Todo>> GetById([FromRoute] Guid id, [FromServices] ITodoService svc,
        CancellationToken ct) =>
        svc.GetByIdAsync(id, ct);

    /// <summary>
    ///     Create a new todo.
    /// </summary>
    /// <param name="request">The todo creation request containing title and description.</param>
    [Post("/api/todos")]
    public static Task<ErrorOr<Todo>> Create(
        [FromBody] CreateTodoRequest request,
        [FromServices] ITodoService svc,
        CancellationToken ct) =>
        svc.CreateAsync(request, ct);

    /// <summary>
    ///     Update a todo.
    /// </summary>
    [Put("/api/todos/{id:guid}")]
    public static Task<ErrorOr<Updated>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateTodoRequest request,
        [FromServices] ITodoService svc,
        CancellationToken ct) =>
        svc.UpdateAsync(id, request, ct);

    /// <summary>
    ///     Delete a todo.
    /// </summary>
    [Delete("/api/todos/{id:guid}")]
    public static Task<ErrorOr<Deleted>> Delete([FromRoute] Guid id, [FromServices] ITodoService svc,
        CancellationToken ct) =>
        svc.DeleteAsync(id, ct);
}
