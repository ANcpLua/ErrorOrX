# CLAUDE.md - ErrorOrX Monorepo

> **Operational brain for the ErrorOrX ecosystem.** Read this completely before any task.

---

## 🚨 PRIORITY 1: Single-Package Migration

**STATUS:** Ready to execute  
**PATTERN:** Refit/System.Text.Json (proven)  
**BREAKING:** Yes (v4.0.0)

### Target Architecture

```
ErrorOr.nupkg (SINGLE PACKAGE)
├── lib/net10.0/
│   └── ErrorOr.dll              ← Runtime (ErrorOr<T>, Error, Attributes)
└── analyzers/dotnet/cs/
    └── ErrorOr.Generators.dll   ← Generator (netstandard2.0)
```

### Target Source Structure

```
src/
├── Shared/
│   └── ErrorType.cs              ← SINGLE source of truth (conditional compile)
│
├── ErrorOr/                      # net10.0 - Main package project
│   ├── ErrorOr.csproj
│   ├── Error.cs
│   ├── ErrorOr.cs                ← partial record struct
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
│   ├── IErrorOr.cs
│   ├── EmptyErrors.cs
│   ├── Results.cs                ← Success, Created, Deleted, Updated
│   └── Endpoints/
│       └── Attributes.cs         ← [Get], [Post], [ProducesError], [ReturnsError]
│
└── ErrorOr.Generators/           # netstandard2.0 - Bundled as analyzer
    ├── ErrorOr.Generators.csproj
    ├── Analyzers/
    │   ├── Descriptors.cs
    │   └── ErrorOrEndpointAnalyzer.cs
    └── Generators/
        ├── ErrorOrEndpointGenerator.cs
        ├── ErrorOrEndpointGenerator.Emitter.cs
        ├── ErrorOrEndpointGenerator.Extractor.cs
        ├── ErrorOrEndpointGenerator.Initialize.cs
        ├── Models.cs
        └── Internal/
            └── ErrorType.cs      → Link to ../Shared/ErrorType.cs
```

### Migration Steps (Execute in Order)

```bash
# 0. Verify clean state
cd /path/to/ErrorOrX
git status --short  # Must be clean
dotnet build ErrorOrX.slnx && dotnet test ErrorOrX.slnx

# 1. Create Shared directory with conditional ErrorType
mkdir -p src/Shared
# Create ErrorType.cs with #if ERROROR_GENERATOR conditional

# 2. Create src/ErrorOr/ from ErrorOr.Core content
mkdir -p src/ErrorOr/Endpoints
# Copy all files from src/ErrorOr.Core/ to src/ErrorOr/
# Add Attributes.cs to src/ErrorOr/Endpoints/

# 3. Create src/ErrorOr.Generators/ from ErrorOr.Endpoints generators
mkdir -p src/ErrorOr.Generators/Analyzers
mkdir -p src/ErrorOr.Generators/Generators/Internal
# Copy generator code, update namespaces

# 4. Update csproj files (see templates below)

# 5. Delete old directories
rm -rf src/ErrorOr.Core src/ErrorOr.Endpoints src/ErrorOr.Endpoints.CodeFixes

# 6. Update tests (namespace changes)
# 7. Update sample project
# 8. Build + Pack verification
dotnet build ErrorOrX.slnx
dotnet test ErrorOrX.slnx
dotnet pack src/ErrorOr/ErrorOr.csproj -o ./nupkgs
unzip -l nupkgs/ErrorOr.*.nupkg | grep -E "(lib/|analyzers/)"
```

### Key Files to Create

#### src/Shared/ErrorType.cs
```csharp
#if ERROROR_GENERATOR
namespace ErrorOr.Generators.Internal;
internal enum ErrorType
#else
namespace ErrorOr;
public enum ErrorType
#endif
{
    Failure = 0,
    Unexpected = 1,
    Validation = 2,
    Conflict = 3,
    NotFound = 4,
    Unauthorized = 5,
    Forbidden = 6,
}
```

#### src/ErrorOr/ErrorOr.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <PackageId>ErrorOr</PackageId>
    <RootNamespace>ErrorOr</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <!-- Shared source file -->
  <ItemGroup>
    <Compile Include="..\Shared\ErrorType.cs" Link="ErrorType.cs" />
  </ItemGroup>

  <!-- Bundle generator as analyzer -->
  <ItemGroup>
    <ProjectReference Include="..\ErrorOr.Generators\ErrorOr.Generators.csproj"
                      PrivateAssets="all"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
  </ItemGroup>
