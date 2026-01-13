# ErrorOrX

[![NuGet](https://img.shields.io/nuget/v/ErrorOrX.Generators.svg)](https://www.nuget.org/packages/ErrorOrX.Generators/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ErrorOrX.Generators.svg)](https://www.nuget.org/packages/ErrorOrX.Generators/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Discriminated unions for .NET with source-generated ASP.NET Core Minimal API integration. Zero boilerplate, full AOT support.

## Installation

```bash
dotnet add package ErrorOrX.Generators
```

## Quick Start

```csharp
// Program.cs
var app = WebApplication.CreateSlimBuilder(args).Build();
app.MapErrorOrEndpoints();
app.Run();
```

```csharp
// TodoApi.cs
using ErrorOr;

public static class TodoApi
{
    [Get("/todos/{id}")]
    public static ErrorOr<Todo> GetById(int id, ITodoService svc)
        => svc.GetById(id).OrNotFound($"Todo {id} not found");

    [Post("/todos")]
    public static ErrorOr<Todo> Create(CreateTodoRequest req, ITodoService svc)
        => svc.Create(req);  // 201 Created

    [Delete("/todos/{id}")]
    public static ErrorOr<Deleted> Delete(int id, ITodoService svc)
        => svc.Delete(id) ? Result.Deleted : Error.NotFound();
}
```

## Nullable Handling

Convert nullable values to `ErrorOr<T>` with fluent extensions:

```csharp
// Auto-generates error code from type name (e.g., "Todo.NotFound")
return _todos.Find(t => t.Id == id).OrNotFound($"Todo {id} not found");
return user.OrUnauthorized("Invalid credentials");
return record.OrValidation("Record is invalid");
```

| Extension          | Error Type   | HTTP |
|--------------------|--------------|------|
| `.OrNotFound()`    | NotFound     | 404  |
| `.OrValidation()`  | Validation   | 400  |
| `.OrUnauthorized()`| Unauthorized | 401  |
| `.OrForbidden()`   | Forbidden    | 403  |
| `.OrConflict()`    | Conflict     | 409  |
| `.OrFailure()`     | Failure      | 500  |

## Error Types

| Factory                | HTTP | Use Case               |
|------------------------|------|------------------------|
| `Error.Validation()`   | 400  | Input validation       |
| `Error.Unauthorized()` | 401  | Authentication         |
| `Error.Forbidden()`    | 403  | Authorization          |
| `Error.NotFound()`     | 404  | Resource not found     |
| `Error.Conflict()`     | 409  | State conflict         |
| `Error.Failure()`      | 500  | Operational failure    |

## Fluent API

```csharp
// Chain operations
var result = ValidateOrder(request)
    .Then(order => ProcessPayment(order))
    .Then(order => CreateShipment(order));

// Handle both cases
return result.Match(
    order => Ok(order),
    errors => BadRequest(errors));
```

## Middleware

```csharp
[Post("/admin")]
[Authorize("Admin")]
[EnableRateLimiting("fixed")]
public static ErrorOr<User> CreateAdmin(CreateUserRequest req) { }
// Generates: .RequireAuthorization("Admin").RequireRateLimiting("fixed")
```

## Native AOT

Fully compatible with `PublishAot=true`. The generator produces reflection-free code.

## Documentation

- [API Reference](docs/api.md) - Full API documentation
- [Parameter Binding](docs/parameter-binding.md) - How parameters are bound
- [Diagnostics](docs/diagnostics.md) - Analyzer warnings and errors
- [Changelog](CHANGELOG.md) - Version history

## License

MIT
