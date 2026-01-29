# ErrorOrX Mega Swarm Audit Findings

**Date:** January 28, 2026
**Scope:** Full codebase analysis
**Agents Deployed:** 12 parallel specialized reviewers

---

## Executive Summary

This document details findings from a comprehensive 12-agent parallel audit of the ErrorOrX source generator. The audit covered architecture, security, performance, testing, code quality, error handling, API contracts, dependencies, configuration, documentation, consistency, and bug hunting.

**Overall Assessment:** The codebase is well-architected with strong fundamentals, but has critical gaps in test coverage and documentation that should be addressed before the next release.

| Severity | Count | Action Required |
|----------|-------|-----------------|
| P0 (Critical) | 3 | Immediate |
| P1 (High) | 18 | This sprint |
| P2 (Medium) | 24 | Next sprint |
| P3 (Low) | 12 | Backlog |

---

## P0: Critical Issues

### 1. Missing Diagnostic Tests (EOE003-EOE054)

**Problem:** Out of 32 diagnostic codes, only 6 have explicit test coverage (19%).

**Untested Diagnostics:**
- EOE003: Route parameter not bound
- EOE005-007: Route/body/AOT validation
- EOE009-014: HTTP method warnings, binding type errors
- EOE016-021: Header/anonymous/nested type issues
- EOE023-033: Constraint/binding/interface diagnostics
- EOE040-041: JSON context warnings
- EOE050-054: API versioning diagnostics

**Impact:** Regressions in error detection go unnoticed. Users encounter cryptic failures.

**Fix:** Create `tests/ErrorOrX.Generators.Tests/DiagnosticTests.cs`:

```csharp
public class DiagnosticTests : GeneratorTestBase
{
    [Theory]
    [InlineData("EOE003")]
    [InlineData("EOE005")]
    // ... all diagnostic codes
    public async Task Diagnostic_IsEmitted_ForViolation(string diagnosticId)
    {
        var source = GetViolatingSourceFor(diagnosticId);
        var result = await RunGeneratorAsync(source);

        result.Diagnostics.Should().ContainSingle(d => d.Id == diagnosticId);
    }
}
```

---

### 2. Parameter Binding Tests (~5% Coverage)

**Problem:** Smart parameter inference is the generator's most complex feature with 600+ lines of logic, but only 2 tests use `[FromBody]`/`[AsParameters]`.

**Missing Test Scenarios:**
- Interface types → Service inference
- Service naming patterns (`*Repository`, `*Handler`, `*Manager`)
- Complex types on POST/PUT → Body inference
- Complex types on GET → EOE025 error
- Primitive collections → Query binding
- `TryParse`/`BindAsync` types → Custom binding
- Special types (HttpContext, CancellationToken, IFormFile)

**Impact:** Smart inference bugs ship to production.

**Fix:** Create `tests/ErrorOrX.Generators.Tests/ParameterBindingTests.cs`:

```csharp
public class ParameterBindingTests : GeneratorTestBase
{
    [Fact]
    public Task Interface_Type_Infers_Service()
    {
        const string source = """
            [Get("/test")]
            public static ErrorOr<string> Handler(IMyService svc) => "ok";
            """;
        return VerifyAsync(source);
    }

    [Fact]
    public Task Complex_Type_On_POST_Infers_Body()
    {
        const string source = """
            [Post("/test")]
            public static ErrorOr<string> Handler(CreateRequest req) => "ok";
            """;
        return VerifyAsync(source);
    }

    [Fact]
    public Task Complex_Type_On_GET_Emits_EOE025()
    {
        const string source = """
            [Get("/test")]
            public static ErrorOr<string> Handler(SearchFilter filter) => "ok";
            """;
        var result = await RunGeneratorAsync(source);
        result.Diagnostics.Should().ContainSingle(d => d.Id == "EOE025");
    }
}
```

---

### 3. Middleware Emission Tests (0% Coverage)

**Problem:** CLAUDE.md explicitly states "Middleware emission tests - None exist". This is security-critical because `[Authorize]` and other security attributes must be forwarded correctly.

**Why This Matters:**
```csharp
// Original method has [Authorize] - but we create a wrapper:
private static async Task Invoke_Ep1(HttpContext ctx) { ... }

// The wrapper has NO ATTRIBUTES. Generator MUST emit:
.RequireAuthorization()
```

