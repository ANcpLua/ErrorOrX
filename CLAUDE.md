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

| Responsibility | Why |
|----------------|-----|
| Map `ErrorOr<T>` → TypedResults | ASP.NET doesn't know ErrorOr; we convert to `Ok<T>`, `NotFound<ProblemDetails>`, etc. |
| Generate `Results<...>` union | OpenAPI reads the union to document all possible response codes |
| Emit middleware fluent calls | **Wrapper delegate loses original method's attributes** (see below) |
| Generate JSON context | AOT requires `[JsonSerializable]` for all types |
| Wire route parameters | Extract from route template, bind from HttpContext |

### Why We Emit Middleware Calls

ASP.NET Core only sees attributes on the delegate you pass to `MapMethods()`.

```csharp
// Original method has [Authorize] - but we create a wrapper:
private static Task<Results<...>> Invoke_Ep1(HttpContext ctx) { ... }

// ASP.NET sees Invoke_Ep1, NOT the original method
app.MapMethods("/path", ["GET"], (Delegate)Invoke_Ep1);
m```

**The wrapper has no attributes.** Therefore the generator MUST emit:
- `.RequireAuthorization()` for `[Authorize]`
- `.RequireRateLimiting()` for `[EnableRateLimiting]`
- `.CacheOutput()` for `[OutputCache]`

This is not redundant - it's required.

## What ASP.NET Already Provides

These work natively when you pass the handler directly (but NOT through our wrapper):

| Attribute | Native Fluent | We Must Emit |
|-----------|---------------|--------------|
| `[Authorize]` | `.RequireAuthorization()` | Yes |
| `[EnableRateLimiting]` | `.RequireRateLimiting()` | Yes |
| `[OutputCache]` | `.CacheOutput()` | Yes |
| `[EnableCors]` | `.RequireCors()` | Yes |

## Current Gaps

| Gap | Status |
|-----|--------|
| `[ApiVersion]` extraction | Not implemented |
| Middleware emission tests | None exist |
| Integration tests (middleware actually runs) | None exist |

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
    ├── Generators/
    │   ├── Core/
    │   │   ├── ErrorOrEndpointGenerator.Analyzer.cs
    │   │   ├── ErrorOrEndpointGenerator.Emitter.cs
    │   │   ├── ErrorOrEndpointGenerator.Extractor.cs
    │   │   ├── ErrorOrEndpointGenerator.Initialize.cs
    │   │   └── ErrorOrEndpointGenerator.ParameterBinding.cs
    │   ├── Models/
    │   │   ├── EndpointModels.cs
    │   │   ├── Enums.cs
    │   │   └── ErrorMapping.cs
    │   ├── TypeResolution/
    │   │   ├── ErrorOrContext.cs
    │   │   ├── ResultsUnionTypeBuilder.cs
    │   │   └── WellKnownTypes.cs
    │   ├── Validation/
    │   │   ├── DuplicateRouteDetector.cs
    │   │   └── RouteValidator.cs
    │   ├── Helpers/
    │   │   ├── EndpointIdentityHelper.cs
    │   │   ├── IncrementalProviderExtensions.cs
    │   │   └── TypeNameHelper.cs
    │   └── OpenApiTransformerGenerator.cs
    └── build/
        └── ErrorOrX.Generators.props
```

## Single Source of Truth

| File | Owns |
|------|------|
| `ErrorMapping.cs` | Error type names, HTTP status codes, TypedResult factories |
| `EndpointModels.cs` | All data structures (EndpointDescriptor, MiddlewareInfo, etc.) |
| `WellKnownTypes.cs` | All FQN string constants |
| `Descriptors.cs` | All diagnostics (EOE001-EOE033) |

## ErrorType → HTTP (RFC 9110)

| ErrorType | HTTP | TypedResult |
|-----------|------|-------------|
| Validation | 400 | ValidationProblem |
| Unauthorized | 401 | UnauthorizedHttpResult |
| Forbidden | 403 | ForbidHttpResult |
| NotFound | 404 | NotFound\<ProblemDetails\> |
| Conflict | 409 | Conflict\<ProblemDetails\> |
| Failure | 500 | InternalServerError\<ProblemDetails\> |
| Unexpected | 500 | InternalServerError\<ProblemDetails\> |

## Commands

```bash
dotnet build ErrorOrX.slnx
dotnet test --solution ErrorOrX.slnx
dotnet pack src/ErrorOrX/ErrorOrX.csproj -c Release
dotnet pack src/ErrorOrX.Generators/ErrorOrX.Generators.csproj -c Release
```

## Package Structure

| Package | Target | NuGet Location |
|---------|--------|----------------|
| `ErrorOrX` | `net10.0` | `lib/net10.0/ErrorOrX.dll` |
| `ErrorOrX.Generators` | `netstandard2.0` | `analyzers/dotnet/cs/ErrorOrX.Generators.dll` |

Consumers reference `ErrorOrX.Generators` which declares a dependency on `ErrorOrX`.