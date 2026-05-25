namespace ErrorOrX.Samples.Api;

public static class AdvancedErrorHandlingApi
{
    [Post("/api/advanced/todos/with-validation")]
    public static Task<ErrorOr<Todo>> CreateWithValidation(
        CreateTodoRequest request,
        ITodoService svc,
        CancellationToken ct)
        => ValidateTitle(request.Title)
            .ThenAsync(_ => svc.CreateAsync(request, ct))
            .Then(static todo => EnrichTodo(todo));

    [Get("/api/advanced/todos/{id:guid}/check")]
    public static async Task<ErrorOr<Todo>> CheckTodoStatus(Guid id, ITodoService svc, CancellationToken ct)
    {
        var result = await svc.GetByIdAsync(id, ct);
        return result.FailIf(
            static todo => !todo.IsComplete,
            Error.Validation("Todo.Incomplete", "Todo must be completed before checking"));
    }

    [Get("/api/advanced/todos/{id:guid}/or-default")]
    public static Task<ErrorOr<Todo>> GetOrDefault(Guid id, ITodoService svc, CancellationToken ct)
        => svc.GetByIdAsync(id, ct)
            .Else(static _ => new Todo(Guid.Empty, "Default Todo"));

    [Get("/api/advanced/todos/{id:guid}/summary")]
    public static async Task<ErrorOr<string>> GetTodoSummary(Guid id, ITodoService svc, CancellationToken ct)
    {
        var result = await svc.GetByIdAsync(id, ct);
        return result.Match(
            static todo => $"Todo '{todo.Title}' is {(todo.IsComplete ? "complete" : "incomplete")}",
            static errors => $"Error: {string.Join(", ", errors.Select(static e => e.Description))}");
    }

    [Get("/api/advanced/todos/{id:guid}/complex")]
    public static async Task<ErrorOr<Todo>> GetWithComplexValidation(Guid id, ITodoService svc, CancellationToken ct)
    {
        var result = await svc.GetByIdAsync(id, ct);
        return result
            .FailIf(static todo => todo.Title.Length > 100,
                Error.Validation("Todo.TitleTooLong", "Title cannot exceed 100 characters"))
            .FailIf(static todo => string.IsNullOrWhiteSpace(todo.Title),
                Error.Validation("Todo.TitleEmpty", "Title cannot be empty"));
    }

    [Put("/api/advanced/todos/{sourceId:guid}/copy-to/{targetId:guid}")]
    public static async Task<ErrorOr<Updated>> CopyTodoTitle(
        Guid sourceId,
        Guid targetId,
        ITodoService svc,
        CancellationToken ct)
    {
        var source = await svc.GetByIdAsync(sourceId, ct);
        if (source.IsError) return source.Errors.ToArray();

        var target = await svc.GetByIdAsync(targetId, ct);
        if (target.IsError) return target.Errors.ToArray();

        return await svc.UpdateAsync(
            targetId,
            new UpdateTodoRequest(source.Value.Title, target.Value.IsComplete),
            ct);
    }

    [Post("/api/advanced/todos/batch")]
    public static async Task<ErrorOr<List<Todo>>> CreateBatch(
        List<CreateTodoRequest> requests,
        ITodoService svc,
        CancellationToken ct)
    {
        switch (requests.Count)
        {
            case > 10: return Error.Validation("Batch.TooLarge", "Cannot create more than 10 todos at once");
            case 0: return Error.Validation("Batch.Empty", "Must provide at least one todo");
        }

        var todos = new List<Todo>();
        foreach (var request in requests)
        {
            var result = await svc.CreateAsync(request, ct);
            if (result.IsError) return result.Errors.ToArray();
            todos.Add(result.Value);
        }

        return todos;
    }

    [Get("/api/advanced/todos/{id:guid}/with-logging")]
    public static async Task<ErrorOr<Todo>> GetWithLogging(Guid id, ITodoService svc, CancellationToken ct)
    {
        var result = await svc.GetByIdAsync(id, ct);
        result.Switch(
            static todo => Console.WriteLine($"Retrieved todo: {todo.Title}"),
            static errors => Console.WriteLine($"Failed: {string.Join(", ", errors.Select(static e => e.Code))}"));
        return result;
    }

    private static ErrorOr<Success> ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Error.Validation("Todo.TitleEmpty", "Title cannot be empty");

        return title.Length switch
        {
            < 3 => Error.Validation("Todo.TitleTooShort", "Title must be at least 3 characters"),
            > 100 => Error.Validation("Todo.TitleTooLong", "Title cannot exceed 100 characters"),
            _ => Result.Success
        };
    }

    private static ErrorOr<Todo> EnrichTodo(Todo todo) => todo with { Title = $"[NEW] {todo.Title}" };
}
