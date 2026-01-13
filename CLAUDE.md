# CLAUDE.md - ErrorOrX

## What the Generator Does

The generator's job is **one thing**: convert `ErrorOr<T>` handlers into ASP.NET Core Minimal API endpoints.

```
User writes:                         Generator produces:
[Get("/todos/{id}")]                 app.MapMethods("/todos/{id}", ["GET"], Invoke_Ep1)
ErrorOr<Todo> GetById(int id)   →       .WithName("TodoApi_GetById")
                                        .RequireAuthorization("Admin")  // if [Authorize("Admin")]
                                        ;

                                     static Task<Results<Ok<Todo>, NotFound<ProblemDetails>, ...>> Invoke_Ep1(...)
                                     {
                                         var result = TodoApi.GetById(id);
                                         return result.Match(
                                             value => TypedResults.Ok(value),
                                             errors => /* map to problem details */);
                                     }
```

### Core Responsibilities

| Responsibility                  | Why                                                                                   |
|---------------------------------|---------------------------------------------------------------------------------------|
| Map `ErrorOr<T>` → TypedResults | ASP.NET doesn't know ErrorOr; we convert to `Ok<T>`, `NotFound<ProblemDetails>`, etc. |
| Generate `Results<...>` union   | OpenAPI reads the union to document all possible response codes                       |
| Emit middleware fluent calls    | **Wrapper delegate loses original method's attributes** (see below)                   |
| Generate JSON context           | AOT requires `[JsonSerializable]` for all types                                       |
| Wire route parameters           | Extract from route template, bind from HttpContext                                    |
| **Smart parameter binding**     | Infer `[FromBody]`/`[FromServices]` based on HTTP method and type                     |

### Why We Emit Middleware Calls

ASP.NET Core only sees attributes on the delegate you pass to `MapMethods()`.

```csharp
// Original method has [Authorize] - but we create a wrapper:
private static Task<Results<...>> Invoke_Ep1(HttpContext ctx) { ... }

// ASP.NET sees Invoke_Ep1, NOT the original method
app.MapMethods("/path", ["GET"], (Delegate)Invoke_Ep1);
```

**The wrapper has no attributes.** Therefore the generator MUST emit:

- `.RequireAuthorization()` for `[Authorize]`
- `.RequireRateLimiting()` for `[EnableRateLimiting]`
- `.CacheOutput()` for `[OutputCache]`

This is not redundant - it's required.

## What ASP.NET Already Provides

These work natively when you pass the handler directly (but NOT through our wrapper):

| Attribute              | Native Fluent             | We Must Emit |
|------------------------|---------------------------|--------------|
| `[Authorize]`          | `.RequireAuthorization()` | Yes          |
| `[EnableRateLimiting]` | `.RequireRateLimiting()`  | Yes          |
| `[OutputCache]`        | `.CacheOutput()`          | Yes          |
| `[EnableCors]`         | `.RequireCors()`          | Yes          |

## Smart Parameter Binding

The generator infers parameter sources based on HTTP method and type characteristics.

### Inference Rules (Priority Order)

| Rule | Condition                                                                                                       | Binding          |
|------|-----------------------------------------------------------------------------------------------------------------|------------------|
| 1    | Explicit attribute (`[FromBody]`, `[FromServices]`, etc.)                                                       | As specified     |
| 2    | Special types (HttpContext, CancellationToken)                                                                  | Auto-detected    |
| 3    | Parameter name matches route `{param}`                                                                          | Route            |
| 4    | Primitive type not in route                                                                                     | Query            |
| 5    | Interface type                                                                                                  | Service          |
| 6    | Abstract type                                                                                                   | Service          |
| 7    | Service naming pattern (`I*Service`, `*Repository`, `*Handler`, `*Manager`, `*Provider`, `*Factory`, `*Client`) | Service          |
| 8    | POST/PUT/PATCH + complex type                                                                                   | **Body**         |
| 9    | GET/DELETE + complex type                                                                                       | **Error EOE025** |
| 10   | Final fallback                                                                                                  | Service          |

### Examples