</Project>
```

#### src/ErrorOr.Generators/ErrorOr.Generators.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <DefineConstants>$(DefineConstants);ERROROR_GENERATOR</DefineConstants>
    <IsRoslynComponent>true</IsRoslynComponent>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
    <RootNamespace>ErrorOr.Generators</RootNamespace>
  </PropertyGroup>

  <!-- Shared source with conditional compile -->
  <ItemGroup>
    <Compile Include="..\Shared\ErrorType.cs" Link="Internal\ErrorType.cs" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="$(RoslynVersion)" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

### Consumer Experience After Migration

```xml
<!-- ONE package instead of two -->
<PackageReference Include="ErrorOr" Version="4.0.0" />
```

```csharp
using ErrorOr;

[Get("/todos/{id}")]
[ProducesError(ErrorType.NotFound, "Todo.NotFound")]
public static async Task<ErrorOr<Todo>> GetById(Guid id, ITodoService svc)
    => await svc.GetByIdAsync(id);
```

### Namespace Changes (Breaking)

| Before | After |
|--------|-------|
| `ErrorOr.Core` | `ErrorOr` |
| `ErrorOr.Endpoints` | `ErrorOr` (attributes) |
| `ErrorOr.Endpoints.Generators` | `ErrorOr.Generators` |

### Verification Checklist

- [ ] `dotnet build ErrorOrX.slnx` - zero errors
- [ ] `dotnet test ErrorOrX.slnx` - all tests pass
- [ ] Package contains `lib/net10.0/ErrorOr.dll`
- [ ] Package contains `analyzers/dotnet/cs/ErrorOr.Generators.dll`
- [ ] Sample project builds and runs
- [ ] OpenAPI spec shows endpoints with error responses

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
│   NO SKIPPING STEPS. NO SHORTCUTS. NO "SHIP IT" PRESSURE.                   │
│                                                                             │
│   Every feature gets:                                                       │
│   - Root implementation (Models.cs)                                         │
│   - Leaf consumption (Emitter.cs)                                           │
│   - Tests                                                                   │
│   - Documentation                                                           │
│                                                                             │
│   Spec compliance: ERROROR_TYPEDRESULTS_SPEC.md is the contract.            │
│   Code = OpenAPI = Runtime. Any deviation = Bug OR documented decision.     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### O(1) Architecture Verification

```
GetErrorMapping() in Models.cs = SINGLE SOURCE OF TRUTH
      │
      ├──> GetStatusCodeForErrorType() - delegates
      │
      └──> GenerateErrorTypeToStatusSwitch() - derives via .Select(GetErrorMapping)
                │
                ├──> EmitProblemDetailsBuilding() - uses
                │
                └──> EmitSupportMethods() ToProblem - uses

To change ErrorType.Failure → 503:
  → Change ONE line in GetErrorMapping()
  → Everything else follows automatically
```

---

## Build & Test Commands

```bash
# Build
dotnet build ErrorOrX.slnx

# Test
dotnet test ErrorOrX.slnx

# Test with filter (xUnit v3 MTP)
dotnet test ErrorOrX.slnx -- --filter-class SomeTestClass  # .NET 9
dotnet test ErrorOrX.slnx --filter-class SomeTestClass     # .NET 10+

# Package
dotnet pack ErrorOrX.slnx -o ./nupkgs

# Verify package contents
unzip -l nupkgs/ErrorOr.*.nupkg | grep -E "(lib/|analyzers/)"
```

---

## ErrorType → HTTP Status Code Mapping (RFC 9110)

**CANONICAL MAPPING - DO NOT CHANGE WITHOUT RFC JUSTIFICATION**

| ErrorType | HTTP | TypedResults | Has Body |
|-----------|------|--------------|----------|
| `Validation` | 400 | `ValidationProblem` | ✅ Yes |
| `Unauthorized` | 401 | `UnauthorizedHttpResult` | ❌ No |
| `Forbidden` | 403 | `ForbidHttpResult` | ❌ No |
| `NotFound` | 404 | `NotFound<ProblemDetails>` | ✅ Yes |
| `Conflict` | 409 | `Conflict<ProblemDetails>` | ✅ Yes |
| `Failure` | **500** | `InternalServerError<ProblemDetails>` | ✅ Yes |
| `Unexpected` | **500** | `InternalServerError<ProblemDetails>` | ✅ Yes |
| _default_ | **500** | `InternalServerError<ProblemDetails>` | ✅ Yes |

### Success Mappings

| SuccessKind | HTTP | TypedResult |
|-------------|------|-------------|
| GET with payload | 200 | `Ok<T>` |
| POST with payload | 201 | `Created<T>` |
| Updated | 204 | `NoContent` |
| Deleted | 204 | `NoContent` |

### Critical: Failure ≠ 422

```
⚠️  ErrorType.Failure MUST map to 500, NEVER to 422

