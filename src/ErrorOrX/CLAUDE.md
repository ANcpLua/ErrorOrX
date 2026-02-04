# ErrorOrX Runtime Library

Runtime types for the ErrorOrX library. Target: `net10.0`, Namespace: `ErrorOr`.

## Core Types

### `ErrorOr<T>`

Discriminated union representing success value or error list.

```csharp
ErrorOr<User> result = await GetUserAsync(id);

// Minimal interface (used by generator)
if (result.IsError) return ToProblem(result.Errors);
return TypedResults.Ok(result.Value);

// Fluent API (for application code)
return result
    .Then(user => ValidateUser(user))
    .Else(errors => LogErrors(errors));
```

### `Error`

Structured error with factory methods:

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

### `ErrorType` to HTTP Mapping

| ErrorType    | HTTP | Use Case                            |
|--------------|------|-------------------------------------|
| Validation   | 400  | Input validation failures           |
| Unauthorized | 401  | Authentication required             |
| Forbidden    | 403  | Insufficient permissions            |
| NotFound     | 404  | Resource doesn't exist              |
| Conflict     | 409  | State conflict (duplicate, version) |
| Failure      | 500  | Known operational failure           |
| Unexpected   | 500  | Unhandled/unknown errors            |

### `Result` Marker Types

```csharp
Result.Success   // 200 OK (no body)
Result.Created   // 201 Created (no body)
Result.Updated   // 204 No Content
Result.Deleted   // 204 No Content
```

## Fluent API

| Method   | Purpose              | Example                                          |
|----------|----------------------|--------------------------------------------------|
| `Then`   | Chain on success     | `.Then(user => ValidateAge(user))`               |
| `Else`   | Handle errors        | `.Else(errors => DefaultUser)`                   |
| `Match`  | Transform both cases | `.Match(ok => ..., err => ...)`                  |
| `Switch` | Side effects         | `.Switch(ok => Log(ok), err => Log(err))`        |
| `FailIf` | Conditional failure  | `.FailIf(u => u.IsDeleted, Error.NotFound(...))` |

### `Or*` Extensions (Nullable to ErrorOr)

```csharp
// Auto-generates error code from type name
_todos.Find(t => t.Id == id).OrNotFound($"Todo {id} not found");
user.OrUnauthorized("Invalid credentials");
record.OrValidation("Record is invalid");
```

| Extension           | Error Type   | Auto-Generated Code       |
|---------------------|--------------|---------------------------|
| `.OrNotFound()`     | NotFound     | `{TypeName}.NotFound`     |
| `.OrValidation()`   | Validation   | `{TypeName}.Invalid`      |
| `.OrUnauthorized()` | Unauthorized | `{TypeName}.Unauthorized` |
| `.OrForbidden()`    | Forbidden    | `{TypeName}.Forbidden`    |
| `.OrConflict()`     | Conflict     | `{TypeName}.Conflict`     |
| `.OrFailure()`      | Failure      | `{TypeName}.Failure`      |
| `.OrError(Error)`   | Any          | User-provided             |

## File Structure

```
src/ErrorOrX/
  Error.cs                      # Error record struct and factories
  ErrorOr.cs                    # Main ErrorOr<T> struct
  ErrorOr.Then.cs               # Then chaining
  ErrorOr.Else.cs               # Else fallback
  ErrorOr.Match.cs              # Match transformation
  ErrorOr.Switch.cs             # Switch side effects
  ErrorOr.FailIf.cs             # Conditional failure
  ErrorOr.OrExtensions.cs       # Or* nullable conversion
  ErrorOr.Equality.cs           # IEquatable implementation
  ErrorOr.ImplicitConverters.cs # T -> ErrorOr<T>, Error -> ErrorOr<T>
  ErrorType.cs                  # ErrorType enum
  Results.cs                    # Marker types (Deleted, Updated, etc.)
  IErrorOr.cs                   # Non-generic interface
```

## Package Flow

`ErrorOrX.Generators` declares dependency on `ErrorOrX` with `PrivateAssets="none"`, ensuring this runtime DLL flows to
consuming projects.