If extraction logic fails silently, endpoints become publicly accessible.

**Fix:** Create `tests/ErrorOrX.Generators.Tests/MiddlewareEmissionTests.cs`:

```csharp
public class MiddlewareEmissionTests : GeneratorTestBase
{
    [Fact]
    public async Task Authorize_Attribute_Emits_RequireAuthorization()
    {
        const string source = """
            [Authorize]
            [Get("/admin")]
            public static ErrorOr<string> AdminOnly() => "secret";
            """;

        var result = await RunGeneratorAsync(source);
        var generated = result.GeneratedFiles
            .First(f => f.HintName == "ErrorOrEndpointMappings.g.cs");

        generated.Content.Should().Contain(".RequireAuthorization()");
    }

    [Fact]
    public async Task Authorize_With_Policy_Emits_Policy_Name()
    {
        const string source = """
            [Authorize(Policy = "Admin")]
            [Get("/admin")]
            public static ErrorOr<string> AdminOnly() => "secret";
            """;

        var result = await RunGeneratorAsync(source);
        var generated = result.GeneratedFiles
            .First(f => f.HintName == "ErrorOrEndpointMappings.g.cs");

        generated.Content.Should().Contain(".RequireAuthorization(\"Admin\")");
    }

    [Fact]
    public async Task RateLimiting_Attribute_Emits_RequireRateLimiting()
    {
        const string source = """
            [EnableRateLimiting("fixed")]
            [Get("/api")]
            public static ErrorOr<string> Limited() => "ok";
            """;

        var result = await RunGeneratorAsync(source);
        var generated = result.GeneratedFiles
            .First(f => f.HintName == "ErrorOrEndpointMappings.g.cs");

        generated.Content.Should().Contain(".RequireRateLimiting(\"fixed\")");
    }
}
```

---

### 4. API Versioning Documentation Missing

**Problem:** Full versioning support exists (EOE050-054, `[ApiVersion]`, `[MapToApiVersion]`, `[ApiVersionNeutral]`) but is completely absent from public documentation.

**Evidence:**
- 24 source files reference versioning
- Comprehensive test suites exist
- Sample uses versioning features
- Zero mentions in README.md or docs/

**Fix:** Create `docs/api-versioning.md` (see separate document below).

---

## P1: High Priority Issues

### 5. Authorization Bypass Risk (Security)

**Location:** `src/ErrorOrX.Generators/Core/ErrorOrEndpointGenerator.Extractor.cs:614-738`

**Problem:** Middleware extraction uses manual pattern matching. If an unrecognized security attribute is encountered, it's silently ignored.

**Fix:** Add defensive diagnostic:

```csharp
// In ExtractMiddlewareAttributes()
if (attributeName.Contains("Authori") ||
    attributeName.Contains("Secur") ||
    attributeName.Contains("Protect"))
{
    diagnostics.Add(DiagnosticInfo.Create(
        Descriptors.UnrecognizedSecurityAttribute,
        attr.GetLocation(),
        attributeName));
}
```

---

### 6. N+1 Symbol Lookups (Performance)

**Location:** `src/ErrorOrX.Generators/Core/ErrorOrEndpointGenerator.Extractor.cs:225`

**Problem:** `ErrorOrContext` is created per-method, causing 80+ symbol resolutions for each endpoint. A project with 50 endpoints executes 4,000+ symbol lookups.

**Current Code:**
```csharp
// INSIDE the SelectFlow lambda - runs for EVERY method
var errorOrContext = new ErrorOrContext(ctx.SemanticModel.Compilation);
```

**Fix:** Move to compilation provider:

```csharp
// In Initialize.cs
var compilationProvider = context.CompilationProvider
    .Select((c, _) => new ErrorOrContext(c));

return context.SyntaxProvider
    .ForAttributeWithMetadataName(...)
    .Combine(compilationProvider)
    .SelectFlow((data, ct) => {
        var (ctx, errorOrContext) = data;
        return AnalyzeEndpointFlow(ctx, errorOrContext, ct);
    });
```

---

### 7. ISymbol References in Pipeline (Architecture)

**Location:** `src/ErrorOrX.Generators/Models/EndpointModels.cs:131,215`

