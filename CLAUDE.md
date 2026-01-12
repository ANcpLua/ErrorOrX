# CLAUDE.md - ErrorOrX

> **Operational brain for the ErrorOrX ecosystem.** Read this completely before any task.

---

## Project Status

| Version | Status | Description |
|---------|--------|-------------|
| **v2.0.0** | **COMPLETE** | Single package migration, namespace simplification, auto AOT |
| **v2.1.0** | **COMPLETE** | BCL middleware attribute support |

---

## Architecture (v2.0.0+)

### Package Structure

```
ErrorOr.nupkg (SINGLE PACKAGE)
├── lib/
│   ├── net10.0/
│   │   └── ErrorOr.dll              ← Runtime (ErrorOr<T>, Error, Attributes)
│   └── netstandard2.0/
│       └── ErrorOr.dll              ← Polyfill support
└── analyzers/dotnet/cs/
    └── ErrorOr.dll                  ← Generator + Analyzers (bundled)
```

### Source Structure

```
src/ErrorOr/
├── ErrorOr.csproj                   # Main project (multi-target)
├── Runtime/                         # ErrorOr<T>, Error, markers
│   ├── ErrorOr.cs                   # Core struct
│   ├── ErrorOr.*.cs                 # Partial files (Match, Switch, Then, Else, etc.)
│   ├── Error.cs
│   ├── ErrorType.cs
│   ├── IErrorOr.cs
│   ├── EmptyErrors.cs
│   └── Results.cs                   # Success, Created, Updated, Deleted markers
├── Attributes/                      # Endpoint attributes
│   └── Attributes.cs                # [Get], [Post], [ProducesError], etc.
├── Generators/                      # Source generators
│   ├── ErrorOrEndpointGenerator.*.cs
│   ├── OpenApiTransformerGenerator.cs
│   ├── EndpointModels.cs
│   ├── ResultsUnionTypeBuilder.cs
│   ├── ErrorOrContext.cs
│   └── WellKnownTypes.cs
└── Analyzers/
    ├── Descriptors.cs
    └── ErrorOrEndpointAnalyzer.cs
```

---

## Consumer Experience

### Installation

```xml
<PackageReference Include="ErrorOr" Version="2.1.0" />
```

### Usage

```csharp
using ErrorOr;

// Program.cs - Zero boilerplate
var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddOpenApi();

var app = builder.Build();
app.MapOpenApi();
app.MapErrorOrEndpoints();  // Auto-registers all endpoints
app.Run();
```

```csharp
// TodoApi.cs - Clean, functional
using ErrorOr;

public static class TodoApi
{
    [Get("/todos")]
    public static ErrorOr<List<Todo>> GetAll() => _todos;

    [Get("/todos/{id}")]
    public static ErrorOr<Todo> GetById(int id) =>
        _todos.Find(t => t.Id == id) is { } todo
            ? todo
            : Error.NotFound("Todo.NotFound", $"Todo {id} not found");

    [Post("/todos")]
    [Authorize("Admin")]                    // v2.1: Auto-emits .RequireAuthorization("Admin")
    [EnableRateLimiting("fixed")]           // v2.1: Auto-emits .RequireRateLimiting("fixed")
    public static ErrorOr<Todo> Create(CreateTodoRequest req)
    {
        var todo = new Todo(_todos.Count + 1, req.Title);
        _todos.Add(todo);
        return todo;  // 201 Created with Location header (auto)
    }
}
```

---

## v2.1.0 Middleware Attribute Support

The generator automatically detects BCL middleware attributes and emits corresponding fluent calls:

| Attribute | Emitted Call | Union Type Addition |
|-----------|--------------|---------------------|
| `[Authorize]` | `.RequireAuthorization()` | 401, 403 |
| `[Authorize("Policy")]` | `.RequireAuthorization("Policy")` | 401, 403 |
| `[AllowAnonymous]` | `.AllowAnonymous()` | - |
| `[EnableRateLimiting("policy")]` | `.RequireRateLimiting("policy")` | 429 |
| `[DisableRateLimiting]` | `.DisableRateLimiting()` | - |
| `[OutputCache]` | `.CacheOutput()` | - |
| `[OutputCache(Duration = 60)]` | `.CacheOutput(p => p.Expire(...))` | - |
| `[OutputCache(PolicyName = "x")]` | `.CacheOutput("x")` | - |
| `[EnableCors("policy")]` | `.RequireCors("policy")` | - |
| `[DisableCors]` | `.DisableCors()` | - |

### Generated Output Example

```csharp
// With [Authorize("Admin")] and [EnableRateLimiting("fixed")]
app.MapMethods(@"/todos", new[] { "POST" }, (Delegate)Invoke_Ep3)
    .WithName("TodoApi_Create")
    .WithTags("TodoApi")
    .Accepts<CreateTodoRequest>("application/json")
    .RequireAuthorization("Admin")      // From [Authorize("Admin")]
    .RequireRateLimiting("fixed");      // From [EnableRateLimiting("fixed")]
```

### Results<> Union Type

When middleware is detected, union includes appropriate types:

```csharp
Results<
    Created<Todo>,                      // 1 - success
    BadRequest<ProblemDetails>,         // 2 - binding
    ValidationProblem,                  // 3 - validation
    UnauthorizedHttpResult,             // 4 - from [Authorize]
    ForbidHttpResult,                   // 5 - from [Authorize]
    StatusCodeHttpResult,               // 6 - from [EnableRateLimiting] (429)
    InternalServerError<ProblemDetails> // 7 - fallback
>
```

**Note:** Results<> maxes at 8 type parameters. If exceeded, falls back to `IResult`.

