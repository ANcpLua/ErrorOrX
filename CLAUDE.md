# CLAUDE.md - ErrorOrX

## What the Generator Does

The generator's job is **one thing**: convert `ErrorOr<T>` handlers into ASP.NET Core Minimal API endpoints.

```
User writes:                         Generator produces:
[Get("/todos/{id}")]                 app.MapGet("/todos/{id}", Invoke_Ep1)
ErrorOr<Todo> GetById(int id)   →       .WithName("TodoApi_GetById")
                                        .RequireAuthorization("Admin")  // if [Authorize("Admin")]
                                        ;

                                     // Wrapper returns Task (AOT-compatible)
                                     static async Task Invoke_Ep1(HttpContext ctx)
                                     {
                                         var __result = await Invoke_Ep1_Core(ctx);
                                         await __result.ExecuteAsync(ctx);
                                     }

                                     // Core logic uses minimal interface (IsError/Errors/Value)
                                     static Task<IResult> Invoke_Ep1_Core(...)
                                     {
                                         var result = TodoApi.GetById(id);
                                         if (result.IsError) return ToProblem(result.Errors);
                                         return TypedResults.Ok(result.Value);
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

### Minimal Interface Principle

Generated code uses only the **minimal `ErrorOr<T>` interface**: `IsError`, `Errors`, `Value`.

```csharp
// ✅ Correct - uses minimal interface
if (result.IsError) return ToProblem(result.Errors);
return TypedResults.Ok(result.Value);

// ❌ Avoid - creates dependency on convenience API
return result.Match(
    value => TypedResults.Ok(value),
    errors => ToProblem(errors));
```

**Why?**

- Reduces runtime coupling to ErrorOr library internals
- Generated code is more portable and self-contained
- Simpler to understand and debug
- Consistent pattern across all code paths (SSE, union types, fallback)

### AOT-Compatible Handler Pattern

The generator uses a wrapper pattern to ensure Native AOT compatibility:

```csharp
// Wrapper method - matches RequestDelegate signature (HttpContext → Task)
private static async Task Invoke_Ep1(HttpContext ctx)
{
    var __result = await Invoke_Ep1_Core(ctx);
    await __result.ExecuteAsync(ctx);  // Writes response to HttpContext
}

// Core method - returns typed Results<...> for OpenAPI documentation
private static Task<Results<Ok<Todo>, NotFound<ProblemDetails>>> Invoke_Ep1_Core(HttpContext ctx)
{
    // ... actual handler logic
}
```

**Why this pattern?**

1. **Avoids reflection** - Without `(Delegate)` cast, ASP.NET uses the AOT-friendly path
2. **No JSON metadata for `Task<Results<...>>`** - BCL generic types can't have `[JsonSerializable]`
3. **Explicit response writing** - `IResult.ExecuteAsync()` handles serialization via TypedResults

**The alternative (what fails in AOT):**

```csharp
// This forces reflection-based RequestDelegateFactory
app.MapGet("/path", (Delegate)Handler);  // ❌ Requires JsonTypeInfo for Task<Results<...>>

// This works in JIT but fails in AOT with:
// "JsonTypeInfo metadata for type 'Task<Results<...>>' was not provided"
```

### Why We Emit Middleware Calls

ASP.NET Core only sees attributes on the delegate you pass to `MapGet()`, `MapPost()`, etc.

```csharp
// Original method has [Authorize] - but we create a wrapper:
private static async Task Invoke_Ep1(HttpContext ctx) { ... }

// ASP.NET sees Invoke_Ep1, NOT the original method
app.MapGet("/path", Invoke_Ep1);
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

## MSBuild Properties

| Property                     | Default | Purpose                                                           |
|------------------------------|---------|-------------------------------------------------------------------|
| `ErrorOrGenerateJsonContext` | `false` | **Disabled by default.** See AOT JSON Context Requirements below. |

### AOT JSON Context Requirements

**CRITICAL:** Roslyn source generators cannot see output from other generators. This means:

1. If ErrorOrX generates `ErrorOrJsonContext`, the System.Text.Json source generator will **NOT** process it
2. Native AOT serialization will fail at runtime with cryptic "JsonTypeInfo metadata not found" errors

**You MUST create your own `JsonSerializerContext`:**

```csharp
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(YourRequestType))]
[JsonSerializable(typeof(YourResponseType))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
internal partial class AppJsonSerializerContext : JsonSerializerContext { }
```

And register it:

```csharp
builder.Services.AddErrorOrEndpoints(options => options
    .UseJsonContext<AppJsonSerializerContext>());
```

**EOE041 error** is emitted if you have `[FromBody]` parameters without a `JsonSerializerContext`.

## Consumer Setup

### Minimal Setup

```csharp
var builder = WebApplication.CreateSlimBuilder(args);
var app = builder.Build();

app.MapErrorOrEndpoints(); // Generated extension method

app.Run();
```