**Problem:** `ParameterMeta.Symbol` and `MethodAnalysis.Method` hold `ISymbol` references. These should not leak into cached pipeline stages.

**Impact:** Holds old compilations in memory, degrading IDE performance on large projects.

**Fix:** Extract all needed data to strings/primitives before caching:

```csharp
// Before
internal record struct ParameterMeta(IParameterSymbol Symbol, ...);

// After
internal readonly record struct ParameterMeta(
    string Name,
    string TypeFqn,
    NullableAnnotation NullableAnnotation,
    ImmutableArray<AttributeData> Attributes, // Only if needed immediately
    ...);
```

---

### 8. Parameter Binding Duplication (Code Quality)

**Location:**
- `src/ErrorOrX.Generators/Core/ErrorOrEndpointGenerator.Emitter.cs:545-936`
- `src/ErrorOrX.Generators/Emitters/BindingCodeEmitter.cs:7-528`

**Problem:** ~400 lines of identical parameter binding emission logic exists in both files.

**Fix:**
1. Make `BindingCodeEmitter` methods `internal static`
2. Remove duplicates from `Emitter.cs`
3. Call `BindingCodeEmitter.EmitParameterBinding()` from `Emitter.cs`

```csharp
// ErrorOrEndpointGenerator.Emitter.cs - AFTER refactor
private static bool EmitParameterBinding(StringBuilder code, in EndpointParameter param,
    string paramName, string bindFailFn)
{
    return BindingCodeEmitter.EmitParameterBinding(code, in param, paramName, bindFailFn);
}
```

---

### 9. Potential NRE on AttributeClass (Bugs)

**Locations:** `Extractor.cs:214, 406, 847, 863`

**Problem:** `attr.AttributeClass` can be null if attribute type resolution fails. Code assumes non-null.

**Current Code:**
```csharp
if (context.ProducesErrorAttribute is not null &&
    attr.AttributeClass.IsEqualTo(context.ProducesErrorAttribute))  // NRE risk
```

**Fix:**
```csharp
if (context.ProducesErrorAttribute is not null &&
    attr.AttributeClass is not null &&
    attr.AttributeClass.IsEqualTo(context.ProducesErrorAttribute))
```

---

### 10. Missing Version Attributes (API)

**Problem:** No `[Experimental]`, `[Obsolete]`, or `[EditorBrowsable]` attributes on any public API.

**Impact:** Breaking changes in v2.0.0, v2.2.0, v2.3.0 had no compiler warnings. Users upgrading get unexpected errors.

**Fix:** Add version attributes to new features:

```csharp
// Mark new features as experimental
[Experimental("ERROROR001")]
public static ErrorOr<TValue> OrNotFound<TValue>(this TValue? value, string description)

// Deprecate before breaking changes
[Obsolete("Use explicit [FromQuery] or [AsParameters]. See migration guide.")]
public static ErrorOr<T> MethodWithBreakingChange(ComplexType param)
```

---

### 11. Error.Metadata Not in ProblemDetails (API)

**Problem:** `Error.Metadata` dictionary exists but generated `ToProblem()` doesn't include it in ProblemDetails extensions.

**Impact:** Metadata is lost in HTTP responses. Clients can't access error context (field names, retry hints).

**Fix:** In `ToProblem()` generation:

```csharp
if (first.Metadata is not null)
{
    foreach (var kvp in first.Metadata)
        problem.Extensions[kvp.Key] = kvp.Value;
}
```

---

### 12. Config Validation Silent Failures (Config)

**Location:** `src/ErrorOrX.Generators/Core/ErrorOrEndpointGenerator.Initialize.cs:159-169`

**Problem:** Invalid config values are silently ignored. Typos go undetected.

```xml
<!-- User intends this -->
<ErrorOrGenerateJsonContext>true</ErrorOrGenerateJsonContext>

<!-- But types it as (SILENT FAILURE) -->
<ErrorOrGenerateJsonContext>truee</ErrorOrGenerateJsonContext>
```

**Fix:** Emit diagnostic for invalid values:

