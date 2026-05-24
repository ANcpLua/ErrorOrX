namespace ErrorOrX.Samples.Api.Domain;

public sealed record Todo(Guid Id, string Title, bool IsComplete = false);

public sealed record CreateTodoRequest(string Title);

public sealed record UpdateTodoRequest(string Title, bool IsComplete);