### With Fluent Configuration (Recommended)

Use the fluent builder for AOT-compatible JSON configuration:

```csharp
var builder = WebApplication.CreateSlimBuilder(args);

// Fluent configuration with all options
builder.Services.AddErrorOrEndpoints(options => options
    .UseJsonContext<AppJsonSerializerContext>()  // Register JSON context
    .WithCamelCase()                              // Use camelCase (default: true)
    .WithIgnoreNulls());                          // Ignore nulls (default: true)

var app = builder.Build();
app.MapErrorOrEndpoints();
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
| `TypeNameHelper.cs` | Type name manipulation (normalize, unwrap, compare, extract)   |
| `RouteValidator.cs` | Route validation AND route parameter lookup building           |

## Before Writing New Helper Code

**ALWAYS search for existing implementations before writing new utility methods.**

Type manipulation helpers are especially prone to duplication. Two distinct concepts exist:

| Concept                                 | API                                           | Location            |
|-----------------------------------------|-----------------------------------------------|---------------------|
| **Symbol-based** (Roslyn `ITypeSymbol`) | `ErrorOrContext.UnwrapNullable(ITypeSymbol)`  | `ErrorOrContext.cs` |
| **String-based** (FQN strings)          | `TypeNameHelper.UnwrapNullable(string, bool)` | `TypeNameHelper.cs` |

Before adding any type manipulation code, check `TypeNameHelper.cs` for:

- `Normalize()` - removes `global::` prefixes and trailing `?`
- `UnwrapNullable()` - unwraps `Nullable<T>` and `?` annotation
- `StripGlobalPrefix()` - removes `global::` prefix only
- `ExtractShortName()` - gets short type name from FQN
- `IsStringType()` / `IsPrimitiveJsonType()` - type classification
- `TypeNamesMatch()` - compares types handling aliases
- `GetKeywordAlias()` - maps BCL types to C# keywords

Before adding route/binding helpers, check `RouteValidator.cs` for:

- `BuildRouteParameterLookup()` - builds parameter dictionary by route name
- `ExtractRouteParameters()` - parses route template
- `ValidateConstraintTypes()` - validates constraint/type compatibility

**If you need similar functionality, extend the existing helper rather than duplicating.**

## Diagnostics

| ID     | Severity | Description                                              |
|--------|----------|----------------------------------------------------------|
| EOE001 | Error    | Invalid return type                                      |
| EOE002 | Error    | Handler must be static                                   |
| EOE003 | Error    | Route parameter not bound                                |
| EOE004 | Error    | Duplicate route                                          |
| EOE005 | Error    | Invalid route pattern                                    |
| EOE006 | Error    | Multiple body sources                                    |
| EOE007 | Error    | Type not AOT-serializable (not in JsonSerializerContext) |
| EOE009 | Warning  | Body on read-only HTTP method                            |
| EOE010 | Warning  | [AcceptedResponse] on read-only method                   |
| EOE011 | Error    | Invalid [FromRoute] type                                 |
| EOE012 | Error    | Invalid [FromQuery] type                                 |
| EOE013 | Error    | Invalid [AsParameters] type                              |
| EOE014 | Error    | [AsParameters] type has no constructor                   |
| EOE016 | Error    | Invalid [FromHeader] type                                |
| EOE023 | Warning  | Route constraint type mismatch                           |
| EOE025 | Error    | Ambiguous parameter binding (GET/DELETE + complex type)  |
| EOE030 | Info     | Too many result types for union                          |
| EOE032 | Warning  | Unknown error factory                                    |
| EOE033 | Error    | Undocumented interface call                              |
| EOE040 | Warning  | Missing CamelCase JSON policy                            |
| EOE041 | Error    | Missing JsonSerializerContext for AOT (no user context)  |

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

## Dependencies

| Package                          | Version  | Purpose                                |
|----------------------------------|----------|----------------------------------------|
| ANcpLua.Roslyn.Utilities         | 1.16.0   | Roslyn incremental generator utilities |
| ANcpLua.Roslyn.Utilities.Testing | 1.16.0   | Generator testing framework            |
| ANcpLua.Analyzers                | 1.9.0    | Code quality analyzers                 |
| Microsoft.CodeAnalysis.CSharp    | 5.0.0    | Roslyn APIs                            |
| xunit.v3                         | 3.2.2    | Testing framework with MTP             |
| AwesomeAssertions                | 9.3.0    | Fluent assertions                      |
| Microsoft.SourceLink.GitHub      | 10.0.102 | Source Link support                    |

## Maintenance Notes

- When extending shared SDK helpers (for example, `Throw.UnreachableException`), upstream the change in
  ANcpLua.NET.Sdk, bump the package version, and update `global.json` `msbuild-sdks` (plus `Directory.Packages.props`
  if a package reference is added).
