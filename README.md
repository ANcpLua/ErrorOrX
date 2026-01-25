# ErrorOrX

[![NuGet](https://img.shields.io/nuget/v/ErrorOrX.Generators.svg)](https://www.nuget.org/packages/ErrorOrX.Generators/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ErrorOrX.Generators.svg)](https://www.nuget.org/packages/ErrorOrX.Generators/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Railway-Oriented Programming for .NET with source-generated ASP.NET Core Minimal API integration. Zero boilerplate, full
Native AOT support.

## Features

- **Discriminated Unions** - `ErrorOr<T>` represents success or a list of typed errors
- **Fluent API** - Chain operations with `Then`, `Else`, `Match`, `Switch`, and `FailIf`
- **Nullable Extensions** - Convert nullable values with `OrNotFound()`, `OrValidation()`, and more
- **Source Generator** - Auto-generates `MapErrorOrEndpoints()` from attributed static methods
- **Smart Binding** - Automatic parameter inference based on HTTP method and type
- **OpenAPI Ready** - Typed `Results<...>` unions for complete API documentation
- **Native AOT** - Reflection-free code generation with JSON serialization contexts
- **Middleware Support** - Emits fluent calls for `[Authorize]`, `[EnableRateLimiting]`, `[OutputCache]`

## Installation

```bash
dotnet add package ErrorOrX.Generators
```

This package includes both the source generator and the `ErrorOrX` runtime library.

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

## Error Types

Create structured errors mapped to HTTP status codes:

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

## Nullable-to-ErrorOr Extensions

Convert nullable values to `ErrorOr<T>` with auto-generated error codes:

```csharp
// Error code auto-generated from type name (e.g., "Todo.NotFound")
return _todos.Find(t => t.Id == id).OrNotFound($"Todo {id} not found");
return user.OrUnauthorized("Invalid credentials");
return record.OrValidation("Record is invalid");

// Custom errors
return value.OrError(Error.Custom(422, "Custom.Code", "Custom message"));
return value.OrError(() => BuildExpensiveError());  // Lazy evaluation
```

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

## Fluent API

Chain operations using railway-oriented programming patterns:

```csharp
// Chain operations - errors short-circuit the pipeline
var result = ValidateOrder(request)
    .Then(order => ProcessPayment(order))
    .Then(order => CreateShipment(order))
    .FailIf(order => order.Total <= 0, Error.Validation("Order.InvalidTotal", "Total must be positive"));

// Handle both cases
return result.Match(
    order => Ok(order),
    errors => BadRequest(errors.First().Description));

// Provide fallback on error
var user = GetUser(id).Else(errors => DefaultUser);

// Side effects
GetUser(id).Switch(
    user => Console.WriteLine($"Found: {user.Name}"),
    errors => Logger.LogError(errors.First().Description));
```

## Result Markers

Use semantic markers for endpoints without response bodies:

```csharp
Result.Success   // 200 OK (no body)
Result.Created   // 201 Created (no body)
Result.Updated   // 204 No Content
Result.Deleted   // 204 No Content
```

## Smart Parameter Binding

The generator automatically infers parameter sources:

```csharp
[Post("/todos")]
public static ErrorOr<Todo> Create(
    CreateTodoRequest req,    // -> Body (POST + complex type)
    ITodoService svc)         // -> Service (interface)
    => svc.Create(req);

[Get("/todos/{id}")]
public static ErrorOr<Todo> GetById(
    int id,                   // -> Route (matches {id})
    ITodoService svc)         // -> Service
    => svc.GetById(id).OrNotFound();
```

## Middleware Attributes

Middleware attributes are automatically translated to fluent calls:

```csharp
[Post("/admin")]
[Authorize("Admin")]
[EnableRateLimiting("fixed")]
[OutputCache(Duration = 60)]
public static ErrorOr<User> CreateAdmin(CreateUserRequest req) { }
// Generates: .RequireAuthorization("Admin").RequireRateLimiting("fixed").CacheOutput(...)
```

## Native AOT

Fully compatible with `PublishAot=true`. For custom JSON configuration:

```csharp
var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddErrorOrEndpoints(options => options
    .UseJsonContext<AppJsonSerializerContext>()  // Register JSON context
    .WithCamelCase()                              // Use camelCase (default: true)
    .WithIgnoreNulls());                          // Ignore nulls (default: true)

var app = builder.Build();
app.MapErrorOrEndpoints();
app.Run();
```

Create a `JsonSerializerContext` with your endpoint types:

```csharp
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Todo))]
[JsonSerializable(typeof(CreateTodoRequest))]
[JsonSerializable(typeof(ProblemDetails))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
```

Disable generator JSON context if providing your own:

```xml
<PropertyGroup>
    <ErrorOrGenerateJsonContext>false</ErrorOrGenerateJsonContext>
</PropertyGroup>
```

## Packages

| Package               | Target           | Description                          |
|-----------------------|------------------|--------------------------------------|
| `ErrorOrX.Generators` | `netstandard2.0` | Source generator (includes ErrorOrX) |
| `ErrorOrX`            | `net10.0`        | Runtime library (auto-referenced)    |

## Documentation

- [API Reference](https://github.com/ANcpLua/ErrorOrX/blob/main/docs/api.md)
- [Parameter Binding](https://github.com/ANcpLua/ErrorOrX/blob/main/docs/parameter-binding.md)
- [Diagnostics](https://github.com/ANcpLua/ErrorOrX/blob/main/docs/diagnostics.md)
- [Changelog](https://github.com/ANcpLua/ErrorOrX/blob/main/CHANGELOG.md)
