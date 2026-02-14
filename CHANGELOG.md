# Changelog

All notable changes to this project are documented in this file.

## [Unreleased]

### Fixed

- **Analyzer early-return scoping**: Custom HTTP methods (e.g., `[ErrorOrEndpoint("CONNECT", "/path/{id}")]`) with route
  parameters silently skipped EOE006, EOE008, EOE009, and EOE039 diagnostics. The `return` when `ParseMethodString`
  returned null exited the entire `AnalyzeEndpoint` method instead of just the route binding block.

- **HttpVerb doc comment**: Corrected misleading "No default/discard arms" comment — the enum's switch expressions do
  have defensive discard arms (`_ => throw`), which is intentional for the `byte`-backed enum.

### Changed

- **Renamed `EmitJsonConfigExtension`** to `EmitAddErrorOrEndpointsExtension` — the method emits the
  `AddErrorOrEndpoints()` service registration extension, not JSON configuration. Historical name no longer matched.

- **Typed `IsBodyless` in analyzer**: EOE008 and EOE009 now use `HttpVerb.IsBodyless()` with string fallback for custom
  verbs, replacing the previous string-only `WellKnownTypes.HttpMethod.IsBodyless()` calls.

- **Updated dependencies**: ANcpLua.Analyzers 1.13.0 → 1.14.0, ANcpLua.Roslyn.Utilities 1.31.0 → 1.33.0,
  ANcpLua.NET.Sdk 2.0.5 → 2.0.6, .NET SDK 10.0.102 → 10.0.103.

## [3.6.0] - 2026-02-13

### Added

- **HttpVerb enum**: Replaced string-based HTTP method comparisons throughout the core generator pipeline with a
  strongly-typed `HttpVerb` enum, providing compile-time exhaustiveness checks and eliminating string comparison bugs.

- **EmitContext record struct**: Flattened deeply nested Roslyn pipeline tuples into a named `EmitContext` record
  struct,
  improving readability of `RegisterSourceOutput` callbacks.

- **MapCallEmitter**: Extracted shared map call emission helpers (`EmitMapCallStart`/`EmitMapCallEnd`) used by both
  grouped and ungrouped endpoint emission, eliminating code duplication.

- **ValidationResolverEmitter**: New emitter for validation resolver support with DataAnnotations integration.

- **Validation showcase sample**: Added `ErrorOrX.Validation.Showcase` sample project demonstrating validation patterns.

### Changed

- **Generic CombineAll**: Replaced hand-written `CombineSix`/`CombineNine` provider combiners with a generic
  `CombineAll<T>(params IncrementalValuesProvider<T>[])` using pairwise loop (163 lines → 47 lines).

- **Per-concern middleware emission**: Split monolithic `EmitMiddlewareCalls` into 4 focused per-concern methods
  (`EmitAuthorizationMiddleware`, `EmitRateLimitingMiddleware`, `EmitOutputCacheMiddleware`, `EmitCorsMiddleware`).

- **Pure `CollectSerializableTypes`**: Extracted serializable type collection as a pure method from the analyzer.

- **Exhaustive switch expressions**: Added `ArgumentOutOfRangeException` discard arms to all `ParameterSource` and
  `HttpVerb` switch expressions for compile-time safety.

- **`in` parameter optimization**: Added `in` modifier to 5 `MiddlewareInfo` and `VersioningInfo` readonly record struct
  parameters to avoid unnecessary copies.

- **Updated dependencies**: ANcpLua.Roslyn.Utilities 1.31.0 → 1.33.0.

## [3.5.0] - 2026-02-08

### Changed