---

## Development Philosophy

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         ROOT-FIRST, LEAF-LAST                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   1. Define the TRUTH in Models.cs (data structures, mappings)              │
│   2. Emitter.cs ONLY USES Models.cs (never duplicates logic)                │
│   3. Generated code is the LEAF (derived, not authoritative)                │
│                                                                             │
│   Spec compliance: ERROROR_TYPEDRESULTS_SPEC.md is the contract.            │
│   Code = OpenAPI = Runtime. Any deviation = Bug OR documented decision.     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Build & Test Commands

```bash
# Build
dotnet build ErrorOrX.slnx

# Test
dotnet test ErrorOrX.slnx

# Test specific project
dotnet test --project tests/ErrorOr.Tests/ErrorOr.Tests.csproj

# Package
dotnet pack src/ErrorOr/ErrorOr.csproj -o ./nupkgs

# Verify package contents
unzip -l nupkgs/ErrorOr.*.nupkg | grep -E "(lib/|analyzers/)"
```

---

## ErrorType → HTTP Status Code Mapping (RFC 9110)

**CANONICAL MAPPING - DO NOT CHANGE WITHOUT RFC JUSTIFICATION**

| ErrorType | HTTP | TypedResults | Has Body |
|-----------|------|--------------|----------|
| `Validation` | 400 | `ValidationProblem` | Yes |
| `Unauthorized` | 401 | `UnauthorizedHttpResult` | No |
| `Forbidden` | 403 | `ForbidHttpResult` | No |
| `NotFound` | 404 | `NotFound<ProblemDetails>` | Yes |
| `Conflict` | 409 | `Conflict<ProblemDetails>` | Yes |
| `Failure` | **500** | `InternalServerError<ProblemDetails>` | Yes |
| `Unexpected` | **500** | `InternalServerError<ProblemDetails>` | Yes |
| _default_ | **500** | `InternalServerError<ProblemDetails>` | Yes |

### Success Mappings

| SuccessKind | HTTP | TypedResult |
|-------------|------|-------------|
| GET with payload | 200 | `Ok<T>` |
| POST with payload | 201 | `Created<T>` + Location header |
| Updated | 204 | `NoContent` |
| Deleted | 204 | `NoContent` |

---

## Diagnostic Reference

| ID | Severity | Description |
|----|----------|-------------|
| EOE001 | Error | Invalid return type (must be ErrorOr<T> or Task<ErrorOr<T>>) |
| EOE002 | Error | Handler must be static |
| EOE003 | Warning | Route parameter not bound |
| EOE004 | Error | Duplicate route pattern |
| EOE005 | Error | Invalid route pattern syntax |
| EOE006 | Error | Multiple body sources |
| EOE007 | Warning | Missing JSON context registration |
| EOE008 | Warning | Undocumented custom error (missing [ProducesError]) |
| EOE026 | Warning | Error.Custom without [ProducesError] |
| EOE030 | Warning | Union type exceeds BCL limit |
| EOE033 | Warning | Interface call without [ReturnsError] documentation |

---

## Attributes Reference

| Attribute | Target | Purpose |
|-----------|--------|---------|
| `[Get("/path")]` | Method | HTTP GET endpoint |
| `[Post("/path")]` | Method | HTTP POST endpoint |
| `[Put("/path")]` | Method | HTTP PUT endpoint |
| `[Delete("/path")]` | Method | HTTP DELETE endpoint |
| `[Patch("/path")]` | Method | HTTP PATCH endpoint |
| `[ProducesError(ErrorType, code)]` | Endpoint | Document possible errors |
| `[ProducesError(statusCode, code)]` | Endpoint | Document custom HTTP status |
| `[ReturnsError(ErrorType, code)]` | Interface | Contract-level error docs |
| `[AcceptedResponse]` | Endpoint | Return 202 Accepted instead of 201 |

---

## Key Files

| File | Purpose |
|------|---------|
| `EndpointModels.cs` | Data structures for endpoint descriptors |
| `ErrorOrEndpointGenerator.Initialize.cs` | Generator entry point, pipeline setup |
| `ErrorOrEndpointGenerator.Extractor.cs` | Extracts info from method symbols |
| `ErrorOrEndpointGenerator.Emitter.cs` | Generates source code output |
| `ResultsUnionTypeBuilder.cs` | Computes Results<> union types |
| `WellKnownTypes.cs` | Centralized type metadata names |
| `ErrorOrContext.cs` | Compilation context with symbol lookups |

---

## Dependencies

### Runtime (net10.0)
- No external dependencies (uses BCL only)

### Generator (netstandard2.0)
- Microsoft.CodeAnalysis.CSharp 5.0.0
- ANcpLua.Roslyn.Utilities 1.6.0

### Testing
- xunit.v3.mtp-v2 3.2.1
- FluentAssertions 8.3.0
- Verify.SourceGenerators 2.5.0

---

## Migration from v1.x

See [docs/migration-v2.md](docs/migration-v2.md) for detailed instructions.

Quick summary:
```diff
- <PackageReference Include="ErrorOr.Core" Version="1.x" />
- <PackageReference Include="ErrorOr.Endpoints" Version="1.x" />
+ <PackageReference Include="ErrorOr" Version="2.1.0" />
```

```diff
- using ErrorOr;
- using ErrorOr.Core.ErrorOr;
- using ErrorOr.Core.Errors;
- using ErrorOr.Core.Results;
+ using ErrorOr;
```

```diff
- builder.Services.AddErrorOrEndpointJson<AppJsonSerializerContext>().AddOpenApi();
+ builder.Services.AddOpenApi();
```
