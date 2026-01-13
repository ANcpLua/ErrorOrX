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
    <!-- Defaults -->
    <ErrorOrGenerateJsonContext Condition="'$(ErrorOrGenerateJsonContext)' == ''">true</ErrorOrGenerateJsonContext>
    <ErrorOrLegacyParameterBinding Condition="'$(ErrorOrLegacyParameterBinding)' == ''">false</ErrorOrLegacyParameterBinding>
  </PropertyGroup>

  <ItemGroup>
    <!-- Make properties visible to generator -->
    <CompilerVisibleProperty Include="ErrorOrGenerateJsonContext" />
    <CompilerVisibleProperty Include="ErrorOrLegacyParameterBinding" />
  </ItemGroup>

  <ItemGroup>
    <!-- Ensure ErrorOrX flows to consuming projects -->
    <PackageReference Update="ErrorOrX" PrivateAssets="none" />
  </ItemGroup>
</Project>
```

## Key Files

| File                  | Responsibility                               |
|-----------------------|----------------------------------------------|
| `Initialize.cs`       | Generator entry, pipeline orchestration      |
| `ParameterBinding.cs` | Parameter classification and smart inference |
| `Emitter.cs`          | Code generation (mappings, JSON context)     |
| `Extractor.cs`        | Method/attribute extraction                  |
| `Analyzer.cs`         | JSON context detection, AOT validation       |
| `ErrorOrContext.cs`   | Type resolution helpers                      |
| `Descriptors.cs`      | Diagnostic definitions                       |
| `ErrorMapping.cs`     | ErrorType → HTTP mapping                     |
| `WellKnownTypes.cs`   | FQN string constants                         |

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