- **Emitter cohesion refactoring**: Improved expressiveness and removed incoherent patterns in
  `ErrorOrEndpointGenerator.Emitter.cs`:
    - Removed passthrough wrappers (`EmitParameterBinding`, `BuildArgumentExpression`) that added indirection without
      logic — callers now use `BindingCodeEmitter` directly
    - Unified `WrapReturn` — eliminated duplicate local function in `EmitUnionTypeErrorHandling` by threading
      `InvokerContext` through `EmitValidationHandling` and `EmitErrorTypeSwitch`, also removing `Func<string, string>`
      delegate allocations
    - Collapsed `GetSuccessFactoryWithLocation` from 4 sequential guard clauses into a single positive condition
    - Simplified `HasValidatableTypes` from manual nested loop to `Any()`/`Any()` LINQ expression
    - Simplified `SortEndpoints` from manual array copy + `Array.Sort` to idiomatic `OrderBy`/`ThenBy` chain

- **Nullable suppression fix**: Replaced `constant.Value.ToString()!` in `ErrorOrContext.TypedConstantToLiteral` with
  `Convert.ToString(constant.Value, CultureInfo.InvariantCulture) ?? "null"` — removes hidden nullable assumption and
  uses `InvariantCulture` consistently with adjacent numeric arms.

## [3.4.0] - 2026-02-07

### Changed

- **Test suite cleanup**: Removed redundant assertion-based tests (-7,823 lines), consolidated shared boilerplate into
  dedicated snapshot tests. 446 tests remain with identical coverage.

- **Updated dependencies**: ANcpLua.Roslyn.Utilities 1.28.0 → 1.30.2, cleaned up .editorconfig with generator-specific
  suppression rationale.

- **Generator code cleanup**: Removed unused `ParameterSource` properties, simplified constructor signatures, added
  documentation comments for suppressed warnings in .csproj.

### Fixed

- **Integration tests missing from solution**: Added `ErrorOrX.Integration.Tests` to `ErrorOrX.slnx`.

- **Documentation accuracy**: Fixed README analyzer count (38 → 41), corrected CLAUDE.md dependency versions.

## [3.3.0] - 2026-02-04

### Removed

- **tools/ directory**: Removed internal DiagnosticsAlignment tooling
    - `DiagnosticsAlignment.Analyzers` was orphaned (not referenced by any project)
    - `DiagnosticsAlignment.Host` was dead code (empty class with unused AdditionalFiles)
    - `DiagnosticsAlignment.Tool` functionality moved to ANcpLua.Analyzers

### Fixed

- **MSB4011 double-import warning**: Added `DirectoryBuildPropsImported` guard property to prevent nested
  `Directory.Build.props` from importing root twice

- **AL0018 false positive**: Moved suppression to root `Directory.Build.props` since repo uses CPM
  (`ManagePackageVersionsCentrally=true`) which AL0018 couldn't detect without AdditionalFiles

- **Unused properties in ParameterSource**: Removed `IsFromRequest`, `RequiresJsonContext`, `IsSpecialType`,
  `IsComposite` properties that were never read. Constructor params kept as discards for API stability.

## [3.2.0] - 2026-02-01

### Fixed

- **BUG-001: TryGetReferencedSymbol NRE for ILocalSymbol**: Fixed `NullReferenceException` when error type inference
  encountered local variables referencing Error factories. `ILocalSymbol` has no `ContainingAssembly`, but is always
  in scope for the current method.

- **BUG-002: AttributeClass null checks**: Added null guards on `attr.AttributeClass` in `HasParameterAttribute` and
  `TryGetAttributeName` methods. `AttributeClass` can be null for malformed attributes or missing references.

- **BUG-004: BuildRouteParameterLookup determinism**: Changed from "last wins" to "first wins" semantics when multiple
  parameters bind to the same route name. This provides deterministic behavior and prevents silent data loss.

- **BUG-005: GroupAggregator empty endpoints**: Added defensive check before accessing first element of `endpoints`
  array in `CreateAggregate`. Prevents undefined behavior if collection is empty.

- **N+1 symbol lookup performance**: Fixed N+1 performance issue where `ErrorOrContext` was created once per endpoint,
  causing 90+ symbol lookups per endpoint. Now creates `ErrorOrContext` once per compilation using
  `CompilationProvider` with `.Combine()` pattern. Reduces symbol lookups from N×90 to 90 for N endpoints.

