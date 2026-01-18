---
See [Root CLAUDE.md](../CLAUDE.md) for project context.
---

# ErrorOrX.Generators

This project contains the Roslyn source generator and analyzers for ErrorOrX.

## Package Details

- **PackageId**: `ErrorOrX.Generators`
- **Target**: `netstandard2.0` (required for Roslyn analyzers)
- **SDK**: `Microsoft.NET.Sdk` (not ANcpLua.NET.Sdk - PolySharp is needed for C# features)

## What It Does

Converts `ErrorOr<T>` methods with route attributes into ASP.NET Core Minimal API endpoints:

```csharp
[Get("/todos/{id}")]
public static ErrorOr<Todo> GetById(int id) => ...
```

Generates:

- `MapErrorOrEndpoints()` extension method
- Typed `Results<...>` union for OpenAPI
- Automatic parameter binding with smart inference
- Middleware attribute emission
- JSON serialization context (optional)

## Dependencies

- `Microsoft.CodeAnalysis.CSharp` - Roslyn APIs
- `ANcpLua.Roslyn.Utilities` - Bundled in package
- `PolySharp` - C# polyfills for netstandard2.0

## Package Structure

The `.nupkg` contains:

- `analyzers/dotnet/cs/ErrorOrX.Generators.dll` - The generator
- `analyzers/dotnet/cs/ANcpLua.Roslyn.Utilities.dll` - Bundled dependency
- `build/ErrorOrX.Generators.props` - MSBuild properties & CompilerVisibleProperty definitions
- Dependency on `ErrorOrX` (flows to consumers via `PrivateAssets="none"`)

## Generator Pipeline

```
Initialize.cs                    Emitter.cs
     │                               │
     ▼                               ▼
┌─────────────┐    ┌─────────────┐    ┌─────────────────────────┐
│ Syntax      │───▶│ Extract     │───▶│ Emit                    │
│ Provider    │    │ Endpoints   │    │ - MapErrorOrEndpoints() │
└─────────────┘    └─────────────┘    │ - JSON Context          │
                         │            │ - OpenAPI Transformer   │
                         ▼            └─────────────────────────┘
                   ┌─────────────┐
                   │ Bind        │
                   │ Parameters  │◀── httpMethod context
                   └─────────────┘
                         │
                         ▼
                   ┌─────────────┐
                   │ Validate    │
                   │ Routes      │
                   └─────────────┘
```

## Parameter Binding (ParameterBinding.cs)

### Entry Point

```csharp
BindParameters(
    IMethodSymbol method,
    ImmutableHashSet<string> routeParameters,
    ImmutableArray<DiagnosticInfo>.Builder diagnostics,
    ErrorOrContext context,
    string httpMethod)  // ← Required for smart inference
```

### Classification Priority

The `ClassifyParameter()` method processes parameters in this order:

1. **Explicit Attributes** (always win)
    - `[FromBody]` → Body
    - `[FromServices]` → Service
    - `[FromKeyedServices]` → KeyedService
    - `[FromRoute]` → Route
    - `[FromQuery]` → Query
    - `[FromHeader]` → Header
    - `[FromForm]` → Form
    - `[AsParameters]` → Expanded

2. **Special Types** (auto-detected)
    - `HttpContext` → HttpContext
    - `CancellationToken` → CancellationToken
    - `IFormFile` → FormFile
    - `IFormFileCollection` → FormFiles
    - `IFormCollection` → FormCollection
    - `Stream` → Stream
    - `PipeReader` → PipeReader

3. **Implicit Route** (name match)
    - Parameter name in route template → Route

4. **Implicit Query** (primitives)
    - Primitive types not in route → Query
    - Collections of primitives → Query

5. **Custom Binding**
    - Types with `TryParse` → Query or Route
    - Types with `BindAsync` → Query

6. **Smart Inference** (HTTP-method aware)
    - Interface types → Service
    - Abstract types → Service
    - Service naming patterns → Service
    - POST/PUT/PATCH + complex type → **Body**
    - GET/DELETE + complex type → **Error EOE025**
    - Fallback → Service

### Service Type Detection

`IsLikelyServiceType(ITypeSymbol)` detects:

```csharp
// Interface with Service suffix
ITodoService, IUserRepository → true

// Common DI suffixes
TodoRepository    → true (*Repository)
TodoHandler       → true (*Handler)
TodoManager       → true (*Manager)
ConfigProvider    → true (*Provider)
TodoFactory       → true (*Factory)
HttpClient        → true (*Client)
AppDbContext      → true (*Context with Db)
```

### Complex Type Detection

`IsComplexType(ITypeSymbol, ErrorOrContext)` returns `true` for types that are NOT:

- Primitives (`int`, `string`, `bool`, etc.)
- Special types (HttpContext, CancellationToken, etc.)
- Route-bindable types (types with `TryParse`)
- Collections of primitives
- `Nullable<T>` where T is not complex

### EOE025: Ambiguous Parameter Binding

GET/DELETE with complex type triggers this error:

```csharp
// ❌ EOE025: Parameter 'filter' of type 'SearchFilter' on GET endpoint requires explicit binding
[Get("/todos")]
public static ErrorOr<List<Todo>> Search(SearchFilter filter) => ...

// ✅ Fixed with explicit attribute
[Get("/todos")]
public static ErrorOr<List<Todo>> Search([FromQuery] SearchFilter filter) => ...

// ✅ Or use [AsParameters] for query object
[Get("/todos")]
public static ErrorOr<List<Todo>> Search([AsParameters] SearchFilter filter) => ...
```

## JSON Context Generation (Emitter.cs)

### Default Behavior

When `ErrorOrGenerateJsonContext` is `true` (default):

```csharp
// ErrorOrJsonContext.g.cs
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Todo))]
[JsonSerializable(typeof(CreateTodoRequest))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
internal partial class ErrorOrJsonContext : JsonSerializerContext { }
```

### With User Context

When user has existing `JsonSerializerContext`:

1. Generator detects via `JsonContextProvider`
2. Checks for missing types and CamelCase policy
3. Emits helper file instead:

```csharp
// ErrorOrJsonContext.MissingTypes.g.cs
// Add these attributes to your JsonSerializerContext:
// Target class: AppJsonSerializerContext
//
// [JsonSerializable(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails))]
// [JsonSerializable(typeof(Microsoft.AspNetCore.Http.HttpValidationProblemDetails))]
//
// Also add JsonSourceGenerationOptions for web API compatibility:
// [JsonSourceGenerationOptions(
//     PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
//     DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
```

### EOE040: Missing CamelCase Policy

Triggers when user's context lacks `PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase`:

```
warning EOE040: JsonSerializerContext 'AppJsonSerializerContext' should use 
PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase for web API compatibility.
```

## MSBuild Properties (ErrorOrX.Generators.props)

```xml
<Project>
  <PropertyGroup>
    <!-- Enable JSON context generation by default -->
    <ErrorOrGenerateJsonContext Condition="'$(ErrorOrGenerateJsonContext)' == ''">true</ErrorOrGenerateJsonContext>
  </PropertyGroup>

  <ItemGroup>
    <!-- Make MSBuild properties visible to the source generator -->
    <CompilerVisibleProperty Include="ErrorOrGenerateJsonContext" />
  </ItemGroup>

  <ItemGroup>
    <!-- Ensure ErrorOrX flows to consuming projects -->
    <PackageReference Update="ErrorOrX" PrivateAssets="none" />
  </ItemGroup>
</Project>
```

## Key Files

| File                  | Responsibility                                              |
|-----------------------|-------------------------------------------------------------|
| `Initialize.cs`       | Generator entry, pipeline orchestration                     |
| `ParameterBinding.cs` | Parameter classification and smart inference                |
| `Emitter.cs`          | Code generation (mappings, JSON context, AOT wrapper)       |
| `Extractor.cs`        | Method/attribute extraction                                 |
| `Analyzer.cs`         | JSON context detection, AOT validation                      |
| `ErrorOrContext.cs`   | Type resolution helpers                                     |
| `Descriptors.cs`      | Diagnostic definitions                                      |
| `ErrorMapping.cs`     | ErrorType → HTTP mapping                                    |
| `WellKnownTypes.cs`   | FQN string constants                                        |

## AOT-Compatible Handler Emission (Emitter.cs)

The emitter generates handlers using a wrapper pattern for Native AOT compatibility:

### Generated Code Structure

```csharp
// 1. Map registration uses typed MapGet/MapPost (no Delegate cast)
app.MapGet(@"/todos/{id:int}", Invoke_Ep1)
    .WithName("TodoApi_GetById")
    .WithMetadata(new AcceptsMetadata(new[] { "application/json" }, typeof(CreateTodoRequest)));

// 2. Wrapper method - returns Task (matches RequestDelegate)
private static async Task Invoke_Ep1(HttpContext ctx)
{
    var __result = await Invoke_Ep1_Core(ctx);
    await __result.ExecuteAsync(ctx);  // Writes response to HttpContext
}

// 3. Core method - returns typed Results<...> for OpenAPI
private static Task<Results<Ok<Todo>, NotFound<ProblemDetails>>> Invoke_Ep1_Core(HttpContext ctx)
{
    int id = (int)ctx.Request.RouteValues["id"]!;
    var result = TodoApi.GetById(id);
    return Task.FromResult(result.Match(
        value => (Results<Ok<Todo>, NotFound<ProblemDetails>>)TypedResults.Ok(value),
        errors => MapError(errors)));
}
```

### Why This Pattern?

| Problem                        | Solution                                             |
|--------------------------------|------------------------------------------------------|
| `(Delegate)` cast forces reflection | Use typed `MapGet`/`MapPost` without cast        |
| `Task<Results<...>>` needs JsonTypeInfo | Wrapper returns `Task`, not `Task<T>`        |
| BCL generics can't have [JsonSerializable] | Call `IResult.ExecuteAsync()` explicitly   |
| `.Accepts()` needs RouteHandlerBuilder | Use `.WithMetadata(new AcceptsMetadata(...))` |

### AcceptsMetadata Constructor

```csharp
// Correct signature: (string[] contentTypes, Type? requestType = null)
new AcceptsMetadata(new[] { "application/json" }, typeof(CreateTodoRequest))

// NOT: AcceptsMetadata(typeof(T), string[])  // Wrong order!
```

## Testing

```bash
# Unit tests
dotnet test tests/ErrorOrX.Generators.Tests/ErrorOrX.Generators.Tests.csproj

# Verify generator output
dotnet build samples/ErrorOrX.Sample --configuration Debug
cat samples/ErrorOrX.Sample/obj/Debug/net10.0/generated/ErrorOrX.Generators/*.cs
```

### Test Coverage Targets

| Area                     | Tests                                            |
|--------------------------|--------------------------------------------------|
| `IsLikelyServiceType()`  | Interface patterns, suffix patterns, edge cases  |
| `IsComplexType()`        | Primitives, collections, nullable, special types |
| `InferParameterSource()` | Each HTTP method × type combination              |
| EOE025                   | GET/DELETE with complex type                     |
| JSON context detection   | User context present/absent, CamelCase check     |

## AotJson Generator

The `AotJsonGenerator` automatically discovers types from ErrorOr endpoints and generates `[JsonSerializable]`
attributes for Native AOT compatibility.

### What It Does

```csharp
// User writes:
[AotJson]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class AppJsonSerializerContext : JsonSerializerContext;

public static class TodoApi
{
    [Get("/todos")]
    public static ErrorOr<List<Todo>> GetAll() => ...;
}

// Generator produces:
[JsonSerializable(typeof(global::Todo))]
[JsonSerializable(typeof(global::System.Collections.Generic.List<global::Todo>))]
[JsonSerializable(typeof(global::Todo[]))]
[JsonSerializable(typeof(global::Microsoft.AspNetCore.Mvc.ProblemDetails))]
[JsonSerializable(typeof(global::Microsoft.AspNetCore.Http.HttpValidationProblemDetails))]
partial class AppJsonSerializerContext;
```

### Pipeline

```
Initialize()
     │
     ├─► RegisterPostInitializationOutput()
     │       └─► EmitAotJsonAttribute()  // Emits [AotJson] + CollectionKind enum
     │
     ├─► ForAttributeWithMetadataName("ErrorOr.AotJsonAttribute")
     │       └─► ExtractAotJsonContext()  // Extracts settings from [AotJson]
     │
     ├─► CreateEndpointTypeProvider()
     │       └─► ForAttributeWithMetadataName() for each HTTP verb
     │           └─► ExtractTypesFromMethod()  // Discovers types from endpoints
     │
     └─► RegisterSourceOutput()
             └─► GenerateJsonSerializableAttributes()  // Emits [JsonSerializable] attributes
```

### Key Files

| File                  | Responsibility                                           |
|-----------------------|----------------------------------------------------------|
| `AotJsonGenerator.cs` | Main generator - type discovery and code emission        |
| `AotJsonModels.cs`    | Data models (`AotJsonContextInfo`, `DiscoveredTypeInfo`) |

### AotJson Attribute Options

| Property                | Default         | Purpose                                      |
|-------------------------|-----------------|----------------------------------------------|
| `ScanEndpoints`         | `true`          | Discover types from ErrorOr endpoint returns |
| `ScanNamespaces`        | `null`          | Additional namespaces to scan                |
| `IncludeTypes`          | `null`          | Explicit types to always include             |
| `ExcludeTypes`          | `null`          | Types to exclude from generation             |
| `GenerateCollections`   | `List \| Array` | Collection variants to generate              |
| `IncludeProblemDetails` | `true`          | Include ProblemDetails types                 |

### CollectionKind Enum

```csharp
[Flags]
public enum CollectionKind
{
    None = 0,
    List = 1 << 0,           // List<T>
    Array = 1 << 1,          // T[]
    IEnumerable = 1 << 2,    // IEnumerable<T>
    IReadOnlyList = 1 << 3,  // IReadOnlyList<T>
    All = List | Array | IEnumerable | IReadOnlyList
}
```

### Type Unwrapping

The generator recursively unwraps nested types:

```
Task<ErrorOr<List<Todo>>>
  └─► ErrorOr<List<Todo>>     (unwrap Task)
        └─► List<Todo>        (unwrap ErrorOr)
              └─► Todo        (unwrap List element)
```

### AOTJ Diagnostics

| ID      | Severity | Description                          |
|---------|----------|--------------------------------------|
| AOTJ001 | Warning  | JsonSerializerContext not registered |
| AOTJ002 | Info     | Missing [AotJson] attribute          |
| AOTJ003 | Warning  | Duplicate [AotJson] contexts         |
| AOTJ004 | Warning  | Type not serializable                |
| AOTJ005 | Error    | [AotJson] on non-partial class       |

### Important Implementation Notes

1. **Internal CollectionKind**: The generator needs `CollectionKind` at compile-time, so there's an internal copy in
   `AotJsonModels.cs`. The user-facing version is emitted via `PostInitializationOutput`.

2. **ForAttributeWithMetadataName Limitation**: Each generator can only see types from its own
   `PostInitializationOutput`. That's why:
    - Tests must define route attributes in test source code
    - The generator emits its own `[AotJson]` attribute

3. **Incremental Caching**: Uses `EquatableArray<T>` and primitive-only record structs - never cache `ISymbol` or
   `Compilation`.

### ANcpLua.Roslyn.Utilities Used

| Utility                                           | Purpose                                                    |
|---------------------------------------------------|------------------------------------------------------------|
| `ForAttributeWithMetadataNameOfClassesAndRecords` | Find `[AotJson]` decorated classes                         |
| `DiagnosticFlow<T>`                               | Railway-oriented error handling with diagnostic collection |
| `.SelectFlow()`                                   | Transform with `DiagnosticFlow`                            |
| `.ReportAndContinue()`                            | Report diagnostics and continue with values                |
| `EquatableArray<T>`                               | Value-equality collection for incremental caching          |
| `.CollectAsEquatableArray()`                      | Collect with value equality                                |
| `.Distinct()`                                     | Remove duplicates from discovered types                    |
| `AwaitableContext`                                | Unwrap `Task<T>`, `ValueTask<T>`                           |
| `CollectionContext`                               | Unwrap collections, get element types                      |
| `DiagnosticInfo.Create()`                         | Create diagnostic info for flow                            |
| `.WithTrackingName()`                             | Add step name for caching verification                     |

## Incremental Caching

### Tracking Names

The generators use `.WithTrackingName()` to identify custom pipeline steps for caching verification:

| Generator              | Step Name              | Purpose                                      |
|------------------------|------------------------|----------------------------------------------|
| `AotJsonGenerator`     | `AotJson_Contexts`     | [AotJson] decorated context classes          |
| `AotJsonGenerator`     | `AotJson_EndpointTypes`| Discovered types from ErrorOr endpoints      |

### Caching Test Pattern

```csharp
result.IsCached("AotJson_Contexts", "AotJson_EndpointTypes");
```

When step names are provided to `.IsCached()`:
1. Only checks forbidden types in those named steps (not internal Roslyn steps)
2. Only verifies caching status for those named steps

### Why This Matters

Roslyn's `ForAttributeWithMetadataName` creates internal tracked steps that inherently cache semantic types:
- `Compilation` - CSharpCompilation
- `result_ForAttributeWithMetadataName` - ISymbol, SemanticModel, SyntaxNode

These internal steps cannot be avoided when using semantic APIs. The solution:
1. Add `.WithTrackingName()` to custom transformation steps
2. Test only named steps with `.IsCached("stepName")`
3. Ensure custom data models are fully equatable (no Roslyn types)

### Utilities Fix Required (ANcpLua.Roslyn.Utilities 1.10.5+)

The `IsCached()` method must filter forbidden types by step names:

```csharp
// In GeneratorResult.cs
var violationsToCheck = stepNames.Length > 0
    ? report.ForbiddenTypeViolations.Where(v => stepNames.Contains(v.StepName)).ToList()
    : report.ForbiddenTypeViolations;
```

This allows testing generator caching without failing on internal Roslyn framework steps.