```csharp
// Smart binding infers:
// - req → Body (POST + complex type)
// - svc → Service (interface)
// - id → Route (matches {id})
[Post("/todos")]
public static ErrorOr<Todo> Create(CreateTodoRequest req, ITodoService svc) => ...

[Get("/todos/{id}")]
public static ErrorOr<Todo> GetById(int id, ITodoService svc) => ...

// EOE025 error - GET with complex type requires explicit binding
[Get("/todos")]
public static ErrorOr<List<Todo>> Search(SearchFilter filter) => ... // ❌ Ambiguous
public static ErrorOr<List<Todo>> Search([FromQuery] SearchFilter filter) => ... // ✅ Explicit
```

### Disabling Smart Binding

For legacy behavior (all unclassified → DI service):

```xml
<PropertyGroup>
  <ErrorOrLegacyParameterBinding>true</ErrorOrLegacyParameterBinding>
</PropertyGroup>
```

## MSBuild Properties

| Property                        | Default | Purpose                                           |
|---------------------------------|---------|---------------------------------------------------|
| `ErrorOrGenerateJsonContext`    | `true`  | Emit `ErrorOrJsonContext` with all endpoint types |
| `ErrorOrLegacyParameterBinding` | `false` | Use old DI-fallback for unclassified parameters   |

## Consumer Setup

### Minimal Setup

```csharp
var builder = WebApplication.CreateSlimBuilder(args);
var app = builder.Build();

app.MapErrorOrEndpoints(); // Generated extension method

app.Run();
```

### With Custom JSON Context

When you have your own `JsonSerializerContext`:

```xml
<PropertyGroup>
  <ErrorOrGenerateJsonContext>false</ErrorOrGenerateJsonContext>
</PropertyGroup>
```

The generator emits `ErrorOrJsonContext.MissingTypes.g.cs` with copy-paste ready attributes:

```csharp
// Add to your context:
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails))]
[JsonSerializable(typeof(Microsoft.AspNetCore.Http.HttpValidationProblemDetails))]
// ... your endpoint types
internal partial class AppJsonSerializerContext : JsonSerializerContext;
```

### Common Gotchas

| Issue                | Symptom               | Fix                                                                                         |
|----------------------|-----------------------|---------------------------------------------------------------------------------------------|
| Missing `CamelCase`  | JSON body not binding | Add `[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]` |
| `ErrorOrX` not found | Build error           | Remove `PrivateAssets="all"` from package reference                                         |
| GET with DTO         | EOE025 error          | Add `[FromQuery]` or `[AsParameters]` explicitly                                            |

## Current Gaps

| Gap                                          | Status          |
|----------------------------------------------|-----------------|
| `[ApiVersion]` extraction                    | Not implemented |
| Middleware emission tests                    | None exist      |
| Integration tests (middleware actually runs) | None exist      |

## Source Files

```
src/
├── ErrorOrX/                    # Runtime library (net10.0)
│   ├── EmptyErrors.cs
│   ├── Error.cs
│   ├── ErrorOr.cs
│   ├── ErrorOr.Else.cs
│   ├── ErrorOr.ElseExtensions.cs
│   ├── ErrorOr.Equality.cs
│   ├── ErrorOr.FailIf.cs
│   ├── ErrorOr.FailIfExtensions.cs
│   ├── ErrorOr.ImplicitConverters.cs
│   ├── ErrorOr.Match.cs
│   ├── ErrorOr.MatchExtensions.cs
│   ├── ErrorOr.OrExtensions.cs      # NEW: OrNotFound, OrValidation, etc.
│   ├── ErrorOr.Switch.cs
│   ├── ErrorOr.SwitchExtensions.cs
│   ├── ErrorOr.Then.cs
│   ├── ErrorOr.ThenExtensions.cs
│   ├── ErrorOr.ToErrorOrExtensions.cs
│   ├── ErrorOrFactory.cs
│   ├── ErrorType.cs
│   ├── IErrorOr.cs
│   ├── Results.cs
│   └── TypedResults.cs
│
└── ErrorOrX.Generators/         # Source generator (netstandard2.0)
    ├── Analyzers/
    │   ├── Descriptors.cs
    │   └── ErrorOrEndpointAnalyzer.cs
    ├── Core/
    │   ├── ErrorOrEndpointGenerator.Analyzer.cs
    │   ├── ErrorOrEndpointGenerator.Emitter.cs
    │   ├── ErrorOrEndpointGenerator.Extractor.cs
    │   ├── ErrorOrEndpointGenerator.Initialize.cs
    │   └── ErrorOrEndpointGenerator.ParameterBinding.cs
    ├── Models/
    │   ├── EndpointModels.cs
    │   ├── Enums.cs
    │   └── ErrorMapping.cs
    ├── TypeResolution/
    │   ├── ErrorOrContext.cs
    │   ├── ResultsUnionTypeBuilder.cs
    │   └── WellKnownTypes.cs
    ├── Validation/
    │   ├── DuplicateRouteDetector.cs
    │   └── RouteValidator.cs
    ├── Helpers/
    │   ├── EndpointIdentityHelper.cs
    │   ├── IncrementalProviderExtensions.cs
    │   └── TypeNameHelper.cs
    ├── OpenApiTransformerGenerator.cs
    └── build/
        └── ErrorOrX.Generators.props
```

