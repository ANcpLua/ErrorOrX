namespace ErrorOrX.Sample;

/// <summary>
///     Demonstrates advanced ErrorOr features - Railway-Oriented Programming patterns.
///     Shows how to chain operations, handle errors, and compose complex flows.
/// </summary>
public static class AdvancedErrorHandlingApi
{
    /// <summary>
    ///     Demonstrates .Then() chaining - compose multiple operations that can fail.
    ///     Each step only executes if the previous step succeeded.
    /// </summary>
    [Post("/api/advanced/todos/with-validation")]
    public static Task<ErrorOr<Todo>> CreateWithValidation(
        CreateTodoRequest request,
        ITodoService svc,
        CancellationToken ct)
    {
        // Railway-Oriented Programming: validate → create → enrich
        return ValidateTitle(request.Title)
            .ThenAsync(_ => svc.CreateAsync(request, ct))
            .Then(static todo => EnrichTodo(todo));
    }

    /// <summary>
    ///     Demonstrates .FailIf() - conditional error creation.
    ///     Returns error if condition is true, otherwise continues with value.
    /// </summary>
    [Get("/api/advanced/todos/{id:guid}/check")]
    public static async Task<ErrorOr<Todo>> CheckTodoStatus(
        Guid id,
        ITodoService svc,
        CancellationToken ct)
    {
        var result = await svc.GetByIdAsync(id, ct);

        // Fail if todo is incomplete
        return result.FailIf(
            static todo => !todo.IsComplete,
            Error.Validation("Todo.Incomplete", "Todo must be completed before checking"));
    }

    /// <summary>
    ///     Demonstrates .Else() - fallback logic when an error occurs.
    ///     Provides alternative value or recovery strategy.
    /// </summary>
    [Get("/api/advanced/todos/{id:guid}/or-default")]
    public static Task<ErrorOr<Todo>> GetOrDefault(
        Guid id,
        ITodoService svc,
        CancellationToken ct)
    {
        // Return a default todo if any error occurs
        return svc.GetByIdAsync(id, ct)
            .Else(static _ => new Todo(Guid.Empty, "Default Todo"));
    }

    /// <summary>
    ///     Demonstrates .Match() - pattern matching on success/error.
    ///     Converts ErrorOr to a concrete result type.
    /// </summary>
    [Get("/api/advanced/todos/{id:guid}/summary")]
    public static async Task<ErrorOr<string>> GetTodoSummary(
        Guid id,
        ITodoService svc,
        CancellationToken ct)
    {
        var result = await svc.GetByIdAsync(id, ct);

        // Pattern match on success/error
        var summary = result.Match(
            static todo => $"Todo '{todo.Title}' is {(todo.IsComplete ? "complete" : "incomplete")}",
            static errors => $"Error: {string.Join(", ", errors.Select(static e => e.Description))}");

        return summary;
    }

    /// <summary>
    ///     Demonstrates multiple .FailIf() for validation chains.
    /// </summary>
    [Get("/api/advanced/todos/{id:guid}/complex")]
    public static async Task<ErrorOr<Todo>> GetWithComplexValidation(
        Guid id,
        ITodoService svc,
        CancellationToken ct)
    {
        // Get todo and apply validation chain
        var result = await svc.GetByIdAsync(id, ct);

        return result
            .FailIf(
                static todo => todo.Title.Length > 100,
                Error.Validation("Todo.TitleTooLong", "Title cannot exceed 100 characters"))
            .FailIf(
                static todo => string.IsNullOrWhiteSpace(todo.Title),
                Error.Validation("Todo.TitleEmpty", "Title cannot be empty"));
    }

    /// <summary>
    ///     Demonstrates chaining multiple service calls with error handling.
    /// </summary>
    [Put("/api/advanced/todos/{sourceId:guid}/copy-to/{targetId:guid}")]
    public static async Task<ErrorOr<Updated>> CopyTodoTitle(
        Guid sourceId,
        Guid targetId,
        ITodoService svc,
        CancellationToken ct)
    {
        // Get source todo
        var sourceResult = await svc.GetByIdAsync(sourceId, ct);
        if (sourceResult.IsError) return sourceResult.Errors.ToArray();

        // Get target todo
        var targetResult = await svc.GetByIdAsync(targetId, ct);
        if (targetResult.IsError) return targetResult.Errors.ToArray();

        // Update target with source title
        var updateRequest = new UpdateTodoRequest(
            sourceResult.Value.Title,
            targetResult.Value.IsComplete);

        return await svc.UpdateAsync(targetId, updateRequest, ct);
    }

    /// <summary>
    ///     Demonstrates using .ThenAsync() with async operations and batch validation.
    /// </summary>
    [Post("/api/advanced/todos/batch")]
    public static async Task<ErrorOr<List<Todo>>> CreateBatch(
        List<CreateTodoRequest> requests,
        ITodoService svc,
        CancellationToken ct)
    {
        switch (requests.Count)
        {
            // Validate batch size
            case > 10:
                return Error.Validation("Batch.TooLarge", "Cannot create more than 10 todos at once");
            case 0:
                return Error.Validation("Batch.Empty", "Must provide at least one todo");
        }

        // Create all todos
        var todos = new List<Todo>();
        foreach (var request in requests)
        {
            var result = await svc.CreateAsync(request, ct);
            if (result.IsError) return result.Errors.ToArray();

            todos.Add(result.Value);
        }

        return todos;
    }

    /// <summary>
    ///     Demonstrates .Switch() for side effects on success/error.
    /// </summary>
    [Get("/api/advanced/todos/{id:guid}/with-logging")]
    public static async Task<ErrorOr<Todo>> GetWithLogging(
        Guid id,
        ITodoService svc,
        CancellationToken ct)
    {
        var result = await svc.GetByIdAsync(id, ct);

        // Execute side effects based on success/error
        result.Switch(
            static todo => Console.WriteLine($"Successfully retrieved todo: {todo.Title}"),
            static errors =>
                Console.WriteLine($"Failed to retrieve todo: {string.Join(", ", errors.Select(static e => e.Code))}"));

        return result;
    }

    // Helper methods

    private static ErrorOr<Success> ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return Error.Validation("Todo.TitleEmpty", "Title cannot be empty");

        return title.Length switch
        {
            < 3 => Error.Validation("Todo.TitleTooShort", "Title must be at least 3 characters"),
            > 100 => Error.Validation("Todo.TitleTooLong", "Title cannot exceed 100 characters"),
            _ => Result.Success
        };
    }

    private static ErrorOr<Todo> EnrichTodo(Todo todo)
    {
        // Simulate enrichment logic
        return todo with { Title = $"[NEW] {todo.Title}" };
    }
}
