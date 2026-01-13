# Parameter Binding

The generator automatically binds method parameters from various sources.

## Binding Priority

1. **Explicit Attributes** - Always win
2. **Special Types** - Auto-detected
3. **Route Parameters** - Name matches route template
4. **Query Parameters** - Primitives not in route
5. **Smart Inference** - HTTP method-aware (v2.3.0+)

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
| `[AsParameters]`     | Expand type properties    |

## Special Types (Auto-Detected)

```csharp
public static ErrorOr<File> Upload(
    HttpContext ctx,           // Injected
    CancellationToken ct,      // Request cancellation
    IFormFile file,            // Single file
    IFormFileCollection files, // Multiple files
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

### GET/DELETE with Complex Types

```csharp
[Get("/users")]
public static ErrorOr<List<User>> Search(SearchFilter filter)
// ERROR EOE025: Requires explicit binding attribute
```

Fix by adding explicit binding:

```csharp
[Get("/users")]
public static ErrorOr<List<User>> Search([FromQuery] SearchFilter filter)

// Or expand properties
[Get("/users")]
public static ErrorOr<List<User>> Search([AsParameters] SearchFilter filter)
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
- `*Repository`, `*Handler`, `*Manager`, `*Provider`, `*Factory`, `*Client`

## Legacy Behavior

To restore pre-2.3.0 behavior (all unclassified parameters from DI):

```xml
<PropertyGroup>
  <ErrorOrLegacyParameterBinding>true</ErrorOrLegacyParameterBinding>
</PropertyGroup>
```

## AsParameters Expansion

```csharp
public record SearchParams(int Page, int PageSize, string? Query);

[Get("/users")]
public static ErrorOr<List<User>> Search([AsParameters] SearchParams p)
// Binds: ?page=1&pageSize=10&query=test
```
