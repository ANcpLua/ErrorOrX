namespace ErrorOr.Http.Bcl.Sample.Domain;

public interface ITodoService
{
    Task<ErrorOr<List<Todo>>> GetAllAsync(CancellationToken ct = default);

    Task<ErrorOr<Todo>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<ErrorOr<Todo>> CreateAsync(CreateTodoRequest request, CancellationToken ct = default);

    Task<ErrorOr<Updated>> UpdateAsync(Guid id, UpdateTodoRequest request, CancellationToken ct = default);

    Task<ErrorOr<Deleted>> DeleteAsync(Guid id, CancellationToken ct = default);
}