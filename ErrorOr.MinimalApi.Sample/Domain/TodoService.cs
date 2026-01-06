using ErrorOr.Http.Bcl.Sample.Infrastructure.Messaging;

namespace ErrorOr.Http.Bcl.Sample.Domain;

public sealed class TodoService : ITodoService
{
    private readonly List<Todo> _todos =
    [
        new(Guid.NewGuid(), "Walk the dog"),
        new(Guid.NewGuid(), "Do the dishes", new DateOnly(2025, 1, 15))
    ];

    private readonly IMessagePublisher? _publisher;
    private readonly ISseStream<TodoCreatedEvent>? _createdStream;
    private readonly ISseStream<TodoCompletedEvent>? _completedStream;
    private readonly TimeProvider _time;

    public TodoService(
        IMessagePublisher? publisher = null,
        ISseStream<TodoCreatedEvent>? createdStream = null,
        ISseStream<TodoCompletedEvent>? completedStream = null,
        TimeProvider? time = null)
    {
        _publisher = publisher;
        _createdStream = createdStream;
        _completedStream = completedStream;
        _time = time ?? TimeProvider.System;
    }

    public Task<ErrorOr<List<Todo>>> GetAllAsync(CancellationToken ct = default)
    {
        return Task.FromResult<ErrorOr<List<Todo>>>(_todos.ToList());
    }

    public Task<ErrorOr<Todo>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return Task.FromResult<ErrorOr<Todo>>(
            _todos.Find(t => t.Id == id) is { } todo
                ? todo
                : Error.NotFound("Todo.NotFound", $"Todo {id} not found"));
    }

    public async Task<ErrorOr<Todo>> CreateAsync(CreateTodoRequest request, CancellationToken ct = default)
    {
        var todo = new Todo(Guid.NewGuid(), request.Title, request.DueBy);
        _todos.Add(todo);

        var evt = new TodoCreatedEvent(todo.Id, todo.Title, _time.GetUtcNow());
        if (_publisher is not null)
            await _publisher.PublishAsync("todo.created", evt, ct);

        _createdStream?.Publish(evt);

        return todo;
    }

    public async Task<ErrorOr<Updated>> UpdateAsync(Guid id, UpdateTodoRequest request, CancellationToken ct = default)
    {
        var index = _todos.FindIndex(t => t.Id == id);
        if (index < 0)
            return Error.NotFound("Todo.NotFound", $"Todo {id} not found");

        var existing = _todos[index];
        var wasComplete = existing.IsComplete;

        _todos[index] = existing with
        {
            Title = request.Title,
            DueBy = request.DueBy,
            IsComplete = request.IsComplete
        };

        if (!wasComplete && request.IsComplete)
        {
            var evt = new TodoCompletedEvent(id, request.Title, _time.GetUtcNow());
            if (_publisher is not null)
                await _publisher.PublishAsync("todo.completed", evt, ct);

            _completedStream?.Publish(evt);
        }

        return new Updated();
    }

    public async Task<ErrorOr<Deleted>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var removed = _todos.RemoveAll(t => t.Id == id) > 0;
        if (!removed)
            return Error.NotFound("Todo.NotFound", $"Todo {id} not found");

        if (_publisher is not null)
            await _publisher.PublishAsync("todo.deleted", new TodoDeletedEvent(id, _time.GetUtcNow()), ct);

        return new Deleted();
    }
}
