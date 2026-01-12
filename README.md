# ErrorOrX

[![NuGet](https://img.shields.io/nuget/v/ErrorOrX.svg)](https://www.nuget.org/packages/ErrorOrX/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ErrorOrX.svg)](https://www.nuget.org/packages/ErrorOrX/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A discriminated union type for .NET with source-generated ASP.NET Core Minimal API integration. One package, zero
boilerplate, full AOT support.

## Installation

```bash
dotnet add package ErrorOrX
```

## Quick Start

### Program.cs

```csharp
var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddOpenApi();

var app = builder.Build();
app.MapOpenApi();
app.MapErrorOrEndpoints();  // Auto-registers all endpoints
app.Run();
```

### Define Endpoints

```csharp
using ErrorOr;

public static class TodoApi
{
    [Get("/todos")]
    public static ErrorOr<List<Todo>> GetAll(ITodoService svc)
        => svc.GetAll();

    [Get("/todos/{id}")]
    public static ErrorOr<Todo> GetById(int id, ITodoService svc)
        => svc.GetById(id) is { } todo
            ? todo
            : Error.NotFound("Todo.NotFound", $"Todo {id} not found");

    [Post("/todos")]
    public static ErrorOr<Todo> Create(CreateTodoRequest req, ITodoService svc)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
            return Error.Validation("Todo.InvalidTitle", "Title is required");

        return svc.Create(req);  // Returns 201 Created with Location header
    }

    [Delete("/todos/{id}")]
    public static ErrorOr<Deleted> Delete(int id, ITodoService svc)
        => svc.Delete(id) ? Result.Deleted : Error.NotFound("Todo.NotFound", $"Todo {id} not found");
}
```

## ErrorOr Fundamentals

### Creating Values and Errors

```csharp
// Success - implicit conversion
ErrorOr<int> result = 42;

// Errors
ErrorOr<User> notFound = Error.NotFound("User.NotFound", "User not found");
ErrorOr<User> validation = Error.Validation("User.InvalidEmail", "Invalid email format");

// Multiple errors
ErrorOr<User> errors = new List<Error>
{
    Error.Validation("User.InvalidName", "Name is required"),
    Error.Validation("User.InvalidEmail", "Email is invalid")
};
```

### Checking Results

```csharp
if (result.IsError)
{
    foreach (var error in result.Errors)
        Console.WriteLine($"{error.Code}: {error.Description}");
}
else
{
    Console.WriteLine(result.Value);
}
```

### Built-in Result Types

```csharp
ErrorOr<Deleted> DeleteUser(int id) => Result.Deleted;   // 204 No Content
ErrorOr<Updated> UpdateUser(int id) => Result.Updated;   // 204 No Content
ErrorOr<Created> CreateUser()       => Result.Created;   // 201 Created
ErrorOr<Success> DoSomething()      => Result.Success;   // 200 OK
```

## Error Types and HTTP Mapping

| Error Factory          | HTTP Status | TypedResult                           |
|------------------------|-------------|---------------------------------------|
| `Error.Validation()`   | 400         | `ValidationProblem`                   |
| `Error.Unauthorized()` | 401         | `UnauthorizedHttpResult`              |
| `Error.Forbidden()`    | 403         | `ForbidHttpResult`                    |
| `Error.NotFound()`     | 404         | `NotFound<ProblemDetails>`            |
| `Error.Conflict()`     | 409         | `Conflict<ProblemDetails>`            |
| `Error.Failure()`      | 500         | `InternalServerError<ProblemDetails>` |
| `Error.Unexpected()`   | 500         | `InternalServerError<ProblemDetails>` |

## Middleware Attribute Support

The generator detects BCL middleware attributes and emits corresponding fluent calls:

```csharp
[Post("/admin/users")]
[Authorize("Admin")]
[EnableRateLimiting("fixed")]
public static ErrorOr<User> CreateAdmin(CreateUserRequest req)
{
    // Generated code includes:
    // .RequireAuthorization("Admin")
    // .RequireRateLimiting("fixed")
}
```

| Attribute                         | Generated Call                    |
|-----------------------------------|-----------------------------------|
| `[Authorize]`                     | `.RequireAuthorization()`         |
| `[Authorize("Policy")]`           | `.RequireAuthorization("Policy")` |
| `[AllowAnonymous]`                | `.AllowAnonymous()`               |
| `[EnableRateLimiting("policy")]`  | `.RequireRateLimiting("policy")`  |
| `[DisableRateLimiting]`           | `.DisableRateLimiting()`          |
| `[OutputCache]`                   | `.CacheOutput()`                  |
| `[OutputCache(PolicyName = "x")]` | `.CacheOutput("x")`               |
| `[EnableCors("policy")]`          | `.RequireCors("policy")`          |
| `[DisableCors]`                   | `.DisableCors()`                  |

## Fluent API

Chain operations with railway-oriented programming:

```csharp
// Then - chain dependent operations
ErrorOr<Order> result = ValidateOrder(request)
    .Then(order => CheckInventory(order))
    .Then(order => ProcessPayment(order))
    .Then(order => CreateShipment(order));

// Async chains
var result = await GetUserAsync(id)
    .ThenAsync(user => ValidateAsync(user))
    .ThenAsync(user => EnrichAsync(user));

// Else - provide fallbacks
User user = GetUser(id).Else(User.Guest);
User user = GetUser(id).Else(errors => HandleErrors(errors));

// Match - handle both cases
string message = GetUser(id).Match(
    onValue: user => $"Found: {user.Name}",
    onError: errors => $"Error: {errors.First().Description}"
);

// Switch - side effects
GetUser(id).Switch(
    onValue: user => SendEmail(user),
    onError: errors => LogErrors(errors)
);
```

## Endpoint Attributes

```csharp
[Get("/path")]              // HTTP GET
[Post("/path")]             // HTTP POST
[Put("/path")]              // HTTP PUT
[Delete("/path")]           // HTTP DELETE
[Patch("/path")]            // HTTP PATCH

// Route parameters
[Get("/users/{id}")]
public static ErrorOr<User> Get(int id) { }

// Query parameters (automatically bound)
[Get("/users")]
public static ErrorOr<List<User>> Search(int page = 1, string? search = null) { }

// Request body (automatically bound for POST/PUT/PATCH)
[Post("/users")]
public static ErrorOr<User> Create(CreateUserRequest request) { }

// Async endpoints
[Get("/users/{id}")]
public static Task<ErrorOr<User>> GetAsync(int id, CancellationToken ct) { }
```

## Native AOT Support

ErrorOr is fully compatible with Native AOT. The source generator produces reflection-free code that works with
`PublishAot=true`.

```xml
<PropertyGroup>
    <PublishAot>true</PublishAot>
</PropertyGroup>
```

For AOT JSON serialization, register your types:

```csharp
[JsonSerializable(typeof(Todo))]
[JsonSerializable(typeof(List<Todo>))]
public partial class AppJsonContext : JsonSerializerContext { }

// In Program.cs
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default));
```

## Best Practices

### Domain-Specific Errors

```csharp
public static class UserErrors
{
    public static Error NotFound(int id) =>
        Error.NotFound("User.NotFound", $"User {id} not found");

    public static Error DuplicateEmail(string email) =>
        Error.Conflict("User.DuplicateEmail", $"Email '{email}' already exists");
}

// Usage
return UserErrors.NotFound(id);
```

### Aggregate Validation Errors

```csharp
public static ErrorOr<ValidatedRequest> Validate(CreateUserRequest request)
{
    var errors = new List<Error>();

    if (string.IsNullOrWhiteSpace(request.Name))
        errors.Add(Error.Validation("User.Name.Required", "Name is required"));

    if (string.IsNullOrWhiteSpace(request.Email))
        errors.Add(Error.Validation("User.Email.Required", "Email is required"));

    return errors.Count > 0 ? errors : new ValidatedRequest(request);
}
```

### Keep Endpoints Thin

```csharp
// Delegate to services
[Post("/orders")]
public static ErrorOr<Order> Create(CreateOrderRequest request, IOrderService service)
    => service.CreateOrder(request);
```