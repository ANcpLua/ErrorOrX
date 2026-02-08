# ErrorOrX

Source generator converting `ErrorOr<T>` handlers into ASP.NET Core Minimal API endpoints with full Native AOT support.

## Automatic Routing (for Claude)

**Always invoke** `/working-in-erroror` skill when starting work in this repo.

| Task              | Use                                             |
|-------------------|-------------------------------------------------|
| Implementation    | Task tool → `erroror-generator-specialist`      |
| Debugging         | Task tool → `deep-debugger`                     |
| Before completion | Run `dotnet build` + `dotnet test`, show output |
| Cross-repo work   | Invoke `/ancplua-ecosystem` first               |

## Quick Reference

```bash
dotnet build ErrorOrX.slnx
dotnet test --solution ErrorOrX.slnx # VERIFY
dotnet pack src/ErrorOrX/ErrorOrX.csproj -c Release
dotnet pack src/ErrorOrX.Generators/ErrorOrX.Generators.csproj -c Release
```

> **Note**: The `# VERIFY` comment bypasses the MTP smart-test-filtering hook that blocks full suite runs. Always
> include it for verification runs.

## What the Generator Does

Convert `ErrorOr<T>` handlers into fully-wired ASP.NET endpoints:

```
User writes:                         Generator produces:
[Get("/todos/{id:guid}")]             app.MapGet("/todos/{id:guid}", (Delegate)Invoke_Ep1)
ErrorOr<Todo> GetById(Guid id)  ->       .WithName("TodoApi_GetById")
                                         .WithMetadata(new ProducesResponseTypeMetadata(...))
                                         .RequireAuthorization("Admin")
                                         ;

                                     static async Task<Results<Ok<Todo>, ...>> Invoke_Ep1(HttpContext ctx)
                                     {
                                         return await Invoke_Ep1_Core(ctx);
                                     }

                                     static Task<Results<Ok<Todo>, ...>> Invoke_Ep1_Core(...)
                                     {
                                         var result = TodoApi.GetById(id);
                                         if (result.IsError) return ToProblem(result.Errors);
                                         return TypedResults.Ok(result.Value);
                                     }
```

## Core Generator Patterns

### Minimal Interface Principle

Generated code uses ONLY `IsError`, `Errors`, `Value` from `ErrorOr<T>`:

```csharp
// CORRECT - minimal interface
if (result.IsError) return ToProblem(result.Errors);
return TypedResults.Ok(result.Value);

// NEVER emit - creates coupling to convenience API
return result.Match(
    value => TypedResults.Ok(value),
    errors => ToProblem(errors));
```

**Why**: Reduces runtime coupling, portable code, consistent across all code paths.

### AOT Wrapper Pattern

Two-method pattern ensures Native AOT compatibility and OpenAPI visibility:

```csharp
// Wrapper - returns typed Results<...> for OpenAPI metadata
// MapGet uses (Delegate)Invoke_Ep1 to force the Delegate overload
private static async Task<Results<Ok<Todo>, NotFound<PD>>> Invoke_Ep1(HttpContext ctx)
{
    return await Invoke_Ep1_Core(ctx);
}

// Core - returns typed Results<...> with handler logic
private static Task<Results<Ok<Todo>, NotFound<ProblemDetails>>> Invoke_Ep1_Core(HttpContext ctx)
{
    // ... handler logic using minimal interface
}
```

**Why**: Without `(Delegate)` cast, `Func<HttpContext, Task<T>>` matches `RequestDelegate` — endpoints become invisible to OpenAPI. The cast forces `RequestDelegateFactory` to process the delegate, enabling typed return inspection.

### Middleware Emission

Wrapper delegates lose original method attributes. Generator MUST emit:

- `.RequireAuthorization()` for `[Authorize]`
- `.RequireRateLimiting()` for `[EnableRateLimiting]`
- `.CacheOutput()` for `[OutputCache]`
- `.RequireCors()` for `[EnableCors]`

## Smart Parameter Binding

| Priority | Condition                                                                                               | Binding          |
|----------|---------------------------------------------------------------------------------------------------------|------------------|
| 1        | Explicit attribute (`[FromBody]`, `[FromServices]`, etc.)                                               | As specified     |
| 2        | Special types (`HttpContext`, `CancellationToken`)                                                      | Auto-detected    |
| 3        | Parameter name matches route `{param}`                                                                  | Route            |
| 4        | Primitive type not in route                                                                             | Query            |
| 5        | Interface type                                                                                          | Service          |
| 6        | Abstract type                                                                                           | Service          |
| 7        | Service naming (`I*Service`, `*Repository`, `*Handler`, `*Manager`, `*Provider`, `*Factory`, `*Client`) | Service          |
| 8        | POST/PUT/PATCH + complex type                                                                           | **Body**         |
| 9        | GET/DELETE + complex type                                                                               | **Error EOE021** |
| 10       | Final fallback                                                                                          | Service          |

