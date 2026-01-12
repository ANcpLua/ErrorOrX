namespace ErrorOrX.Sample.Domain;

public sealed class TodoService : ITodoService
{
    private readonly List<Todo> _todos =
    [
        new(Guid.NewGuid(), "Walk the dog"),
        new(Guid.NewGuid(), "Do the dishes")
    ];

    public async Task<ErrorOr<List<Todo>>> GetAllAsync(CancellationToken ct)
    {
        await Task.Delay(10, ct); // Simulate I/O
        return _todos.ToList();
    }

    public async Task<ErrorOr<Todo>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        await Task.Delay(10, ct); // Simulate I/O
        return _todos.Find(t => t.Id == id) is { } todo
            ? todo
            : Error.NotFound("Todo.NotFound", $"Todo {id} not found");
    }

    public async Task<ErrorOr<Todo>> CreateAsync(CreateTodoRequest request, CancellationToken ct)
    {
        await Task.Delay(10, ct); // Simulate I/O
        var todo = new Todo(Guid.NewGuid(), request.Title);
        _todos.Add(todo);
        return todo;
    }

    public async Task<ErrorOr<Updated>> UpdateAsync(Guid id, UpdateTodoRequest request, CancellationToken ct)
    {
        await Task.Delay(10, ct); // Simulate I/O
        var index = _todos.FindIndex(t => t.Id == id);
        if (index < 0)
            return Error.NotFound("Todo.NotFound", $"Todo {id} not found");

        _todos[index] = _todos[index] with
        {
            Title = request.Title,
            IsComplete = request.IsComplete
        };

        return new Updated();
    }

    public async Task<ErrorOr<Deleted>> DeleteAsync(Guid id, CancellationToken ct)
    {
        await Task.Delay(10, ct); // Simulate I/O
        var removed = _todos.RemoveAll(t => t.Id == id) > 0;
        return removed
            ? new Deleted()
            : Error.NotFound("Todo.NotFound", $"Todo {id} not found");
    }
}