RFC 9110 §15.5.21 (422 Unprocessable Content):
  → CLIENT error - request was semantically invalid

RFC 9110 §15.6.1 (500 Internal Server Error):
  → SERVER error - something went wrong on the server

ErrorType.Failure = server problem → 500
For 422, use: Error.Custom(422, "Code", "Description")
```

---

## Diagnostic Reference

| ID | Description |
|----|-------------|
| EOE001 | Invalid return type (must be ErrorOr<T> or Task<ErrorOr<T>>) |
| EOE002 | Handler must be static |
| EOE003 | Route parameter not bound |
| EOE004 | Duplicate route pattern |
| EOE005 | Invalid route pattern syntax |
| EOE006 | Multiple body sources |
| EOE007 | Missing JSON context registration |
| EOE008 | Undocumented custom error (missing [ProducesError]) |
| EOE009 | Body on GET/HEAD/DELETE/OPTIONS |
| EOE026 | Error.Custom without [ProducesError] |
| EOE030 | Union type exceeds 6-type BCL limit |
| EOE033 | Interface call without [ReturnsError] documentation |

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
| `[ReturnsError(ErrorType, code)]` | Interface method | Contract-level error docs |
| `[ReturnsError(statusCode, code)]` | Interface method | Contract-level custom status |

### EOE033: Undocumented Interface Call

```csharp
// ❌ FAILS BUILD (EOE033 Error)
[Get("/api/todos/{id}")]
public static Task<ErrorOr<Todo>> GetById([FromServices] ITodoService svc, ...)
    => svc.GetByIdAsync(id);  // Interface call without documentation

// ✅ PASSES - Option 1: [ProducesError] on endpoint
[Get("/api/todos/{id}")]
[ProducesError(404, "Todo.NotFound")]
public static Task<ErrorOr<Todo>> GetById([FromServices] ITodoService svc, ...)
    => svc.GetByIdAsync(id);

// ✅ PASSES - Option 2: [ReturnsError] on interface (PREFERRED)
interface ITodoService
{
    [ReturnsError(ErrorType.NotFound, "Todo.NotFound")]
    Task<ErrorOr<Todo>> GetByIdAsync(Guid id);
}

[Get("/api/todos/{id}")]
public static Task<ErrorOr<Todo>> GetById([FromServices] ITodoService svc, ...)
    => svc.GetByIdAsync(id);  // Errors inferred from interface
```

---

## Dependencies

### Runtime (ErrorOr)
- net10.0 only (no polyfills needed)

### Generator (ErrorOr.Generators)
- Microsoft.CodeAnalysis.CSharp 5.0.0 (Roslyn for C# 14)
- netstandard2.0 (bundled in package as analyzer)

### Testing
- xunit.v3 + Microsoft Testing Platform
- AwesomeAssertions 9.x
- Verify.SourceGenerators

---

## Design Decisions

### Updated → 204 NoContent (not 200 Ok)

`ErrorOr<Updated>` is a marker type signaling "update succeeded" without returning data.
This maps semantically to 204, not 200.

```csharp
// Returns 204 NoContent (marker type)
public static ErrorOr<Updated> UpdateEmail(Guid id, string email) { ... }

// Returns 200 Ok<User> (value type with body)
public static ErrorOr<User> UpdateUser(Guid id, UpdateRequest req) { ... }
```

### Location Header = string.Empty

Generator lacks runtime info (entity ID, route name) needed for proper Location URI.
Using `string.Empty` is valid per RFC 9110 (relative URI).

---

## Failure Conditions

You have FAILED if:

1. You execute migration steps out of order
2. You skip creating Shared/ErrorType.cs with conditional compile
3. You claim "build passes" without showing `dotnet build` output
4. Generated package missing either `lib/` or `analyzers/` content
5. Sample project doesn't build after migration

## Success Conditions

You have SUCCEEDED when:

1. `dotnet build ErrorOrX.slnx` - zero errors
2. `dotnet test ErrorOrX.slnx` - all tests pass
3. Package structure verified: `lib/net10.0/` + `analyzers/dotnet/cs/`
4. Sample project runs and serves OpenAPI spec
5. CHANGELOG.md updated with v4.0.0 breaking changes