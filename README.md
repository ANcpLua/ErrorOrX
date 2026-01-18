# ErrorOrX

[![NuGet](https://img.shields.io/nuget/v/ErrorOrX.Generators.svg)](https://www.nuget.org/packages/ErrorOrX.Generators/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ErrorOrX.Generators.svg)](https://www.nuget.org/packages/ErrorOrX.Generators/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Discriminated unions for .NET with source-generated ASP.NET Core Minimal API integration. Zero boilerplate, full AOT
support.

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

## Nullable-to-ErrorOr Extensions

Convert nullable values to `ErrorOr<T>` with fluent extensions:

```csharp
// Auto-generates error code from type name (e.g., "Todo.NotFound")
return _todos.Find(t => t.Id == id).OrNotFound($"Todo {id} not found");
return user.OrUnauthorized("Invalid credentials");
return record.OrValidation("Record is invalid");

// Custom errors
return value.OrError(Error.Custom(422, "Custom.Code", "Custom message"));
return value.OrError(() => BuildExpensiveError());
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

## Error Types

```csharp
Error.Validation("User.InvalidEmail", "Email format is invalid")
Error.NotFound("User.NotFound", "User does not exist")
Error.Conflict("User.Duplicate", "Email already registered")
Error.Unauthorized("Auth.InvalidToken", "Token has expired")
Error.Forbidden("Auth.InsufficientRole", "Admin role required")
Error.Failure("Db.ConnectionFailed", "Database unavailable")
Error.Unexpected("Unknown", "An unexpected error occurred")
Error.Custom(422, "Validation.Complex", "Complex validation failed")
```

| Factory                | HTTP | Use Case                   |
|------------------------|------|----------------------------|
| `Error.Validation()`   | 400  | Input/request validation   |
| `Error.Unauthorized()` | 401  | Authentication required    |
| `Error.Forbidden()`    | 403  | Insufficient permissions   |
| `Error.NotFound()`     | 404  | Resource doesn't exist     |
| `Error.Conflict()`     | 409  | State conflict (duplicate) |
| `Error.Failure()`      | 500  | Known operational failure  |
| `Error.Unexpected()`   | 500  | Unhandled/unknown error    |
| `Error.Custom()`       | Any  | Custom HTTP status code    |

## Fluent API

```csharp
// Chain operations (railway-oriented programming)
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

```csharp
[Post("/admin")]
[Authorize("Admin")]
[EnableRateLimiting("fixed")]
[OutputCache(Duration = 60)]
public static ErrorOr<User> CreateAdmin(CreateUserRequest req) { }
// Generates: .RequireAuthorization("Admin").RequireRateLimiting("fixed").CacheOutput(...)
```

## Native AOT

Fully compatible with `PublishAot=true`. The generator produces reflection-free code that works seamlessly with Native
AOT compilation.

### Zero-Config (Recommended)

Add one line - the generator auto-discovers all types from your endpoints:

```csharp
// Add this anywhere in your project
[assembly: ErrorOr.AotJsonAssembly]
```

Or for more control, decorate your own context:

```csharp
[AotJson]  // Auto-generates [JsonSerializable] for all endpoint types
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
```

### Program.cs Setup

Use the fluent builder for configuration:

```csharp
var builder = WebApplication.CreateSlimBuilder(args);

// Fluent configuration - CamelCase and IgnoreNulls enabled by default
builder.Services.AddErrorOrEndpoints(options => options
    .UseJsonContext<AppJsonSerializerContext>());

var app = builder.Build();
app.MapErrorOrEndpoints();
app.Run();
```

#### Available Options

```csharp
services.AddErrorOrEndpoints(options => options
    .UseJsonContext<AppJsonSerializerContext>()  // Register JSON context for AOT
    .WithCamelCase()                              // Use camelCase naming (default: true)
    .WithIgnoreNulls());                          // Ignore null values (default: true)
```

### AotJson Attribute Options

```csharp
[AotJson(
    ScanEndpoints = true,              // Discover types from ErrorOr endpoints (default: true)
    IncludeProblemDetails = true,      // Include ProblemDetails types (default: true)
    TraversePropertyTypes = true,      // Discover nested types from properties (default: true)
    GenerateCollections = CollectionKind.List | CollectionKind.Array,  // Collection variants
    IncludeTypes = new[] { typeof(CustomType) },  // Explicit includes
    ExcludeTypes = new[] { typeof(InternalType) } // Explicit excludes
)]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
```

### How It Works

The generator emits AOT-compatible endpoint handlers using a wrapper pattern:

1. **Typed Map methods** - Uses `MapGet`, `MapPost`, etc. instead of `MapMethods` with delegate cast
2. **ExecuteAsync pattern** - Handlers return `Task` and explicitly write responses via `IResult.ExecuteAsync()`
3. **No reflection** - All routing and serialization uses compile-time generated code

This design avoids the reflection-based `RequestDelegateFactory` path that requires JSON metadata for
`Task<Results<...>>` types, which cannot be satisfied for BCL generic types at compile time.

## Documentation

- [API Reference](docs/api.md)
- [Parameter Binding](docs/parameter-binding.md)
- [Diagnostics](docs/diagnostics.md)
- [Changelog](CHANGELOG.md)

## License

MIT