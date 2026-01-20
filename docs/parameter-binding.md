# Parameter Binding

The generator automatically binds method parameters from various sources.

## Binding Priority

1. **Explicit Attributes** - Always win
2. **Special Types** - Auto-detected
3. **Route Parameters** - Name matches route template
4. **Query Parameters** - Primitives not in route
5. **Custom Binding** - TryParse/BindAsync/IBindableFromHttpContext
6. **Smart Inference** - HTTP method-aware (v2.3.0+)

## Explicit Binding Attributes

```csharp
[Get("/users/{id}")]
public static ErrorOr<User> Get(
    [FromRoute] int id,
    [FromQuery] string? search,
    [FromHeader] string authorization,
    [FromServices] IUserService svc)
```

| Attribute            | Source                    |
|----------------------|---------------------------|
| `[FromRoute]`        | Route template segment    |
| `[FromQuery]`        | Query string              |
| `[FromHeader]`       | HTTP header               |
| `[FromBody]`         | Request body (JSON)       |
| `[FromForm]`         | Form data                 |
| `[FromServices]`     | DI container              |
| `[FromKeyedServices]`| Keyed DI service          |
| `[AsParameters]`     | Expand constructor params |

## Special Types (Auto-Detected)

```csharp
public static ErrorOr<File> Upload(
    HttpContext ctx,           // Injected
    CancellationToken ct,      // Request cancellation
    IFormFile file,            // Single file
    IFormFileCollection files, // Multiple files
    IFormCollection form,      // Raw form collection
    Stream body,               // Raw body stream
    PipeReader reader)         // Body as PipeReader
```

## Smart Parameter Inference (v2.3.0+)

The generator infers binding based on HTTP method and type:

### POST/PUT/PATCH with Complex Types

```csharp
[Post("/users")]
public static ErrorOr<User> Create(CreateUserRequest req)
// req is automatically bound from body (no [FromBody] needed)
```

### Non-Body Methods with Complex Types

```csharp
[Get("/users")]
public static ErrorOr<List<User>> Search(SearchFilter filter)
// WARNING EOE025: Explicit binding required for DTO-like types
```

For bodyless/custom methods, the generator will not infer body binding.
If the type does not look like a DI service, it emits EOE025 and treats the parameter as a service.

Fix by adding explicit binding:

```csharp
[Get("/users")]
public static ErrorOr<List<User>> Search([AsParameters] SearchFilter filter)

// Or explicitly allow body binding (even on GET)
[Get("/users")]
public static ErrorOr<List<User>> Search([FromBody] SearchFilter filter)
```

### Service Detection

Interface types and common patterns are detected as services:

```csharp
[Get("/todos")]
public static ErrorOr<List<Todo>> GetAll(ITodoService svc)
// svc resolved from DI (no [FromServices] needed)
```

Detected patterns:
- Interface types (`ITodoService`)
- Abstract types
- `*Repository`, `*Handler`, `*Manager`, `*Provider`, `*Factory`, `*Client`
- `*Context` types that start with or contain `Db` (DbContext patterns)

## AsParameters Expansion

```csharp
public record SearchParams(int Page, int PageSize, string? Query);

[Get("/users")]
public static ErrorOr<List<User>> Search([AsParameters] SearchParams p)
// Binds: ?page=1&pageSize=10&query=test
```

Notes:
- Uses the public constructor with the most parameters (EOE014 if none).
- Each constructor parameter is classified using the same binding rules.
- Only class/struct types are allowed (EOE013 otherwise).

## Custom Binding (TryParse/BindAsync)

The generator detects custom binding patterns:
- `static bool TryParse(string|ReadOnlySpan<char>, out T)` (optionally with `IFormatProvider`)
- `static Task<T> or ValueTask<T> BindAsync(HttpContext[, ParameterInfo])`
- `IBindableFromHttpContext<T>`

Rules:
- TryParse types bind from route when the parameter name matches the route; otherwise query.
- BindAsync/IBindableFromHttpContext bind from query by default; route binding requires a primitive or TryParse type.

## Form Binding

`[FromForm]` supports:
- Scalars and collections of primitives
- `IFormFile`, `IFormFileCollection`, `IFormCollection`
- Complex DTOs, using the public constructor with the most parameters

Notes:
- Only one body source is allowed across parameters (body, form, stream, PipeReader). Multiple sources emit EOE006.