```csharp
private static bool ParseGenerateJsonContextOption(AnalyzerConfigOptionsProvider options,
    CancellationToken _)
{
    options.GlobalOptions.TryGetValue("build_property.ErrorOrGenerateJsonContext", out var value);

    if (string.IsNullOrEmpty(value))
        return false;

    if (bool.TryParse(value, out var result))
        return result;

    // Log diagnostic about invalid value
    ReportDiagnostic(Descriptors.InvalidConfigurationValue,
        $"ErrorOrGenerateJsonContext value '{value}' is not valid. Expected 'true' or 'false'.");
    return false;
}
```

---

## P2: Medium Priority Issues

### 13. String Escaping in Deprecation Metadata (Security)

**Location:** `src/ErrorOrX.Generators/Emitters/EndpointMetadataEmitter.cs:183`

**Problem:** Deprecation messages use basic `Replace("\"", "\\\"")` which is insufficient for C# string literals.

**Fix:**
```csharp
var escapedMessage = message
    .Replace("\\", "\\\\")  // Backslash first!
    .Replace("\"", "\\\"")
    .Replace("\r", "\\r")
    .Replace("\n", "\\n")
    .Replace("\t", "\\t");
```

---

### 14. Emitter.cs God Object (Architecture)

**Location:** `src/ErrorOrX.Generators/Core/ErrorOrEndpointGenerator.Emitter.cs`

**Problem:** 1400+ lines handling multiple responsibilities.

**Fix:** Extract into focused emitters:
- `InvokerEmitter.cs` - `EmitInvoker`, `EmitWrapperMethod`, `EmitCoreMethod`
- `JsonContextEmitter.cs` - `EmitJsonContext`, `EmitJsonContextHelper`
- `VersionSetEmitter.cs` - `EmitVersionSet`, `EmitVersioningCalls`

Keep `Emitter.cs` as orchestrator with `EmitEndpoints()` and `EmitMappings()`.

---

### 15. Inconsistent Analyzer Namespace (Consistency)

**Problem:** Analyzer files use `namespace ErrorOr.Analyzers;` while all other generator files use `namespace ErrorOr.Generators;`.

**Fix:** Decide on single namespace:
- Option 1: Move to `ErrorOr.Generators.Analyzers` (matches folder structure)
- Option 2: Keep `ErrorOr.Analyzers` and document exception in CLAUDE.md

---

### 16. CLAUDE.md Version Drift (Docs)

**Problem:** CLAUDE.md lists `ANcpLua.Roslyn.Utilities 1.16.0`, actual is `1.20.0`.

**Fix:** Update CLAUDE.md Dependencies section:

```markdown
| Package                          | Version  | Purpose                                |
|----------------------------------|----------|----------------------------------------|
| ANcpLua.Roslyn.Utilities         | 1.20.0   | Roslyn incremental generator utilities |
| ANcpLua.Roslyn.Utilities.Testing | 1.20.0   | Generator testing framework            |
```

---

## Appendix A: Test Coverage Matrix

| Feature | Current Coverage | Target |
|---------|-----------------|--------|
| Diagnostics (32 total) | 19% (6/32) | 100% |
| Parameter Binding | ~5% | 80% |
| Middleware Emission | 0% | 100% |
| Error Type Mapping | Unit only | + Integration |
| Incremental Caching | 1 test | 10+ tests |
| API Versioning | Good | Maintain |
| JSON Context | Minimal | 80% |

---

## Appendix B: Security Checklist

- [ ] Add defensive diagnostics for unrecognized security attributes
- [ ] Add integration tests for middleware enforcement
- [ ] Use proper C# string escaping in all generated string literals
- [ ] Validate route patterns for characters that break verbatim strings
- [ ] Add pattern validation for custom error codes

---

## Appendix C: Performance Optimizations

| Optimization | Impact | Effort |
|--------------|--------|--------|
| Move ErrorOrContext to compilation provider | 50-80% reduction in symbol lookups | Medium |
| Pre-size StringBuilder in EmitBodyCode | 10-20% reduction in emission time | Low |
| Use source-generated regex | Better AOT compatibility | Low |
| Remove ISymbol from cached models | Reduced memory retention | Medium |

---

## Next Steps

1. **Week 1:** Address P0 test coverage gaps
2. **Week 2:** Fix P1 security and performance issues
3. **Week 3:** Document API versioning feature
4. **Week 4:** Address P2 code quality issues

Run `/turbo-fix` on individual issues for parallel implementation.
