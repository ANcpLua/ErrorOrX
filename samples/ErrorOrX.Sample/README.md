# ErrorOrX Sample Application

This sample demonstrates all key features of **ErrorOrX** - a Railway-Oriented Programming library for .NET with
source-generated ASP.NET Core Minimal API integration.

## What This Sample Demonstrates

### ✅ Core Features

- **ErrorOr<T> Return Types** - Discriminated unions for success/error handling
- **Source-Generated Endpoints** - Zero-boilerplate Minimal API registration
- **Smart Parameter Binding** - Automatic inference of `[FromBody]`, `[FromRoute]`, `[FromServices]`
- **Native AOT Support** - Custom `JsonSerializerContext` for reflection-free serialization
- **OpenAPI Integration** - Typed `Results<...>` unions for complete API documentation
- **[ReturnsError] Attributes** - Interface method error documentation for TypeScript-like type safety

### ✅ HTTP Methods

- `GET` - Retrieve todos
- `POST` - Create new todos
- `PUT` - Update existing todos
- `DELETE` - Remove todos

### ✅ ErrorOr Extensions

- `.OrNotFound()` - Convert nullable to `ErrorOr<T>` with NotFound error (TodoService.cs:20)
- Automatic error code generation from type names (`"Todo.NotFound"`)

### ✅ Result Markers

- `Updated` - 200 OK for successful updates
- `Deleted` - 204 No Content for successful deletions

## Project Structure

```
ErrorOrX.Sample/
├── Program.cs                      # App configuration & endpoint registration
├── AppJsonSerializerContext.cs     # AOT-compatible JSON serialization
├── TodoApi.cs                      # HTTP endpoint handlers
└── Domain/
    ├── ITodoService.cs             # Interface with [ReturnsError] attributes
    ├── TodoService.cs              # Implementation using .OrNotFound()
    └── Todo.cs                     # Domain models
```

## Running the Sample

```bash
# From repository root
cd samples/ErrorOrX.Sample
dotnet run

# Test endpoints
curl http://localhost:5000/api/todos
curl http://localhost:5000/api/todos/{guid}
```

### OpenAPI Documentation

Navigate to `/openapi/v1.json` to see the generated OpenAPI specification with complete response types.

## Key Code Snippets

### 1. Endpoint Handler (TodoApi.cs)

```csharp
/// <summary>Get todo by ID.</summary>
[Get("/api/todos/{id:guid}")]
public static Task<ErrorOr<Todo>> GetById(
    [FromRoute] Guid id,
    [FromServices] ITodoService svc,
    CancellationToken ct) =>
    svc.GetByIdAsync(id, ct);
```

**What happens:**

1. Generator creates `app.MapGet("/api/todos/{id:guid}", ...)`
2. Extracts `id` from route
3. Injects `ITodoService` from DI
4. Maps `ErrorOr<Todo>` to:
    - `200 OK` with `Todo` JSON body (success)
    - `404 Not Found` with `ProblemDetails` (error)

### 2. Interface with [ReturnsError] (ITodoService.cs)

```csharp
public interface ITodoService
{
    /// <summary>Get todo by ID. Returns NotFound if not exists.</summary>
    [ReturnsError(ErrorType.NotFound, "Todo.NotFound")]
    Task<ErrorOr<Todo>> GetByIdAsync(Guid id, CancellationToken ct);
}
```

**What happens:**

- Generator reads `[ReturnsError]` attributes
- Produces `Results<Ok<Todo>, NotFound<ProblemDetails>>` for OpenAPI
- Documents all possible error responses

### 3. Using .OrNotFound() Extension (TodoService.cs)

```csharp
public async Task<ErrorOr<Todo>> GetByIdAsync(Guid id, CancellationToken ct)
{
    await Task.Delay(10, ct);
    return _todos.Find(t => t.Id == id).OrNotFound($"Todo {id} not found");
}
```

**What happens:**

- If `Find()` returns `null`, `.OrNotFound()` creates `Error.NotFound("Todo.NotFound", message)`
- Error code `"Todo.NotFound"` is auto-generated from type name `Todo`
- Returns `ErrorOr<Todo>` with either the todo or the error

### 4. Custom JSON Context (AppJsonSerializerContext.cs)

```csharp
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Todo))]
[JsonSerializable(typeof(ProblemDetails))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
```

**What happens:**

- System.Text.Json source generator creates AOT-compatible serializers at compile time
- No reflection needed at runtime
- Works with `PublishAot=true`

### 5. Endpoint Registration (Program.cs)

```csharp
builder.Services.AddErrorOrEndpoints(static options => options
    .UseJsonContext<AppJsonSerializerContext>());

app.MapErrorOrEndpoints(); // Auto-generated extension method
```

**What happens:**

- `AddErrorOrEndpoints()` registers the custom JSON context
- `MapErrorOrEndpoints()` registers all endpoints from classes with `[Get]`, `[Post]`, etc.
- Zero manual endpoint registration needed

## Generated Code

With `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>`, generated code is written to:

```
obj/GeneratedFiles/ErrorOrX.Generators/
├── ErrorOrEndpointMappings.cs         # MapErrorOrEndpoints() implementation
├── ErrorOrEndpointAttributes.Mappings.g.cs  # Attribute definitions
└── OpenApiTransformers.g.cs           # XML doc → OpenAPI integration
```

## Response Examples

### Success Response (200 OK)

```bash
GET /api/todos/550e8400-e29b-41d4-a716-446655440000
```

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "title": "Walk the dog",
  "isComplete": false
}
```

### Error Response (404 Not Found)

```bash
GET /api/todos/00000000-0000-0000-0000-000000000000
```

```json
{
  "type": "https://httpstatuses.io/404",
  "title": "Not Found",
  "status": 404,
  "detail": "Todo 00000000-0000-0000-0000-000000000000 not found",
  "code": "Todo.NotFound",
  "traceId": "00-abc123..."
}
```

## Next Steps

1. **Add your own endpoints** - Create a new static class with `[Get]`, `[Post]`, etc.
2. **Explore error types** - Use `Error.Validation()`, `Error.Conflict()`, etc.
3. **Try smart binding** - Remove explicit `[FromBody]`/`[FromRoute]` and let the generator infer
4. **Add validation** - Use `.FailIf()` for conditional errors
5. **Chain operations** - Use `.Then()`, `.Else()`, `.Match()` for Railway-Oriented Programming

## Learn More

- [Main Documentation](../../README.md)
- [Parameter Binding Guide](../../docs/parameter-binding.md)
- [Diagnostics Reference](../../docs/diagnostics.md)
- [API Reference](../../docs/api.md)