### Added

- **BugRegressionTests**: New test class with regression tests for BUG-001 through BUG-005 to prevent future
  regressions.

- **DiagnosticTests**: Comprehensive test class covering 26 diagnostic scenarios including EOE003 (route parameter not
  bound),
  EOE005 (invalid route pattern), EOE006 (multiple body sources), EOE011-EOE016 (invalid binding types), EOE017-EOE021
  (type restrictions), EOE023 (constraint mismatch), EOE025 (ambiguous binding), and EOE032 (unknown error factory).
  Increases diagnostic test coverage from 6/32 to 32/32.

## [3.0.1] - 2026-01-25

### Fixed

- **Package installation issue**: Removed `developmentDependency=true` from `.nuspec` to prevent NuGet from
  automatically adding `PrivateAssets=all` when installing via `dotnet add package`. Added MSBuild target in `.props`
  that explicitly adds ErrorOrX reference with `PrivateAssets=none` if not already present. Users no longer need to
  manually edit `.csproj` after installation.

## [3.0.0] - 2026-01-25

### Changed

- **Consolidated type unwrapping logic**: Moved duplicate `UnwrapNullableType` implementations from `RouteValidator` and
  `ErrorOrEndpointAnalyzer` into `TypeNameHelper.UnwrapNullable`. Reduces code duplication and ensures consistent
  nullable type handling across the generator.

- **Consolidated route parameter lookup**: Unified `BuildMethodParamsByRouteName` and `BuildRouteMethodParameterLookup`
  into `RouteValidator.BuildRouteParameterLookup` with optional `requireTypeFqn` parameter. Eliminates duplicate
  dictionary-building logic between generator and analyzer.

- **Decomposed EmitInvoker method**: Refactored 95-line `EmitInvoker` into focused single-responsibility methods:
  `ComputeInvokerContext`, `EmitBodyCode`, `EmitErrorHandling`, `EmitWrapperMethod`, `EmitCoreMethod`. Introduced
  `InvokerContext` record struct with clear flag separation (`HasFormBinding`, `HasBodyBinding`, `NeedsAwait`).
  Generated code is byte-identical to previous version.

- **Removed Match API dependency from generated code**: The fallback error handling path now uses the minimal
  `ErrorOr<T>` interface (`IsError`/`Errors`/`Value`) instead of the convenience `Match` API. This reduces the
  runtime coupling between generated endpoints and the ErrorOr library, making the generated code more portable
  and easier to understand. Removed the unused `GetMatchFactoryWithLocation` helper and `MatchFactory` property
  from `SuccessResponseInfo`.

### Fixed

- **Test analyzer cleanups**: Sealed test helper records, added a shared `Unreachable` helper, and adjusted interface
  tests to satisfy CA1820, CA1812, CA1826, and CA2201.

### Documentation

- **Enterprise guidance**: Documented upstreaming `Throw.UnreachableException` changes to ANcpLua.NET.Sdk and bumping
  `global.json` `msbuild-sdks` to the new SDK version (plus `Directory.Packages.props` if a package reference is added).

## [2.6.3] - 2026-01-18

### Removed

- **AotJsonGenerator and related attributes**: Removed the `[AotJson]` and `[AotJsonAssembly]` attributes along with
  the `AotJsonGenerator`. These features attempted to auto-generate `[JsonSerializable]` attributes, but due to a
  fundamental source generator limitation (generators run in parallel and cannot add attributes that other generators
  will see), the STJ generator never processed these generated attributes. Users must manually add `[JsonSerializable]`
  attributes to their `JsonSerializerContext`.

- **AOTJ diagnostics**: Removed AOTJ001-AOTJ005 diagnostics since the AotJson feature has been removed.

## [2.6.2] - 2026-01-18

### Fixed

