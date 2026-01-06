using System.Text.Json.Serialization;
using ErrorOr.Http.Bcl.Sample.Domain;
using Microsoft.AspNetCore.Mvc;

namespace ErrorOr.Http.Bcl.Sample;

[JsonSerializable(typeof(Todo))]
[JsonSerializable(typeof(Todo[]))]
[JsonSerializable(typeof(List<Todo>))]
[JsonSerializable(typeof(CreateTodoRequest))]
[JsonSerializable(typeof(UpdateTodoRequest))]
[JsonSerializable(typeof(TodoCreatedEvent))]
[JsonSerializable(typeof(TodoCompletedEvent))]
[JsonSerializable(typeof(TodoDeletedEvent))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class AppJsonSerializerContext : JsonSerializerContext;