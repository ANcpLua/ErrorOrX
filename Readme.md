# ErrorOr

[![NuGet](https://img.shields.io/nuget/v/ErrorOr.svg)](https://www.nuget.org/packages/ErrorOr/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ErrorOr.svg)](https://www.nuget.org/packages/ErrorOr/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A unified package for discriminated union error handling with source-generated ASP.NET Core Minimal API integration.

---

## Features

- **Discriminated Union (`ErrorOr<T>`)** - Type-safe error handling without exceptions
- **Source Generated** - Zero reflection, compile-time endpoint generation
- **AOT Ready** - Full Native AOT compatibility out of the box
- **Automatic HTTP Mapping** - Errors automatically map to appropriate HTTP status codes
- **OpenAPI Integration** - Swagger/OpenAPI documentation generated automatically
- **Minimal API Native** - First-class support for ASP.NET Core Minimal APIs

---

## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [ErrorOr Fundamentals](#erroror-fundamentals)
- [Error Types and HTTP Mapping](#error-types-and-http-mapping)
- [Fluent API](#fluent-api)
- [Endpoint Attributes](#endpoint-attributes)
- [Native AOT Support](#native-aot-support)
- [Configuration](#configuration)
- [Best Practices](#best-practices)
- [Documentation](#documentation)

---

## Installation

Install the unified ErrorOr package:

```bash
dotnet add package ErrorOr
```

That's it! The package includes everything you need:
- Core `ErrorOr<T>` type
- Source generators for Minimal API endpoints
- OpenAPI integration

---

## Quick Start

### 1. Define Your Endpoint

Create an endpoint class with attributes specifying the HTTP method and route:

```csharp
using ErrorOr;

public class GetUserEndpoint
{
    [Get("/users/{id}")]
    public static ErrorOr<UserResponse> Handle(int id, IUserService userService)
    {
        var user = userService.GetById(id);

        if (user is null)
        {
            return Error.NotFound("User.NotFound", $"User with ID {id} was not found.");
        }

        return new UserResponse(user.Id, user.Name, user.Email);
    }
}
```

### 2. Create an Endpoint with Validation

```csharp
public class CreateUserEndpoint
{
    [Post("/users")]
    public static ErrorOr<UserResponse> Handle(CreateUserRequest request, IUserService userService)
    {
        // Validate the request
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Error.Validation("User.InvalidName", "Name cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Error.Validation("User.InvalidEmail", "Email cannot be empty.");
        }

        // Check for duplicates
        if (userService.ExistsByEmail(request.Email))
        {
            return Error.Conflict("User.DuplicateEmail", "A user with this email already exists.");
        }

        var user = userService.Create(request.Name, request.Email);

        return new UserResponse(user.Id, user.Name, user.Email);
    }
}
```

### 3. Delete Endpoint Example

```csharp
public class DeleteUserEndpoint
{
    [Delete("/users/{id}")]
    public static ErrorOr<Deleted> Handle(int id, IUserService userService)
    {
        var user = userService.GetById(id);

        if (user is null)
        {
            return Error.NotFound("User.NotFound", $"User with ID {id} was not found.");
        }

        userService.Delete(id);

        return Result.Deleted;
    }
}
```

### 4. Configure Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Map all ErrorOr endpoints (source-generated)
app.MapErrorOrEndpoints();

app.Run();
```

---

## ErrorOr Fundamentals

### Creating Success Values

```csharp
// Implicit conversion from T
ErrorOr<int> result = 42;

// Explicit creation
ErrorOr<string> greeting = ErrorOr<string>.From("Hello, World!");
```

### Creating Errors

```csharp
// Single error
ErrorOr<User> result = Error.NotFound("User.NotFound", "User was not found.");

// Multiple errors
ErrorOr<User> result = new List<Error>
{
    Error.Validation("User.InvalidName", "Name is required."),
    Error.Validation("User.InvalidEmail", "Email format is invalid.")
};
```

### Checking Results

```csharp
ErrorOr<User> result = GetUser(id);

if (result.IsError)
{
    // Handle errors
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"{error.Code}: {error.Description}");
    }
}
else
{
    // Use the value
    User user = result.Value;
    Console.WriteLine($"Found user: {user.Name}");
}
```

### Built-in Result Types

For operations that don't return a value, use the built-in result types:

```csharp
// For successful deletions
ErrorOr<Deleted> DeleteUser(int id) => Result.Deleted;

// For successful updates
ErrorOr<Updated> UpdateUser(int id, UpdateRequest request) => Result.Updated;

// For successful creations (when you don't need to return the entity)
ErrorOr<Created> CreateUser(CreateRequest request) => Result.Created;

// For successful operations with no specific result
ErrorOr<Success> DoSomething() => Result.Success;
```

---

## Error Types and HTTP Mapping

ErrorOr automatically maps error types to appropriate HTTP status codes:

| Error Type | HTTP Status Code | Description |
|------------|------------------|-------------|
| `Error.Validation()` | 400 Bad Request | Input validation failures |
| `Error.Unauthorized()` | 401 Unauthorized | Authentication required |
| `Error.Forbidden()` | 403 Forbidden | Insufficient permissions |
| `Error.NotFound()` | 404 Not Found | Resource not found |
| `Error.Conflict()` | 409 Conflict | Resource state conflict |
| `Error.Failure()` | 500 Internal Server Error | General failures |
| `Error.Unexpected()` | 500 Internal Server Error | Unexpected errors |

### Creating Errors

```csharp
// Validation error (400)
Error.Validation("User.InvalidEmail", "The email format is invalid.");

// Unauthorized error (401)
Error.Unauthorized("Auth.InvalidToken", "The authentication token is invalid.");

// Forbidden error (403)
Error.Forbidden("Auth.InsufficientPermissions", "You don't have permission to access this resource.");

// NotFound error (404)
Error.NotFound("User.NotFound", "The user was not found.");

// Conflict error (409)
Error.Conflict("User.DuplicateEmail", "A user with this email already exists.");

// Failure error (500)
Error.Failure("Database.ConnectionFailed", "Failed to connect to the database.");

// Unexpected error (500)
Error.Unexpected("Unknown.Error", "An unexpected error occurred.");
```

### Custom Error Codes

You can create errors with custom codes for more specific error handling:

```csharp
// Using the full constructor
var error = Error.Custom(
    type: ErrorType.Validation,
    code: "User.PasswordTooWeak",
    description: "Password must contain at least 8 characters, one uppercase letter, and one number."
);
```

---

## Fluent API

ErrorOr provides a powerful fluent API for chaining operations.

### Then - Chain Operations

Use `Then` to chain operations that depend on the previous result:

```csharp
ErrorOr<User> result = GetUserId(email)
    .Then(id => GetUserById(id))
    .Then(user => ValidateUser(user))
    .Then(user => EnrichUserData(user));
```

Each `Then` only executes if the previous operation succeeded. If any operation returns an error, the chain short-circuits.

### ThenAsync - Async Chain Operations

```csharp
ErrorOr<User> result = await GetUserIdAsync(email)
    .ThenAsync(id => GetUserByIdAsync(id))
    .ThenAsync(user => ValidateUserAsync(user))
    .ThenAsync(user => EnrichUserDataAsync(user));
```

### Else - Handle Errors

Use `Else` to provide fallback values or handle errors:

```csharp
// Provide a fallback value
User user = GetUser(id)
    .Else(User.Guest);

// Provide a fallback based on errors
User user = GetUser(id)
    .Else(errors => CreateDefaultUser(errors.First().Code));

// Provide a fallback ErrorOr
ErrorOr<User> result = GetUser(id)
    .Else(errors => GetCachedUser(id));
```

### ElseAsync - Async Error Handling

```csharp
User user = await GetUserAsync(id)
    .ElseAsync(errors => GetCachedUserAsync(id));
```

### Match - Transform Both Cases

Use `Match` to handle both success and error cases:

```csharp
string message = GetUser(id).Match(
    onValue: user => $"Found user: {user.Name}",
    onError: errors => $"Error: {errors.First().Description}"
);
```

### MatchAsync - Async Match

```csharp
string message = await GetUserAsync(id).MatchAsync(
    onValue: user => $"Found user: {user.Name}",
    onError: errors => $"Error: {errors.First().Description}"
);
```

### MatchFirst - Handle First Error Only

```csharp
string message = GetUser(id).MatchFirst(
    onValue: user => $"Found user: {user.Name}",
    onError: error => $"Error: {error.Description}"
);
```

### Switch - Side Effects

Use `Switch` when you need to perform side effects:

```csharp
GetUser(id).Switch(
    onValue: user => SendWelcomeEmail(user),
    onError: errors => LogErrors(errors)
);
```

### SwitchFirst - First Error Side Effects

```csharp
GetUser(id).SwitchFirst(
    onValue: user => SendWelcomeEmail(user),
    onError: error => LogError(error)
);
```

### Complete Fluent Example

```csharp
public async Task<IActionResult> ProcessOrder(OrderRequest request)
{
    return await ValidateOrder(request)
        .ThenAsync(order => CheckInventory(order))
        .ThenAsync(order => ProcessPayment(order))
        .ThenAsync(order => CreateShipment(order))
        .ThenAsync(order => SendConfirmationEmail(order))
        .MatchAsync(
            onValue: order => Ok(new OrderResponse(order)),
            onError: errors => BadRequest(new ErrorResponse(errors))
        );
}
```

---

## Endpoint Attributes

### HTTP Method Attributes

```csharp
[Get("/path")]           // HTTP GET
[Post("/path")]          // HTTP POST
[Put("/path")]           // HTTP PUT
[Delete("/path")]        // HTTP DELETE
[Patch("/path")]         // HTTP PATCH
```

### Route Parameters

```csharp
// Path parameters
[Get("/users/{id}")]
public static ErrorOr<User> Handle(int id) { }

// Multiple parameters
[Get("/users/{userId}/orders/{orderId}")]
public static ErrorOr<Order> Handle(int userId, int orderId) { }
```

### Query Parameters

Query parameters are automatically bound from method parameters not in the route:

```csharp
[Get("/users")]
public static ErrorOr<PagedResult<User>> Handle(
    int page = 1,
    int pageSize = 10,
    string? search = null)
{
    // ?page=2&pageSize=20&search=john
}
```

### Request Body

For POST, PUT, and PATCH requests, complex types are bound from the request body:

```csharp
[Post("/users")]
public static ErrorOr<User> Handle(CreateUserRequest request) { }

public record CreateUserRequest(string Name, string Email);
```

### Dependency Injection

Services are automatically injected:

```csharp
[Get("/users/{id}")]
public static ErrorOr<User> Handle(
    int id,
    IUserService userService,
    ILogger<GetUserEndpoint> logger)
{
    logger.LogInformation("Getting user {Id}", id);
    return userService.GetById(id);
}
```

### Async Endpoints

```csharp
[Get("/users/{id}")]
public static async Task<ErrorOr<User>> Handle(
    int id,
    IUserService userService,
    CancellationToken cancellationToken)
{
    return await userService.GetByIdAsync(id, cancellationToken);
}
```

---

## Native AOT Support

ErrorOr is fully compatible with .NET Native AOT compilation. The source generator produces code that:

- Uses no reflection
- Is fully trimmer-safe
- Requires no runtime code generation
- Works with `PublishAot=true`

### Enable Native AOT

Simply add to your project file:

```xml
<PropertyGroup>
    <PublishAot>true</PublishAot>
</PropertyGroup>
```

No additional configuration is needed. The source generator automatically produces AOT-compatible code.

### JSON Serialization

For AOT compatibility, ensure your DTOs are JSON-serializable:

```csharp
[JsonSerializable(typeof(UserResponse))]
[JsonSerializable(typeof(CreateUserRequest))]
[JsonSerializable(typeof(ErrorResponse))]
public partial class AppJsonContext : JsonSerializerContext { }
```

Configure in Program.cs:

```csharp
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default);
});
```

---

## Configuration

### Custom Error Response Format

You can customize how errors are serialized:

```csharp
builder.Services.AddErrorOr(options =>
{
    options.ErrorResponseFactory = errors => new
    {
        success = false,
        errors = errors.Select(e => new
        {
            code = e.Code,
            message = e.Description
        })
    };
});
```

### Endpoint Groups

Organize endpoints into groups with common prefixes:

```csharp
app.MapErrorOrEndpoints(options =>
{
    options.GroupPrefix = "/api/v1";
});
```

### Authorization

Apply authorization to endpoints:

```csharp
public class AdminEndpoint
{
    [Get("/admin/users")]
    [Authorize(Roles = "Admin")]
    public static ErrorOr<List<User>> Handle(IUserService userService)
    {
        return userService.GetAllUsers();
    }
}
```

---

## Best Practices

### 1. Use Descriptive Error Codes

```csharp
// Good - specific and descriptive
Error.Validation("User.Email.InvalidFormat", "The email format is invalid.");

// Avoid - too generic
Error.Validation("Invalid", "Invalid input.");
```

### 2. Create Domain-Specific Errors

```csharp
public static class UserErrors
{
    public static Error NotFound(int id) =>
        Error.NotFound("User.NotFound", $"User with ID {id} was not found.");

    public static Error DuplicateEmail(string email) =>
        Error.Conflict("User.DuplicateEmail", $"A user with email '{email}' already exists.");

    public static Error InvalidCredentials =>
        Error.Unauthorized("User.InvalidCredentials", "Invalid username or password.");
}

// Usage
return UserErrors.NotFound(id);
```

### 3. Aggregate Validation Errors

```csharp
public static ErrorOr<ValidatedRequest> Validate(CreateUserRequest request)
{
    var errors = new List<Error>();

    if (string.IsNullOrWhiteSpace(request.Name))
        errors.Add(Error.Validation("User.Name.Required", "Name is required."));

    if (string.IsNullOrWhiteSpace(request.Email))
        errors.Add(Error.Validation("User.Email.Required", "Email is required."));
    else if (!IsValidEmail(request.Email))
        errors.Add(Error.Validation("User.Email.InvalidFormat", "Email format is invalid."));

    if (errors.Count > 0)
        return errors;

    return new ValidatedRequest(request.Name, request.Email);
}
```

### 4. Use Then for Railway-Oriented Programming

```csharp
public ErrorOr<OrderResult> ProcessOrder(OrderRequest request)
{
    return ValidateRequest(request)
        .Then(validated => CheckInventory(validated))
        .Then(available => ReserveItems(available))
        .Then(reserved => ProcessPayment(reserved))
        .Then(paid => CreateOrder(paid));
}
```

### 5. Keep Endpoints Thin

```csharp
// Good - endpoint delegates to service
[Post("/orders")]
public static ErrorOr<OrderResponse> Handle(
    CreateOrderRequest request,
    IOrderService orderService)
{
    return orderService.CreateOrder(request);
}

// Avoid - business logic in endpoint
[Post("/orders")]
public static ErrorOr<OrderResponse> Handle(
    CreateOrderRequest request,
    IDbContext db)
{
    // Lots of business logic here...
}
```

---

## Documentation

For more detailed documentation, see the `/docs` folder:

- [Getting Started Guide](docs/getting-started.md)
- [Error Handling Patterns](docs/error-handling.md)
- [Fluent API Reference](docs/fluent-api.md)
- [Endpoint Configuration](docs/endpoints.md)
- [Native AOT Guide](docs/native-aot.md)
- [Migration Guide](docs/migration.md)
- [API Reference](docs/api-reference.md)

---

## Contributing

Contributions are welcome! Please read our [Contributing Guide](CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## Acknowledgments

Inspired by functional programming concepts and the need for clean, type-safe error handling in .NET applications.