- **False positive EOE007 warnings for generic types**: Fixed type matching to correctly compare types like
  `List<Todo>` against the generator's fully-qualified `List<global::Todo>`. The `TypeNameHelper.Normalize`
  method now strips all `global::` prefixes including those inside generic type arguments.

## [2.6.1] - 2026-01-18

Re-release with correct generated code. The 2.6.0 package was inadvertently built from stale code.

## [2.6.0] - 2026-01-17

### Added

- **Fluent Configuration Builder**: New `AddErrorOrEndpoints()` extension method with fluent API for configuring JSON
  options:

```csharp
services.AddErrorOrEndpoints(options => options
    .UseJsonContext<AppJsonSerializerContext>()  // Register AOT-compatible JSON context
    .WithCamelCase()                              // Enable camelCase naming (default: true)
    .WithIgnoreNulls());                          // Ignore null values (default: true)
```

### Removed

- `AddErrorOrEndpointJson<TContext>()` - replaced by `AddErrorOrEndpoints()` fluent builder.
- `ErrorOrLegacyParameterBinding` MSBuild property - smart parameter binding is now always enabled.

### Fixed

- **Native AOT Runtime Support**: Fixed critical runtime failure where AOT-published apps would throw
  `NotSupportedException: JsonTypeInfo metadata for type 'Task<Results<...>>' was not provided`.

### Changed

#### Handler Pattern for AOT Compatibility

The generated endpoint handlers now use a wrapper pattern that properly writes responses to `HttpContext`:

```csharp
// Before: Returned Task<Results<...>> which failed in AOT
private static async Task<Results<Ok<Todo>, NotFound<ProblemDetails>>> Invoke_Ep0(HttpContext ctx)
{
    // ... logic returning Results<...>
}

// After: Returns Task (matches RequestDelegate) and calls ExecuteAsync
private static async Task Invoke_Ep0(HttpContext ctx)
{
    var __result = await Invoke_Ep0_Core(ctx);
    await __result.ExecuteAsync(ctx);  // Writes response to HttpContext
}

private static async Task<Results<...>> Invoke_Ep0_Core(HttpContext ctx)
{
    // ... logic returning Results<...>
}
```

#### Accepts Metadata via WithMetadata

Replaced `.Accepts<T>()` (which requires `RouteHandlerBuilder`) with `.WithMetadata(AcceptsMetadata)` (which works on
`IEndpointConventionBuilder`):

```csharp
// Before: Required Delegate cast for RouteHandlerBuilder
app.MapPost("/todos", (Delegate)Invoke_Ep0)
    .Accepts<CreateTodoRequest>("application/json")

// After: Uses AcceptsMetadata for broader compatibility
app.MapPost("/todos", Invoke_Ep0)
    .WithMetadata(new AcceptsMetadata(new[] { "application/json" }, typeof(CreateTodoRequest)))
```

### Technical Details

The root cause was that passing handlers with `(Delegate)` cast to `Map*` methods forced ASP.NET Core to use
reflection-based `RequestDelegateFactory`, which requires JSON metadata for `Task<Results<...>>` - metadata that doesn't
exist because these are BCL types.

The fix:

1. Handlers now return `Task` (matching `RequestDelegate` signature) instead of `Task<Results<...>>`
2. The wrapper calls `IResult.ExecuteAsync(HttpContext)` to write the response
3. Core logic remains in a `_Core` method that returns typed `Results<...>` for OpenAPI documentation
4. No `(Delegate)` cast needed - the handler naturally matches `RequestDelegate`

---

## [2.4.0] - 2026-01-13

### Added

