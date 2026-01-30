# ErrorOrX.Generators

Roslyn source generator and analyzers for ErrorOrX. Target: `netstandard2.0`.

## What It Generates

Converts `ErrorOr<T>` methods with route attributes into ASP.NET Core Minimal API endpoints:

```csharp
[Get("/todos/{id}")]
public static ErrorOr<Todo> GetById(int id) => ...
```

Outputs:
- `MapErrorOrEndpoints()` extension method
- Typed `Results<...>` union for OpenAPI
- Automatic parameter binding with smart inference
- Middleware attribute emission
- JSON serialization context (optional)

## Core Generator Patterns

### Minimal Interface Principle

Generated code uses ONLY `IsError`, `Errors`, `Value`:

```csharp
// CORRECT
if (result.IsError) return ToProblem(result.Errors);
return TypedResults.Ok(result.Value);

// NEVER emit
return result.Match(value => TypedResults.Ok(value), errors => ToProblem(errors));
```

### AOT Wrapper Pattern

```csharp
// Wrapper - returns Task (no Delegate cast needed)
private static async Task Invoke_Ep1(HttpContext ctx)
{
    var __result = await Invoke_Ep1_Core(ctx);
    await __result.ExecuteAsync(ctx);
}

// Core - returns typed Results<...> for OpenAPI
private static Task<IResult> Invoke_Ep1_Core(HttpContext ctx)
{
    int id = (int)ctx.Request.RouteValues["id"]!;
    var result = TodoApi.GetById(id);
    if (result.IsError) return Task.FromResult(ToProblem(result.Errors));
    return Task.FromResult(TypedResults.Ok(result.Value));
}
```

**Why**: `(Delegate)` cast forces reflection; `Task<Results<...>>` cannot have `[JsonSerializable]`.

## Generator Pipeline

```
Initialize.cs -> Extractor.cs -> ParameterBinding.cs -> RouteValidator.cs -> Emitter.cs
     |               |                  |                     |                  |
  Syntax         Extract           Classify             Validate            Generate
  Provider       Methods           Params               Routes              Code
```

## Parameter Binding Classification

Priority order in `ClassifyParameter()`:

1. **Explicit attributes** - `[FromBody]`, `[FromServices]`, etc.
2. **Special types** - `HttpContext`, `CancellationToken`, `IFormFile`
3. **Route match** - Parameter name in route template
4. **Primitives** - Query binding for non-route primitives
5. **Custom binding** - Types with `TryParse` or `BindAsync`
6. **Smart inference** (HTTP-aware):
   - Interface/abstract types -> Service
   - Service naming patterns -> Service
   - POST/PUT/PATCH + complex -> **Body**
   - GET/DELETE + complex -> **Error EOE021**
   - Fallback -> Service

### Service Type Detection

`IsLikelyServiceType()` detects:
- Interface with Service suffix (`ITodoService`)
- Common DI suffixes: `*Repository`, `*Handler`, `*Manager`, `*Provider`, `*Factory`, `*Client`, `*Context` (with Db)

### EOE021: Ambiguous Parameter

```csharp
// Error - GET with complex type
[Get("/todos")]
public static ErrorOr<List<Todo>> Search(SearchFilter filter) => ...

// Fix - explicit binding
public static ErrorOr<List<Todo>> Search([FromQuery] SearchFilter filter) => ...
public static ErrorOr<List<Todo>> Search([AsParameters] SearchFilter filter) => ...
```

## Key Source Files

| File | Responsibility |
|------|----------------|
| `Initialize.cs` | Generator entry, pipeline orchestration |
| `ParameterBinding.cs` | Parameter classification and smart inference |
| `Emitter.cs` | Code generation (mappings, JSON context, AOT wrapper) |
| `Extractor.cs` | Method/attribute extraction |
| `Analyzer.cs` | JSON context detection, AOT validation |
| `Descriptors.cs` | All diagnostic definitions (EOE001-EOE038) |
| `ErrorMapping.cs` | ErrorType -> HTTP status mapping |
| `WellKnownTypes.cs` | FQN string constants |
| `RouteValidator.cs` | Route validation and parameter lookup |
| `ResultsUnionTypeBuilder.cs` | Build Results<...> union types |

## ANcpLua.Roslyn.Utilities Used

| Utility | Purpose |
|---------|---------|
| `DiagnosticFlow<T>` | Railway-oriented error handling |
| `.SelectFlow()` | Transform with diagnostic collection |
| `EquatableArray<T>` | Value-equality for incremental caching |
| `AwaitableContext` | Unwrap `Task<T>`, `ValueTask<T>` |
| `CollectionContext` | Unwrap collections |
| `.WithTrackingName()` | Add step name for cache verification |

## MSBuild Properties

```xml
<PropertyGroup>
  <ErrorOrGenerateJsonContext>false</ErrorOrGenerateJsonContext>  <!-- Default: false -->
</PropertyGroup>
```

When user has `JsonSerializerContext`, generator emits `ErrorOrJsonContext.MissingTypes.g.cs` with copy-paste attributes.

## Testing

```bash
dotnet test --project tests/ErrorOrX.Generators.Tests
dotnet test --project tests/ErrorOrX.Generators.Tests --filter-class "*ResultsUnionTypeBuilder*"
```

## Package Structure

```
analyzers/dotnet/cs/
  ErrorOrX.Generators.dll
  ANcpLua.Roslyn.Utilities.dll  (bundled)
build/
  ErrorOrX.Generators.props     (MSBuild properties)
```
