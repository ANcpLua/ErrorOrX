using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace ErrorOrX.Sample;

// JSON options configured via builder: .WithCamelCase().WithIgnoreNulls()
[JsonSerializable(typeof(Todo))]
[JsonSerializable(typeof(Todo[]))]
[JsonSerializable(typeof(List<Todo>))]
[JsonSerializable(typeof(CreateTodoRequest))]
[JsonSerializable(typeof(List<CreateTodoRequest>))]
[JsonSerializable(typeof(UpdateTodoRequest))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;