- **New `Or*` extension methods** for fluent nullable-to-ErrorOr conversion:
    - `.OrNotFound(description)` - Returns NotFound error if null
    - `.OrValidation(description)` - Returns Validation error if null
    - `.OrUnauthorized(description)` - Returns Unauthorized error if null
    - `.OrForbidden(description)` - Returns Forbidden error if null
    - `.OrConflict(description)` - Returns Conflict error if null
    - `.OrFailure(description)` - Returns Failure error if null

  Error codes are auto-generated from the type name (e.g., `Todo.NotFound`).

  ```csharp
  // Before (verbose)
  return _todos.Find(t => t.Id == id) is { } todo
      ? todo
      : Error.NotFound("Todo.NotFound", $"Todo {id} not found");

  // After (clean one-liner)
  return _todos.Find(t => t.Id == id).OrNotFound($"Todo {id} not found");
  ```

- **`ToErrorOr` overload for nullable types** with explicit error parameter

---

## [2.3.1] - 2026-01-13

### Documentation

- Streamlined README for NuGet package
- Added `docs/` folder with detailed documentation

---

## [2.3.0] - 2026-01-13

### Breaking Changes

#### Smart Parameter Binding Inference

The generator now infers parameter binding based on HTTP method and type:

```csharp
// POST/PUT/PATCH with complex type → automatically bound from body
[Post("/todos")]
public static ErrorOr<Todo> Create(CreateTodoRequest req)  // No [FromBody] needed!

// Interface types → automatically resolved from DI
[Get("/todos")]
public static ErrorOr<List<Todo>> GetAll(ITodoService svc)  // No [FromServices] needed!
```

**Breaking:** GET/DELETE with complex types now require explicit binding:

```csharp
// Before v2.3.0: Would silently try DI injection
// After v2.3.0: Error EOE025

[Get("/users")]
public static ErrorOr<List<User>> Search(SearchFilter filter)  // ❌ EOE025

// Fix with explicit attribute:
[Get("/users")]
public static ErrorOr<List<User>> Search([FromQuery] SearchFilter filter)  // ✅
// Or use [AsParameters] for expanded binding
```

### Added

- **EOE025 Diagnostic**: Error when GET/DELETE endpoints have complex type parameters without explicit binding attribute
- **Service Type Detection**: Interface types and common DI patterns (`*Repository`, `*Handler`, `*Manager`,
  `*Provider`, `*Factory`, `*Client`) are automatically detected as services
- **Complex Type Detection**: DTOs and request objects are distinguished from primitives and services

### Fixed

- **Incremental Caching**: Generator now properly caches outputs when source is unchanged. Fixed by removing
  `CompilationProvider` usage that cached `CSharpCompilation` references.

### Internal Changes

- Replaced dynamic max arity detection with hardcoded `const int maxArity = 6` to avoid `CompilationProvider`
- `ErrorOrContext` is now created lazily from `SemanticModel.Compilation` instead of early combination
- Added `GeneratorCachingTests` to verify incremental caching behavior
- Added XML documentation to core generator types

### Documentation

- Streamlined README to focus on quick start
- Created `docs/` folder with detailed documentation:
    - `docs/api.md` - Full API reference
    - `docs/parameter-binding.md` - Parameter binding details
    - `docs/diagnostics.md` - Analyzer warnings and errors

---

## [2.2.0] - 2026-01-12

### Breaking Changes

#### Split Package Architecture

The single `ErrorOrX` package has been split into two packages for cleaner separation:

```diff
- <PackageReference Include="ErrorOrX" Version="2.1.1" />
+ <PackageReference Include="ErrorOrX.Generators" Version="2.2.0" />
```

**Note:** `ErrorOrX.Generators` automatically brings in `ErrorOrX` as a dependency - you only need to reference the
generators package.

| Package               | Target           | Contents                                                                 |
|-----------------------|------------------|--------------------------------------------------------------------------|
| `ErrorOrX`            | `net10.0`        | Runtime types: `ErrorOr<T>`, `Error`, `ErrorType`, fluent API extensions |
| `ErrorOrX.Generators` | `netstandard2.0` | Source generator for ASP.NET Core Minimal API endpoints                  |

### Why Split?