```csharp
// Smart binding infers:
// - req -> Body (POST + complex)
// - svc -> Service (interface)
// - id -> Route (matches {id})
[Post("/todos")]
public static ErrorOr<Todo> Create(CreateTodoRequest req, ITodoService svc) => ...

// EOE021 error - GET with complex type requires explicit binding
[Get("/todos")]
public static ErrorOr<List<Todo>> Search(SearchFilter filter) => ...  // Error
public static ErrorOr<List<Todo>> Search([FromQuery] SearchFilter filter) => ...  // OK
```

## ErrorType to HTTP Mapping (RFC 9110)

| ErrorType    | HTTP | TypedResult                           |
|--------------|------|---------------------------------------|
| Validation   | 400  | ValidationProblem                     |
| Unauthorized | 401  | UnauthorizedHttpResult                |
| Forbidden    | 403  | ForbidHttpResult                      |
| NotFound     | 404  | NotFound\<ProblemDetails\>            |
| Conflict     | 409  | Conflict\<ProblemDetails\>            |
| Failure      | 500  | InternalServerError\<ProblemDetails\> |
| Unexpected   | 500  | InternalServerError\<ProblemDetails\> |

## Diagnostics Summary

| Category           | IDs                            | Description                                                                 |
|--------------------|--------------------------------|-----------------------------------------------------------------------------|
| Handler validation | EOE001-002                     | Invalid return type, must be static                                         |
| Route validation   | EOE003-005                     | Unbound parameter, duplicate route, invalid pattern                         |
| Binding validation | EOE006, EOE008-021             | Multiple body, invalid binding types, ambiguous binding                     |
| Union types        | EOE022-024                     | Too many types, unknown factory, undocumented interface                     |
| JSON/AOT           | EOE007, EOE025-026, EOE039-041 | Not serializable, missing CamelCase, missing context, validation reflection |
| API versioning     | EOE027-031                     | Version-neutral conflicts, undeclared versions                              |
| Route/naming       | EOE032-033                     | Duplicate route params, non-PascalCase method names                         |
| AOT safety         | EOE034-038                     | Activator, Type.GetType, reflection, Expression.Compile, dynamic            |

## Consumer Setup

### Minimal

```csharp
var builder = WebApplication.CreateSlimBuilder(args);
var app = builder.Build();
app.MapErrorOrEndpoints();
app.Run();
```

### With AOT JSON Context (Required for Native AOT)

```csharp
// 1. Define your JSON context
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(YourRequestType))]
[JsonSerializable(typeof(YourResponseType))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
internal partial class AppJsonSerializerContext : JsonSerializerContext { }

// 2. Register with ErrorOrEndpoints
builder.Services.AddErrorOrEndpoints(options => options
    .UseJsonContext<AppJsonSerializerContext>()
    .WithCamelCase()
    .WithIgnoreNulls());

var app = builder.Build();
app.MapErrorOrEndpoints();
```

**Critical**: Roslyn generators cannot see other generators' output. You MUST create your own `JsonSerializerContext`.

## Source of Truth Files

| File                | Owns                                               |
|---------------------|----------------------------------------------------|
| `Descriptors.cs`    | All diagnostics (EOE001-EOE041)                    |
| `ErrorMapping.cs`   | ErrorType names, HTTP codes, TypedResult factories |
| `EndpointModels.cs` | All data structures                                |
| `WellKnownTypes.cs` | All FQN string constants                           |
| `RouteValidator.cs` | Route validation, parameter lookup building        |

## Project Structure

```
src/
  ErrorOrX/                    # Runtime library (net10.0)
  ErrorOrX.Generators/         # Source generator (netstandard2.0)
tests/
  ErrorOrX.Tests/              # Runtime unit tests
  ErrorOrX.Generators.Tests/   # Generator snapshot tests
  ErrorOrX.Integration.Tests/  # HTTP parity tests
```

## Dependencies

| Package                          | Version | Purpose                         |
|----------------------------------|---------|---------------------------------|
| ANcpLua.Roslyn.Utilities         | 1.31.0  | Incremental generator utilities |
| ANcpLua.Roslyn.Utilities.Testing | 1.31.0  | Generator testing framework     |
| ANcpLua.Analyzers                | 1.13.0  | Code quality analyzers          |
| Microsoft.CodeAnalysis.CSharp    | 5.0.0   | Roslyn APIs                     |
| xunit.v3                         | 3.2.2   | Testing framework               |
| AwesomeAssertions                | 9.3.0   | Fluent assertions               |

## Before Writing New Code

**Search for existing implementations first.** Common duplication areas:

| Concept          | Symbol-based API                             | String-based API                             |
|------------------|----------------------------------------------|----------------------------------------------|
| Unwrap nullable  | `ErrorOrContext.UnwrapNullable(ITypeSymbol)` | `TypeNameHelper.UnwrapNullable(string)`      |
| Type comparison  | Roslyn `ITypeSymbol.Equals`                  | `TypeNameHelper.TypeNamesMatch()`            |
| Route parameters | -                                            | `RouteValidator.BuildRouteParameterLookup()` |
