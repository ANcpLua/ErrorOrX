namespace ErrorOr.Endpoints.Sample.Domain;

public record Todo(Guid Id, string Title, bool IsComplete = false);

public record CreateTodoRequest(string Title);

public record UpdateTodoRequest(string Title, bool IsComplete);
