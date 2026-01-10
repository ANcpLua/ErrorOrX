using ErrorOr.Core.Errors;

namespace ErrorOr.Endpoints.Sample.Domain;

/// <summary>
///     Interface with [ReturnsError] attributes - the TypeScript-equivalent of union return types.
///     These attributes are automatically read by the generator when endpoints call interface methods.
/// </summary>
public interface ITodoService
{
    /// <summary>Get all todos. No domain errors possible, but server errors can occur.</summary>
    [ReturnsError(ErrorType.Failure, "General.Failure")]
    Task<ErrorOr<List<Todo>>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Get todo by ID. Returns NotFound if not exists.</summary>
    [ReturnsError(ErrorType.NotFound, "Todo.NotFound")]
    Task<ErrorOr<Todo>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Create a new todo. May return validation errors.</summary>
    [ReturnsError(ErrorType.Validation, "Todo.Validation")]
    Task<ErrorOr<Todo>> CreateAsync(CreateTodoRequest request, CancellationToken ct = default);

    /// <summary>Update a todo. Returns NotFound if not exists.</summary>
    [ReturnsError(ErrorType.NotFound, "Todo.NotFound")]
    Task<ErrorOr<Updated>> UpdateAsync(Guid id, UpdateTodoRequest request, CancellationToken ct = default);

    /// <summary>Delete a todo. Returns NotFound if not exists.</summary>
    [ReturnsError(ErrorType.NotFound, "Todo.NotFound")]
    Task<ErrorOr<Deleted>> DeleteAsync(Guid id, CancellationToken ct = default);
}