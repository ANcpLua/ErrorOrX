# API Reference

## ErrorOr\<T\>

The core discriminated union type representing either a success value or error collection.

### Properties

| Property   | Type                      | Description                    |
|------------|---------------------------|--------------------------------|
| `IsError`  | `bool`                    | True if contains errors        |
| `Value`    | `T`                       | Success value (throws if error)|
| `Errors`   | `IReadOnlyList<Error>`    | Error collection               |
| `FirstError` | `Error`                 | First error (throws if success)|

### Creating Values

```csharp
// Implicit conversion from T
ErrorOr<int> success = 42;

// From error
ErrorOr<User> error = Error.NotFound("User.NotFound", "Not found");

// From multiple errors
ErrorOr<User> errors = new List<Error> { error1, error2 };
```

### Fluent Methods

#### Then - Chain Operations

```csharp
ErrorOr<Order> result = GetOrder(id)
    .Then(order => ValidateOrder(order))
    .Then(order => ProcessOrder(order));

// Async
var result = await GetOrderAsync(id)
    .ThenAsync(order => ValidateAsync(order));
```

#### Else - Provide Fallbacks

```csharp
User user = GetUser(id).Else(User.Guest);
User user = GetUser(id).Else(errors => CreateDefault(errors));

// Async
var user = await GetUserAsync(id).ElseAsync(FetchBackupAsync);
```

#### Match - Transform Both Cases

```csharp
IResult response = GetUser(id).Match(
    user => Results.Ok(user),
    errors => Results.BadRequest(errors));

// Async
var response = await GetUserAsync(id).MatchAsync(
    async user => await FormatAsync(user),
    errors => Task.FromResult(FormatErrors(errors)));
```

#### Switch - Side Effects

```csharp
GetUser(id).Switch(
    user => SendWelcomeEmail(user),
    errors => LogErrors(errors));
```

#### FailIf - Conditional Failure

```csharp
ErrorOr<User> result = GetUser(id)
    .FailIf(user => user.IsDeleted, Error.NotFound())
    .FailIf(user => !user.IsActive, Error.Forbidden());
```

---

## Error

Structured error value with code, description, and type.

### Factory Methods

```csharp
Error.Validation(string code, string description)
Error.NotFound(string code, string description)
Error.Conflict(string code, string description)
Error.Unauthorized(string code, string description)
Error.Forbidden(string code, string description)
Error.Failure(string code, string description)
Error.Unexpected(string code, string description)
Error.Custom(int type, string code, string description)
```

### Properties

| Property      | Type                         | Description              |
|---------------|------------------------------|--------------------------|
| `Code`        | `string`                     | Error identifier         |
| `Description` | `string`                     | Human-readable message   |
| `Type`        | `ErrorType`                  | Error category           |
| `NumericType` | `int`                        | Custom type (for Custom) |
| `Metadata`    | `Dictionary<string, object>` | Additional data          |

---

## Result Markers

Built-in types for semantic HTTP responses.

```csharp
Result.Success   // 200 OK (no body)
Result.Created   // 201 Created (no body)
Result.Updated   // 204 No Content
Result.Deleted   // 204 No Content
```

Usage:

```csharp
[Delete("/users/{id}")]
public static ErrorOr<Deleted> Delete(int id, IUserService svc)
    => svc.Delete(id) ? Result.Deleted : Error.NotFound();
```

---

## Endpoint Attributes

| Attribute                | HTTP Method |
|--------------------------|-------------|
| `[Get("/path")]`         | GET         |
| `[Post("/path")]`        | POST        |
| `[Put("/path")]`         | PUT         |
| `[Delete("/path")]`      | DELETE      |
| `[Patch("/path")]`       | PATCH       |

Route parameters use `{name}` syntax with optional constraints:

```csharp
[Get("/users/{id:int}")]
[Get("/posts/{slug:alpha}")]
[Get("/files/{*path}")]  // Catch-all
```
