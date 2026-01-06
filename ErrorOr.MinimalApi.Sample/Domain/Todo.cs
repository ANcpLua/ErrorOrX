namespace ErrorOr.Http.Bcl.Sample.Domain;

public record Todo(Guid Id, string Title, DateOnly? DueBy = null, bool IsComplete = false);

public record CreateTodoRequest(string Title, DateOnly? DueBy);

public record UpdateTodoRequest(string Title, DateOnly? DueBy, bool IsComplete);