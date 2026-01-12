# Error Mapping Documentation

This document explains how ErrorOr types map to HTTP status codes in the ErrorOrX library.

## Overview

ErrorOrX provides RFC-compliant error handling that automatically maps domain errors to appropriate HTTP responses:

- **RFC 9110** (HTTP Semantics): Ensures correct HTTP status code usage
- **RFC 7807** (Problem Details): Provides structured error responses via `ProblemDetails`

The library eliminates manual status code management by inferring the correct HTTP response from your domain error types.

---

## Error Type to HTTP Status Mapping

| ErrorType | HTTP Status | Response Body | Description |
|-----------|-------------|---------------|-------------|
| `Validation` | 400 Bad Request | `ValidationProblemDetails` | Input validation failures |
| `Unauthorized` | 401 Unauthorized | None | Authentication required |
| `Forbidden` | 403 Forbidden | None | Insufficient permissions |
| `NotFound` | 404 Not Found | `ProblemDetails` | Resource does not exist |
| `Conflict` | 409 Conflict | `ProblemDetails` | State conflict detected |
| `Failure` | 500 Internal Server Error | `ProblemDetails` | Known operational failure |
| `Unexpected` | 500 Internal Server Error | `ProblemDetails` | Unhandled exception |

### Detailed Explanations

#### ErrorType.Validation -> 400 Bad Request

Validation errors return a `ValidationProblemDetails` response, which extends the standard `ProblemDetails` format with an `errors` dictionary containing field-level validation messages.

```csharp
// Domain error
Error.Validation("User.Email", "Email address is invalid")

// HTTP Response
// Status: 400 Bad Request
// Body:
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "User.Email": ["Email address is invalid"]
  }
}
```

#### ErrorType.Unauthorized -> 401 Unauthorized

Authentication failures return a 401 status with **no response body** for security reasons. Exposing error details could aid attackers in identifying valid accounts or authentication mechanisms.

```csharp
// Domain error
Error.Unauthorized("Auth.InvalidCredentials", "Invalid username or password")

// HTTP Response
// Status: 401 Unauthorized
// Body: (empty)
// Headers: WWW-Authenticate: Bearer (if configured)
```

#### ErrorType.Forbidden -> 403 Forbidden

Authorization failures return a 403 status with **no response body** for security reasons. Revealing why access was denied could expose permission structures or resource existence.

```csharp
// Domain error
Error.Forbidden("Auth.InsufficientRole", "Admin role required")

// HTTP Response
// Status: 403 Forbidden
// Body: (empty)
```

#### ErrorType.NotFound -> 404 Not Found

Resource not found errors return a `ProblemDetails` body describing the missing resource.

```csharp
// Domain error
Error.NotFound("User.NotFound", "User with ID 123 was not found")

// HTTP Response
// Status: 404 Not Found
// Body:
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "User with ID 123 was not found"
}
```

#### ErrorType.Conflict -> 409 Conflict

State conflict errors indicate the request conflicts with the current resource state.

```csharp
// Domain error
Error.Conflict("Order.AlreadyShipped", "Cannot cancel an order that has already shipped")

// HTTP Response
// Status: 409 Conflict
// Body:
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "detail": "Cannot cancel an order that has already shipped"
}
```

#### ErrorType.Failure -> 500 Internal Server Error

Known operational failures (database connection issues, external service failures) return a `ProblemDetails` body.

```csharp
// Domain error
Error.Failure("Database.ConnectionFailed", "Unable to connect to database")

// HTTP Response
// Status: 500 Internal Server Error
// Body:
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "Unable to connect to database"
}
```

#### ErrorType.Unexpected -> 500 Internal Server Error

Unhandled exceptions are wrapped as unexpected errors with a generic `ProblemDetails` response. In production, sensitive exception details are suppressed.

```csharp
// Domain error (typically from exception handling)
Error.Unexpected("System.UnhandledException", "An unexpected error occurred")

// HTTP Response
// Status: 500 Internal Server Error
// Body:
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An unexpected error occurred"
}
```

---

## Custom Error Codes

For scenarios not covered by the standard error types, use `Error.Custom` to specify an explicit HTTP status code:

```csharp
// Custom error with specific status code
Error.Custom(
    statusCode: 429,
    code: "RateLimit.Exceeded",
    description: "Too many requests. Please wait 60 seconds."
)

// HTTP Response
// Status: 429 Too Many Requests
// Body:
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.30",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Too many requests. Please wait 60 seconds."
}
```

Common custom status code scenarios:

| Status Code | Use Case |
|-------------|----------|
| 402 Payment Required | Payment processing failures |
| 422 Unprocessable Entity | Semantic validation errors |
| 429 Too Many Requests | Rate limiting |
| 451 Unavailable For Legal Reasons | Content restrictions |
| 503 Service Unavailable | Maintenance mode |

