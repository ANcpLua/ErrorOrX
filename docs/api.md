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

## Or* Extensions

Fluent extensions for converting nullable values to `ErrorOr<T>`. Error codes are auto-generated from the type name.

```csharp
// Returns Todo if found, or NotFound error with code "Todo.NotFound"
var result = _todos.Find(t => t.Id == id).OrNotFound($"Todo {id} not found");
```

| Extension          | Error Type   | Default Code Pattern   |
|--------------------|--------------|------------------------|
| `.OrNotFound()`    | NotFound     | `{TypeName}.NotFound`  |
| `.OrValidation()`  | Validation   | `{TypeName}.Invalid`   |
| `.OrUnauthorized()`| Unauthorized | `{TypeName}.Unauthorized` |
| `.OrForbidden()`   | Forbidden    | `{TypeName}.Forbidden` |
| `.OrConflict()`    | Conflict     | `{TypeName}.Conflict`  |
| `.OrFailure()`     | Failure      | `{TypeName}.Failure`   |

Works with both reference types and nullable value types:

```csharp
// Reference type
User? user = GetUser(id);
var result = user.OrNotFound("User not found");

// Nullable struct
int? count = GetCount();
var result = count.OrValidation("Count is required");
```

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

---

## Native AOT Support

ErrorOrX fully supports Native AOT compilation.

### Setup

1. Create a `JsonSerializerContext` with `[JsonSerializable]` attributes for your endpoint types:

```csharp
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Todo))]
[JsonSerializable(typeof(Todo[]))]
[JsonSerializable(typeof(List<Todo>))]
[JsonSerializable(typeof(CreateTodoRequest))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
```

2. Register it using the fluent builder:

```csharp
var builder = WebApplication.CreateSlimBuilder(args);

// Fluent builder - CamelCase and IgnoreNulls enabled by default
builder.Services.AddErrorOrEndpoints(options => options
    .UseJsonContext<AppJsonSerializerContext>());

var app = builder.Build();
app.MapErrorOrEndpoints();
app.Run();
```

Optional: if you do not define a `JsonSerializerContext`, the generator emits `ErrorOrJsonContext.g.cs` with all
required types. If you do define one, it emits a helper file listing missing `[JsonSerializable]` types and warns
when the CamelCase policy is missing (EOE040).

Optional: disable generator JSON context emission (you must supply your own context for AOT):

```xml
<PropertyGroup>
    <ErrorOrGenerateJsonContext>false</ErrorOrGenerateJsonContext>
</PropertyGroup>
```

### Available Options

```csharp
services.AddErrorOrEndpoints(options => options
    .UseJsonContext<AppJsonSerializerContext>()  // Register JSON context for AOT
    .WithCamelCase()                              // Use camelCase naming (default: true)
    .WithIgnoreNulls());                          // Ignore null values (default: true)
```

Note: `AddErrorOrEndpoints` is generated only when endpoints include JSON bodies or responses.

Publish with AOT:

```bash
dotnet publish -c Release -r linux-x64 --self-contained
```
