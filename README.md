# ErrorOrX

[![NuGet](https://img.shields.io/nuget/v/ErrorOrX.Generators.svg)](https://www.nuget.org/packages/ErrorOrX.Generators/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ErrorOrX.Generators.svg)](https://www.nuget.org/packages/ErrorOrX.Generators/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Railway-Oriented Programming for .NET with source-generated ASP.NET Core Minimal API integration. Zero boilerplate, full
Native AOT support.

## Features

- **Discriminated Unions** — `ErrorOr<T>` represents success or a list of typed errors
- **Fluent API** — `Then`, `Else`, `Match`, `Switch`, `FailIf`
- **Nullable Extensions** — `OrNotFound()`, `OrValidation()`, …
- **Source Generator** — Auto-generates `MapErrorOrEndpoints()` from attributed static methods
- **Smart Binding** — Automatic parameter inference based on HTTP method and type
- **OpenAPI Ready** — Typed `Results<…>` unions for complete API documentation
- **Native AOT** — Reflection-free code generation with JSON serialization contexts
- **Middleware Pass-Through** — `[Authorize]`, `[EnableRateLimiting]`, `[OutputCache]`, `[EnableCors]` → fluent calls
- **API Versioning** — Integrates with `Asp.Versioning.Http` for versioned endpoint groups
- **36 Analyzers** — Real-time IDE feedback for route conflicts, binding errors, AOT compatibility

For the full mental model of what your code becomes after the generator runs, see [CLAUDE.md](CLAUDE.md).

## Installation

```bash
dotnet add package ErrorOrX.Generators
```

Includes the `ErrorOrX` runtime as a transitive dependency.

## Quick Start

```csharp
// Program.cs
var app = WebApplication.CreateSlimBuilder(args).Build();
app.MapErrorOrEndpoints();
app.Run();
```

```csharp
// TodoApi.cs
[Get("/api/todos/{id:guid}")]
public static Task<ErrorOr<Todo>> GetById(Guid id, ITodoService svc, CancellationToken ct)
    => svc.GetByIdAsync(id, ct);
```

Full working example: [`samples/ErrorOrX.Samples.Api/Program.cs`](samples/ErrorOrX.Samples.Api/Program.cs) +
[`TodoApi.cs`](samples/ErrorOrX.Samples.Api/TodoApi.cs).

## Error → HTTP Mapping

Errors map to [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457) ProblemDetails:

| ErrorType    | HTTP | TypedResult                           |
|--------------|------|---------------------------------------|
| Validation   | 400  | `ValidationProblem` with field errors |
| Unauthorized | 401  | `UnauthorizedHttpResult`              |
| Forbidden    | 403  | `ForbidHttpResult`                    |
| NotFound     | 404  | `NotFound<ProblemDetails>`            |
| Conflict     | 409  | `Conflict<ProblemDetails>`            |
| Failure      | 500  | `InternalServerError<ProblemDetails>` |
| Unexpected   | 500  | `InternalServerError<ProblemDetails>` |
| Custom(422)  | 422  | `UnprocessableEntity<ProblemDetails>` |

Source: [`ErrorMapping.cs`](src/ErrorOrX.Generators/Models/ErrorMapping.cs).

## Error Types

```csharp
Error.Validation("User.InvalidEmail", "Email format is invalid")   // 400
Error.Unauthorized("Auth.InvalidToken", "Token has expired")       // 401
Error.Forbidden("Auth.InsufficientRole", "Admin role required")    // 403
Error.NotFound("User.NotFound", "User does not exist")             // 404
Error.Conflict("User.Duplicate", "Email already registered")       // 409
Error.Failure("Db.ConnectionFailed", "Database unavailable")       // 500
Error.Unexpected("Unknown", "An unexpected error occurred")        // 500
Error.Custom(422, "Validation.Complex", "Complex validation failed")
```

## Nullable → `ErrorOr<T>` Extensions

Error code is auto-generated from the type name (e.g., `Todo.NotFound`).

| Extension           | Error Type   | HTTP | Description              |
|---------------------|--------------|------|--------------------------|
| `.OrNotFound()`     | NotFound     | 404  | Resource not found       |
| `.OrValidation()`   | Validation   | 400  | Input validation failed  |
| `.OrUnauthorized()` | Unauthorized | 401  | Authentication required  |
| `.OrForbidden()`    | Forbidden    | 403  | Insufficient permissions |
| `.OrConflict()`     | Conflict     | 409  | State conflict           |
| `.OrFailure()`      | Failure      | 500  | Operational failure      |
| `.OrUnexpected()`   | Unexpected   | 500  | Unexpected error         |
| `.OrError(Error)`   | Any          | Any  | Custom error             |
| `.OrError(Func)`    | Any          | Any  | Lazy custom error        |

Usage in context: [`Domain/TodoService.cs`](samples/ErrorOrX.Samples.Api/Domain/TodoService.cs).

## Fluent API

Chain operations railway-style — errors short-circuit the pipeline. Each operator has a worked endpoint in the sample:

| Operator        | Sample                                                                                                                                                |
|-----------------|-------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Then`/`ThenAsync` | [`AdvancedErrorHandlingApi.cs:10-12`](samples/ErrorOrX.Samples.Api/AdvancedErrorHandlingApi.cs#L10-L12)                                            |
| `FailIf`           | [`AdvancedErrorHandlingApi.cs:17-21`](samples/ErrorOrX.Samples.Api/AdvancedErrorHandlingApi.cs#L17-L21) (single) · [`:41-45`](samples/ErrorOrX.Samples.Api/AdvancedErrorHandlingApi.cs#L41-L45) (chained) |
| `Else`             | [`AdvancedErrorHandlingApi.cs:25-26`](samples/ErrorOrX.Samples.Api/AdvancedErrorHandlingApi.cs#L25-L26)                                            |
| `Match`            | [`AdvancedErrorHandlingApi.cs:31-34`](samples/ErrorOrX.Samples.Api/AdvancedErrorHandlingApi.cs#L31-L34)                                            |
| `Switch`           | [`AdvancedErrorHandlingApi.cs:93-97`](samples/ErrorOrX.Samples.Api/AdvancedErrorHandlingApi.cs#L93-L97)                                            |

## Result Markers

For endpoints without a response body:

```csharp
Result.Success   // 200 OK
Result.Created   // 201 Created
Result.Updated   // 204 No Content
Result.Deleted   // 204 No Content
```

## `[ReturnsError]` on Interfaces

Document possible errors on interface methods — the generator reads them when building the `Results<…>` union for
OpenAPI. See [`Domain/ITodoService.cs`](samples/ErrorOrX.Samples.Api/Domain/ITodoService.cs) for five attribute usages
spanning `Failure`, `NotFound`, and `Validation`.

## Middleware Attributes

Standard ASP.NET Core attributes on handlers are translated to Minimal API fluent
calls. The generator also auto-adds `401`/`403` (for `[Authorize]`) and `429`
(for `[EnableRateLimiting]`) `ProducesResponseTypeMetadata` so OpenAPI documents
the failure modes:

| Attribute               | Emitted fluent call                               |
|-------------------------|---------------------------------------------------|
| `[Authorize(...)]`      | `.RequireAuthorization(...)`                      |
| `[EnableRateLimiting]`  | `.RequireRateLimiting(...)`                       |
| `[OutputCache]`         | `.CacheOutput(p => p.Expire(TimeSpan.From...))`   |
| `[EnableCors]`          | `.RequireCors(...)`                               |

Four worked endpoints (one per attribute, including stacked combinations):
[`samples/ErrorOrX.Samples.Api/AdminApi.cs`](samples/ErrorOrX.Samples.Api/AdminApi.cs).
Service wiring (`AddAuthorizationBuilder`, `AddRateLimiter`, `AddOutputCache`,
`AddCors`) lives in
[`Program.cs`](samples/ErrorOrX.Samples.Api/Program.cs).

## Native AOT

Fully compatible with `PublishAot=true`. Define a `JsonSerializerContext` covering your request/response/problem types:

```csharp
[JsonSerializable(typeof(Todo))]
[JsonSerializable(typeof(CreateTodoRequest))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext;
```

Wire it via the builder — `WithCamelCase()` / `WithIgnoreNulls()` override at runtime, or set them once on the context
via `[JsonSourceGenerationOptions]`:

```csharp
builder.Services.AddErrorOrEndpoints()
    .UseJsonContext<AppJsonSerializerContext>()
    .WithCamelCase()
    .WithIgnoreNulls();
```

Working setup: [`AppJsonSerializerContext.cs`](samples/ErrorOrX.Samples.Api/AppJsonSerializerContext.cs) +
[`Program.cs`](samples/ErrorOrX.Samples.Api/Program.cs).

## Analyzers (36 Diagnostics)

| Category   | Diagnostics                    | Examples                                                                |
|------------|--------------------------------|-------------------------------------------------------------------------|
| Core       | EOE001-006                     | Invalid return type, non-static handler, unbound route param            |
| Binding    | EOE008-021                     | Multiple body sources, invalid `[FromRoute]` type, ambiguous binding    |
| Results    | EOE022-024                     | Too many result types, unknown error factory, undocumented interface    |
| AOT/JSON   | EOE007, EOE025-026, EOE034-036 | Not AOT-serializable, missing camelCase, missing context, validation reflection |
| Versioning | EOE027-031                     | Version-neutral conflict, undeclared version, invalid format            |
| Naming     | EOE032-033                     | Duplicate route binding, non-PascalCase handler                         |

Source: [`Descriptors.*.cs`](src/ErrorOrX.Generators/Analyzers/). Diagnostics-in-action walkthrough:
[`samples/ErrorOrX.Samples.Diagnostics/README.md`](samples/ErrorOrX.Samples.Diagnostics/README.md).

## Packages

| Package               | Target           | Description                          |
|-----------------------|------------------|--------------------------------------|
| `ErrorOrX.Generators` | `netstandard2.0` | Source generator (pulls in ErrorOrX) |
| `ErrorOrX`            | `net10.0`        | Runtime library (auto-referenced)    |

## Changelog

See [CHANGELOG.md](CHANGELOG.md).
