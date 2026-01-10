using System.Net.ServerSentEvents;
using System.Text.Json.Serialization;
using ErrorOr.Endpoints.Sample.Domain;
using Microsoft.AspNetCore.Mvc;

namespace ErrorOr.Endpoints.Sample;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
// Domain types
[JsonSerializable(typeof(Todo))]
[JsonSerializable(typeof(Todo[]))]
[JsonSerializable(typeof(List<Todo>))]
[JsonSerializable(typeof(CreateTodoRequest))]
[JsonSerializable(typeof(UpdateTodoRequest))]
[JsonSerializable(typeof(TodoCreatedEvent))]
[JsonSerializable(typeof(TodoCompletedEvent))]
[JsonSerializable(typeof(TodoDeletedEvent))]
// Test case types
[JsonSerializable(typeof(PaginationResult))]
[JsonSerializable(typeof(JobReference))]
// Primitive types for route constraints and query bindings
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(DateOnly))]
[JsonSerializable(typeof(TimeOnly))]
[JsonSerializable(typeof(TimeSpan))]
[JsonSerializable(typeof(DateTimeOffset))]
// Collection types
[JsonSerializable(typeof(int[]))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(Guid[]))]
[JsonSerializable(typeof(List<string>))]
// ASP.NET Core types
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
// SSE types - required for AOT-safe Server-Sent Events serialization
[JsonSerializable(typeof(SseItem<TodoCreatedEvent>))]
[JsonSerializable(typeof(SseItem<TodoCompletedEvent>))]
[JsonSerializable(typeof(SseItem<TodoDeletedEvent>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;