## Single Source of Truth

| File                | Owns                                                           |
|---------------------|----------------------------------------------------------------|
| `ErrorMapping.cs`   | Error type names, HTTP status codes, TypedResult factories     |
| `EndpointModels.cs` | All data structures (EndpointDescriptor, MiddlewareInfo, etc.) |
| `WellKnownTypes.cs` | All FQN string constants                                       |
| `Descriptors.cs`    | All diagnostics (EOE001-EOE040)                                |

## Diagnostics

| ID     | Severity | Description                                             |
|--------|----------|---------------------------------------------------------|
| EOE001 | Error    | Invalid return type                                     |
| EOE002 | Error    | Handler must be static                                  |
| EOE003 | Error    | Route parameter not bound                               |
| EOE004 | Error    | Duplicate route                                         |
| EOE005 | Error    | Invalid route pattern                                   |
| EOE006 | Error    | Multiple body sources                                   |
| EOE007 | Warning  | Type not AOT-serializable                               |
| EOE009 | Warning  | Body on read-only HTTP method                           |
| EOE010 | Warning  | [AcceptedResponse] on read-only method                  |
| EOE011 | Error    | Invalid [FromRoute] type                                |
| EOE012 | Error    | Invalid [FromQuery] type                                |
| EOE013 | Error    | Invalid [AsParameters] type                             |
| EOE014 | Error    | [AsParameters] type has no constructor                  |
| EOE016 | Error    | Invalid [FromHeader] type                               |
| EOE023 | Warning  | Route constraint type mismatch                          |
| EOE025 | Error    | Ambiguous parameter binding (GET/DELETE + complex type) |
| EOE030 | Info     | Too many result types for union                         |
| EOE032 | Warning  | Unknown error factory                                   |
| EOE033 | Error    | Undocumented interface call                             |
| EOE040 | Warning  | Missing CamelCase JSON policy                           |

## ErrorType → HTTP (RFC 9110)

| ErrorType    | HTTP | TypedResult                           |
|--------------|------|---------------------------------------|
| Validation   | 400  | ValidationProblem                     |
| Unauthorized | 401  | UnauthorizedHttpResult                |
| Forbidden    | 403  | ForbidHttpResult                      |
| NotFound     | 404  | NotFound\<ProblemDetails\>            |
| Conflict     | 409  | Conflict\<ProblemDetails\>            |
| Failure      | 500  | InternalServerError\<ProblemDetails\> |
| Unexpected   | 500  | InternalServerError\<ProblemDetails\> |

## Commands

```bash
dotnet build ErrorOrX.slnx
dotnet test --solution ErrorOrX.slnx
dotnet pack src/ErrorOrX/ErrorOrX.csproj -c Release
dotnet pack src/ErrorOrX.Generators/ErrorOrX.Generators.csproj -c Release
```

## Package Structure

| Package               | Target           | NuGet Location                                |
|-----------------------|------------------|-----------------------------------------------|
| `ErrorOrX`            | `net10.0`        | `lib/net10.0/ErrorOrX.dll`                    |
| `ErrorOrX.Generators` | `netstandard2.0` | `analyzers/dotnet/cs/ErrorOrX.Generators.dll` |

Consumers reference `ErrorOrX.Generators` which declares a dependency on `ErrorOrX`.