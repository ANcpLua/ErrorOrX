namespace ErrorOr.Endpoints.Sample.Domain;

public sealed record TodoCreatedEvent(
    Guid Id,
    string Title,
    DateOnly? DueBy,
    DateTimeOffset CreatedAtUtc);

public sealed record TodoCompletedEvent(
    Guid Id,
    bool IsComplete,
    DateTimeOffset CompletedAtUtc);

public sealed record TodoDeletedEvent(
    Guid Id,
    DateTimeOffset DeletedAtUtc);