---

## Validation Error Aggregation

Multiple validation errors are automatically aggregated into a single `ValidationProblemDetails` response. This provides a complete picture of all validation issues in one request.

```csharp
// Multiple validation errors from a command handler
public ErrorOr<User> CreateUser(CreateUserCommand command)
{
    var errors = new List<Error>();

    if (string.IsNullOrEmpty(command.Email))
        errors.Add(Error.Validation("Email", "Email is required"));

    if (command.Email?.Contains("@") != true)
        errors.Add(Error.Validation("Email", "Email must be valid"));

    if (command.Password?.Length < 8)
        errors.Add(Error.Validation("Password", "Password must be at least 8 characters"));

    if (errors.Any())
        return errors;

    // Create user...
}

// HTTP Response
// Status: 400 Bad Request
// Body:
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Email": [
      "Email is required",
      "Email must be valid"
    ],
    "Password": [
      "Password must be at least 8 characters"
    ]
  }
}
```

### Aggregation Rules

1. **Same field, multiple errors**: All error messages are collected into an array under the field key
2. **Different fields**: Each field gets its own key in the `errors` dictionary
3. **Mixed error types**: If validation errors are mixed with other error types, validation errors take precedence for the response format

---

## Result Types

Success responses use semantic HTTP status codes based on the operation type:

| Result Type | HTTP Status | Response Body | Use Case |
|-------------|-------------|---------------|----------|
| `Success` | 200 OK | Entity/DTO | Read operations, general success |
| `Created` | 201 Created | Entity/DTO | Resource creation |
| `Updated` | 204 No Content | None | Resource modification |
| `Deleted` | 204 No Content | None | Resource deletion |

### Examples

#### Success (200 OK)

```csharp
// Handler returns the entity
return user;

// HTTP Response
// Status: 200 OK
// Body: { "id": 1, "email": "user@example.com", "name": "John Doe" }
```

#### Created (201 Created)

```csharp
// Use Results.Created to indicate resource creation
return Results.Created(newUser);

// HTTP Response
// Status: 201 Created
// Headers: Location: /api/users/1
// Body: { "id": 1, "email": "user@example.com", "name": "John Doe" }
```

#### Updated (204 No Content)

```csharp
// Use Results.Updated for successful modifications
return Results.Updated;

// HTTP Response
// Status: 204 No Content
// Body: (empty)
```

#### Deleted (204 No Content)

```csharp
// Use Results.Deleted for successful deletions
return Results.Deleted;

// HTTP Response
// Status: 204 No Content
// Body: (empty)
```

---

## OpenAPI Integration

ErrorOrX automatically generates OpenAPI documentation that reflects the complete set of possible responses for each endpoint.

### Automatic Response Documentation

When using `Results<TSuccess, TError>` union types, the OpenAPI schema includes all possible outcomes:

```csharp
// Endpoint definition
app.MapGet("/users/{id}", async (int id, IMediator mediator) =>
{
    var result = await mediator.Send(new GetUserQuery(id));
    return result.ToResult();
})
.Produces<UserDto>(200)
.ProducesValidationProblem(400)
.ProducesProblem(404)
.ProducesProblem(500);
```

### Generated OpenAPI Schema

```yaml
paths:
  /users/{id}:
    get:
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/UserDto'
        '400':
          description: Bad Request
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ValidationProblemDetails'
        '404':
          description: Not Found
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ProblemDetails'
        '500':
          description: Internal Server Error
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ProblemDetails'
```

### Results Union Type

The `Results<>` type from ASP.NET Core Minimal APIs is used to document all possible response types:

```csharp
// Explicit result type declaration
public Results<Ok<UserDto>, NotFound<ProblemDetails>, ValidationProblem> GetUser(int id)
{
    // Implementation
}
```

This generates comprehensive OpenAPI documentation showing clients exactly what responses to expect.

### Security Considerations in OpenAPI

For `401 Unauthorized` and `403 Forbidden` responses, the OpenAPI schema correctly shows no response body:

```yaml
'401':
  description: Unauthorized
  content: {}
'403':
  description: Forbidden
  content: {}
```

---

## Summary

ErrorOrX provides a consistent, RFC-compliant mapping between domain errors and HTTP responses:

1. **Standard error types** map to appropriate HTTP status codes automatically
2. **Security-sensitive responses** (401, 403) omit response bodies
3. **Validation errors** aggregate into comprehensive `ValidationProblemDetails`
4. **Custom status codes** are supported via `Error.Custom()`
5. **Success types** (Created, Updated, Deleted) use semantic HTTP status codes
6. **OpenAPI integration** documents all possible response types automatically

This approach ensures your API consumers receive predictable, well-documented error responses while your domain code remains focused on business logic.
