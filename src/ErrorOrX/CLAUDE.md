---
See [Root CLAUDE.md](../CLAUDE.md) for project context.
---

# ErrorOrX Runtime Library

This project contains the runtime types for the ErrorOrX library.

## Package Details

- **PackageId**: `ErrorOrX`
- **Target**: `net10.0`
- **Namespace**: `ErrorOr`

## Core Types

### `ErrorOr<T>`

The discriminated union type representing either a success value or a list of errors.

```csharp
ErrorOr<User> result = await GetUserAsync(id);

// Pattern matching
return result.Match(
    user => Ok(user),
    errors => BadRequest(errors));

// Fluent API
return result
    .Then(user => ValidateUser(user))
    .Then(user => SaveUser(user))
    .Else(errors => LogErrors(errors));
```

### `Error`

Error value with structured information:

```csharp
public readonly record struct Error(
    string Code,
    string Description,
    ErrorType Type,
    Dictionary<string, object>? Metadata = null,
    int NumericType = 0);
```

Factory methods:

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

### `ErrorType`

Enum of error categories mapped to HTTP status codes:

| ErrorType      | HTTP Status | Use Case                            |
|----------------|-------------|-------------------------------------|
| `Validation`   | 400         | Input validation failures           |
| `Unauthorized` | 401         | Authentication required             |
| `Forbidden`    | 403         | Insufficient permissions            |
| `NotFound`     | 404         | Resource doesn't exist              |
| `Conflict`     | 409         | State conflict (duplicate, version) |
| `Failure`      | 500         | Known operational failure           |
| `Unexpected`   | 500         | Unhandled/unknown errors            |

### `Result`

Factory for built-in success marker types:

```csharp
Result.Success   // 200 OK (no body)
Result.Created   // 201 Created (no body)
Result.Updated   // 204 No Content
Result.Deleted   // 204 No Content
```

Usage with explicit response semantics:

```csharp
[Delete("/todos/{id}")]
public static ErrorOr<Deleted> Delete(int id, ITodoService svc)
    => svc.Delete(id)
        ? Result.Deleted
        : Error.NotFound("Todo.NotFound", $"Todo {id} not found");

[Post("/todos")]
public static ErrorOr<Created> Create(CreateTodoRequest req)
    => Result.Created; // 201 with no body
```

## Fluent API Extensions

### `Then` - Chain operations on success

```csharp
ErrorOr<User> result = GetUser(id)
    .Then(user => ValidateAge(user))      // ErrorOr<User> → ErrorOr<User>
    .Then(user => EnrichProfile(user))    // ErrorOr<User> → ErrorOr<Profile>
    .ThenAsync(profile => SaveAsync(profile)); // Async variant
```

### `Else` - Handle errors

```csharp
User user = GetUser(id)
    .Else(errors => DefaultUser)           // Provide fallback
    .ElseAsync(errors => FetchBackup());   // Async fallback
```

### `Match` - Transform both cases

```csharp
IActionResult result = GetUser(id).Match(
    user => Ok(user),
    errors => BadRequest(errors.First().Description));
```

### `Switch` - Side effects

```csharp
GetUser(id).Switch(
    user => Console.WriteLine($"Found: {user.Name}"),
    errors => Logger.LogError(errors));
```

### `FailIf` - Conditional failure

```csharp
ErrorOr<User> result = GetUser(id)
    .FailIf(user => user.IsDeleted, Error.NotFound("User.Deleted", "User was deleted"))
    .FailIf(user => !user.IsActive, Error.Forbidden("User.Inactive", "User is inactive"));
```

### `Or*` - Nullable-to-ErrorOr conversion

Convert nullable values to `ErrorOr<T>` with auto-generated error codes:

```csharp
// Auto-generates error code from type name (e.g., "Todo.NotFound")
return _todos.Find(t => t.Id == id).OrNotFound($"Todo {id} not found");
return user.OrUnauthorized("Invalid credentials");
return record.OrValidation("Record is invalid");

// Custom errors
return value.OrError(Error.Custom(422, "Custom.Code", "Custom message"));
return value.OrError(() => BuildExpensiveError());  // Lazy evaluation
```

| Extension           | Error Type   | HTTP | Auto-Generated Code Pattern |
|---------------------|--------------|------|-----------------------------|
| `.OrNotFound()`     | NotFound     | 404  | `{TypeName}.NotFound`       |
| `.OrValidation()`   | Validation   | 400  | `{TypeName}.Invalid`        |
| `.OrUnauthorized()` | Unauthorized | 401  | `{TypeName}.Unauthorized`   |
| `.OrForbidden()`    | Forbidden    | 403  | `{TypeName}.Forbidden`      |
| `.OrConflict()`     | Conflict     | 409  | `{TypeName}.Conflict`       |
| `.OrFailure()`      | Failure      | 500  | `{TypeName}.Failure`        |
| `.OrUnexpected()`   | Unexpected   | 500  | `{TypeName}.Unexpected`     |
| `.OrError(Error)`   | Any          | Any  | User-provided               |
| `.OrError(Func)`    | Any          | Any  | User-provided (lazy)        |

## Dependencies

- `Microsoft.AspNetCore.App` framework reference (for `TypedResults` helpers in endpoints)

## Consumers

This package is automatically referenced when consumers add `ErrorOrX.Generators`. The generator package declares:

```xml
<PackageReference Include="ErrorOrX" PrivateAssets="none" />
```

This ensures `ErrorOrX.dll` flows to the consuming project's output.

## File Structure

```
src/ErrorOrX/
├── Error.cs                    # Error record struct and factory methods
├── ErrorOr.cs                  # Main ErrorOr<T> struct
├── ErrorOr.Else.cs             # Else/ElseAsync extensions
├── ErrorOr.ElseExtensions.cs   # Additional Else overloads
├── ErrorOr.Equality.cs         # IEquatable implementation
├── ErrorOr.FailIf.cs           # FailIf conditional failure
├── ErrorOr.FailIfExtensions.cs # FailIf extensions
├── ErrorOr.ImplicitConverters.cs # T → ErrorOr<T>, Error → ErrorOr<T>
├── ErrorOr.Match.cs            # Match transformation
├── ErrorOr.MatchExtensions.cs  # Match extensions
├── ErrorOr.OrExtensions.cs     # Or* nullable-to-ErrorOr extensions
├── ErrorOr.Switch.cs           # Switch side effects
├── ErrorOr.SwitchExtensions.cs # Switch extensions
├── ErrorOr.Then.cs             # Then chaining
├── ErrorOr.ThenExtensions.cs   # Then extensions
├── ErrorOr.ToErrorOrExtensions.cs # ToErrorOr() helpers
├── ErrorOrFactory.cs           # ErrorOr static factory
├── ErrorType.cs                # ErrorType enum
├── IErrorOr.cs                 # Non-generic interface
├── Results.cs                  # Result marker types (Deleted, Updated, etc.)
└── EmptyErrors.cs              # Empty error collection singleton
```


<claude-mem-context>
# Recent Activity

<!-- This section is auto-generated by claude-mem. Edit content outside the tags. -->

*No recent activity*
</claude-mem-context>