- **Cleaner NuGet package structure**: Runtime DLL in `lib/net10.0/`, generator in `analyzers/dotnet/cs/`
- **Proper dependency flow**: Consumers get runtime types transitively via generator package dependency
- **Better tooling support**: IDE analyzers load from standard locations

### Migration

Update your package reference:

```xml
<!-- Before -->
<PackageReference Include="ErrorOrX" Version="2.1.1"/>

    <!-- After -->
<PackageReference Include="ErrorOrX.Generators" Version="2.2.0"/>
```

No code changes required - all namespaces remain `ErrorOr`.

### Internal Changes

- Consolidated test projects: `ErrorOr.Core.Tests` + `ErrorOr.Endpoints.Tests` → `ErrorOrX.Tests` +
  `ErrorOrX.Generators.Tests`
- Renamed sample project: `ErrorOr.Endpoints.Sample` → `ErrorOrX.Sample`
- Test count increased from 218 to 267 (merged comprehensive tests from both projects)
- Updated solution file to use new project structure

---

## [2.1.1] - 2026-01-12

### Changed

#### Generator Folder Restructure

Organized 17 generator files into logical subdirectories:

```
Generators/
├── Core/           (5 files) - Main generator partials
├── Models/         (3 files) - Data structures
├── TypeResolution/ (3 files) - Symbol/type computation
├── Validation/     (2 files) - Route validation
├── Helpers/        (3 files) - Utilities
└── OpenApiTransformerGenerator.cs
```

#### Generator Codebase Consolidation

Reduced generator file count from 21 to 17 files through strategic consolidation:

| Deleted File          | Merged Into         | Rationale                                  |
|-----------------------|---------------------|--------------------------------------------|
| `ErrorTypeNames.cs`   | `ErrorMapping.cs`   | SSOT: all error-to-HTTP logic in one place |
| `StatusCodeTitles.cs` | `ErrorMapping.cs`   | Only consumer was ErrorMapping             |
| `ParameterModels.cs`  | `EndpointModels.cs` | Both are endpoint-related data structures  |
| `AspNetContext.cs`    | *(deleted)*         | Dead code - duplicated ErrorOrContext      |

#### ErrorMapping.cs Now Single Source of Truth

```csharp
// Before: scattered across 3 files
ErrorTypeNames.Validation     // ErrorTypeNames.cs
StatusCodeTitles.Get(400)     // StatusCodeTitles.cs
ErrorMapping.Get(errorType)   // ErrorMapping.cs

// After: consolidated in ErrorMapping.cs
ErrorMapping.Validation           // Error type constants
ErrorMapping.GetStatusTitle(400)  // RFC 9110 titles
ErrorMapping.Get(errorType)       // HTTP mappings
ErrorMapping.IsKnownErrorType(s)  // Validation
ErrorMapping.AllErrorTypes        // Deterministic iteration
```

### Internal Changes

- Added `#region` sections to `ErrorMapping.cs` for logical separation
- Added `#region` sections to `EndpointModels.cs` for parameter/endpoint models
- Updated all references from `ErrorTypeNames.X` to `ErrorMapping.X`
- Updated `StatusCodeTitles.Get()` to `ErrorMapping.GetStatusTitle()`
- Renamed `ErrorTypeNamesSyncTests.cs` to `ErrorMappingSyncTests.cs`
- Removed unused `AspNetContext.cs` (all properties already in `ErrorOrContext.cs`)

### Tests

- All 49 tests passing
- Sync tests updated to validate `ErrorMapping` constants against runtime `ErrorType` enum

---

## [2.1.0] - 2026-01-10

### Added

