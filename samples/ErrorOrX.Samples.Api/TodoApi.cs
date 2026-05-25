namespace ErrorOrX.Samples.Api;

public static class TodoApi
{
    [Get("/api/todos")]
    public static Task<ErrorOr<List<Todo>>> GetAll(ITodoService svc, CancellationToken ct)
        => svc.GetAllAsync(ct);

    [Get("/api/todos/{id:guid}")]
    public static Task<ErrorOr<Todo>> GetById(Guid id, ITodoService svc, CancellationToken ct)
        => svc.GetByIdAsync(id, ct);

    [Post("/api/todos")]
    public static Task<ErrorOr<Todo>> Create(CreateTodoRequest request, ITodoService svc, CancellationToken ct)
        => svc.CreateAsync(request, ct);

    [Put("/api/todos/{id:guid}")]
    public static Task<ErrorOr<Updated>> Update(Guid id, UpdateTodoRequest request, ITodoService svc,
        CancellationToken ct)
        => svc.UpdateAsync(id, request, ct);

    [Delete("/api/todos/{id:guid}")]
    public static Task<ErrorOr<Deleted>> Delete(Guid id, ITodoService svc, CancellationToken ct)
        => svc.DeleteAsync(id, ct);
}