- **BCL Middleware Attribute Detection**: Generator now recognizes and emits fluent calls for standard ASP.NET Core
  middleware attributes:

  | Attribute                         | Emitted Call                                            |
      |-----------------------------------|---------------------------------------------------------|
  | `[Authorize]`                     | `.RequireAuthorization()`                               |
  | `[Authorize("Policy")]`           | `.RequireAuthorization("Policy")`                       |
  | `[AllowAnonymous]`                | `.AllowAnonymous()`                                     |
  | `[EnableRateLimiting("policy")]`  | `.RequireRateLimiting("policy")`                        |
  | `[DisableRateLimiting]`           | `.DisableRateLimiting()`                                |
  | `[OutputCache]`                   | `.CacheOutput()`                                        |
  | `[OutputCache(Duration = 60)]`    | `.CacheOutput(p => p.Expire(TimeSpan.FromSeconds(60)))` |
  | `[OutputCache(PolicyName = "x")]` | `.CacheOutput("x")`                                     |
  | `[EnableCors("policy")]`          | `.RequireCors("policy")`                                |
  | `[DisableCors]`                   | `.DisableCors()`                                        |

- **Automatic Results<> Union Updates**: When middleware attributes are detected, the union type automatically includes
  appropriate HTTP result types:
    - `[Authorize]` adds `UnauthorizedHttpResult` (401) and `ForbidHttpResult` (403)
    - `[EnableRateLimiting]` adds `StatusCodeHttpResult` (429)

### Example

```csharp
// Before v2.1
app.MapMethods(@"/todos", new[] { "POST" }, (Delegate)Invoke_Ep3)
    .WithName("TodoApi_Create")
    .Accepts<CreateTodoRequest>("application/json");

// After v2.1 with [Authorize("Admin")] and [EnableRateLimiting("fixed")]
app.MapMethods(@"/todos", new[] { "POST" }, (Delegate)Invoke_Ep3)
    .WithName("TodoApi_Create")
    .Accepts<CreateTodoRequest>("application/json")
    .RequireAuthorization("Admin")      // NEW
    .RequireRateLimiting("fixed");      // NEW
```

### Internal Changes

- Added `MiddlewareInfo` record to track detected middleware configuration
- Added `ExtractMiddlewareAttributes()` to detect BCL attributes on endpoints
- Added `EmitMiddlewareCalls()` to emit corresponding fluent method calls
- Updated `ResultsUnionTypeBuilder` to include 401/403/429 in union when relevant
- Fallback to `IResult` when union exceeds 8 type parameters still works correctly

---

## [2.0.0] - 2026-01-10

### Breaking Changes

#### Single Package Architecture

Replaced two packages with one unified package:

```diff
- <PackageReference Include="ErrorOr.Core" Version="1.x" />
- <PackageReference Include="ErrorOr.Endpoints" Version="1.x" />
+ <PackageReference Include="ErrorOr" Version="2.0.0" />
```

#### Namespace Simplification

| Before (v1.x)                                                                                                      | After (v2.0.0)         |
|--------------------------------------------------------------------------------------------------------------------|------------------------|
| `using ErrorOr;`<br>`using ErrorOr.Core.ErrorOr;`<br>`using ErrorOr.Core.Errors;`<br>`using ErrorOr.Core.Results;` | `using ErrorOr;`       |
| `ErrorOr.Core.ErrorOr.ErrorOr<T>`                                                                                  | `ErrorOr.ErrorOr<T>`   |
| `ErrorOr.Core.Errors.Error`                                                                                        | `ErrorOr.Error`        |
| `ErrorOr.Endpoints.GetAttribute`                                                                                   | `ErrorOr.GetAttribute` |

#### Parameter Binding Attributes Now Optional

`[FromRoute]`, `[FromBody]`, `[FromQuery]` attributes are now automatically inferred:

```csharp
// Before (v1.x)
[Get("/todos/{id:int}")]
public static ErrorOr<Todo> GetById([FromRoute] int id) => ...

[Post("/todos")]
public static ErrorOr<Todo> Create([FromBody] CreateTodoRequest request) => ...

// After (v2.0.0) - attributes optional, inferred automatically
[Get("/todos/{id:int}")]
public static ErrorOr<Todo> GetById(int id) => ...

[Post("/todos")]
public static ErrorOr<Todo> Create(CreateTodoRequest request) => ...
```

### Added

#### Automatic AOT JSON Context Generation

No longer requires manual `JsonSerializerContext` definition. The generator automatically creates `ErrorOrJsonContext`
with all endpoint types:

```csharp
// Before (v1.x) - Manual context required
builder.Services.AddErrorOrEndpointJson<AppJsonSerializerContext>().AddOpenApi();

[JsonSerializable(typeof(Todo))]
[JsonSerializable(typeof(List<Todo>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;

// After (v2.0.0) - Automatic, zero config
builder.Services.AddOpenApi();
// ErrorOrJsonContext is auto-generated with all types
```

#### Automatic Location Header for POST Endpoints

POST endpoints returning 201 Created now automatically include Location header when the response type has an `Id`
property:

```csharp
[Post("/todos")]
public static ErrorOr<Todo> Create(CreateTodoRequest req)
{
    var todo = new Todo(nextId++, req.Title);
    return todo;
}
// Returns: 201 Created
// Location: /todos/{Id}  <- Automatic!
```

#### Added `IsSuccess` Property

```csharp
ErrorOr<int> result = 42;
if (result.IsSuccess)  // NEW - inverse of IsError
{
    Console.WriteLine(result.Value);
}
```

#### Added `ToString()` Override

```csharp
ErrorOr<int> success = 42;
Console.WriteLine(success);  // "ErrorOr { IsError = False, Value = 42 }"

ErrorOr<int> error = Error.NotFound("Not.Found", "Item not found");
Console.WriteLine(error);  // "ErrorOr { IsError = True, FirstError = Not.Found }"
```

### Changed

- **Target Framework**: Now multi-targets `net10.0` (runtime) and `netstandard2.0` (generator)
- **Package Structure**: Single package contains both runtime DLL and analyzer/generator
- **JSON Serialization**: Automatic context generation replaces opt-in approach

### Removed

- `ErrorOr.Core` package (merged into `ErrorOr`)
- `ErrorOr.Endpoints` package (merged into `ErrorOr`)
- Manual `AddErrorOrEndpointJson<T>()` requirement
- Requirement for explicit `[FromRoute]`/`[FromBody]` attributes .

---

## [1.10.0] - 2024-02-14

### Added

- `ErrorType.Forbidden`
- README to NuGet package

## [1.9.0] - 2024-01-06

### Added

- `ToErrorOr`

## [1.0.0] - 2024-03-26

### Added

- `FailIf`

```csharp
public ErrorOr<TValue> FailIf(Func<TValue, bool> onValue, Error error)
```

```csharp
ErrorOr<int> errorOr = 1;
errorOr.FailIf(x => x > 0, Error.Failure());
```

### Breaking Changes

- `Then` that receives an action is now called `ThenDo`

```diff
-public ErrorOr<TValue> Then(Action<TValue> action)
+public ErrorOr<TValue> ThenDo(Action<TValue> action)
```

```diff
-public static async Task<ErrorOr<TValue>> Then<TValue>(this Task<ErrorOr<TValue>> errorOr, Action<TValue> action)
+public static async Task<ErrorOr<TValue>> ThenDo<TValue>(this Task<ErrorOr<TValue>> errorOr, Action<TValue> action)
```

- `ThenAsync` that receives an action is now called `ThenDoAsync`

```diff
-public async Task<ErrorOr<TValue>> ThenAsync(Func<TValue, Task> action)
+public async Task<ErrorOr<TValue>> ThenDoAsync(Func<TValue, Task> action)
```

```diff
-public static async Task<ErrorOr<TValue>> ThenAsync<TValue>(this Task<ErrorOr<TValue>> errorOr, Func<TValue, Task> action)
+public static async Task<ErrorOr<TValue>> ThenDoAsync<TValue>(this Task<ErrorOr<TValue>> errorOr, Func<TValue, Task> action)
```
