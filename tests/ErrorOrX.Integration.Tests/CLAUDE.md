# ErrorOrX.Integration.Tests

Integration tests that verify ErrorOrX-generated endpoints behave identically to native ASP.NET Core Minimal APIs at runtime.

## Purpose

These tests ensure **behavioral parity** between:
- ErrorOrX-generated endpoints (`MapErrorOrEndpoints()`)
- Native ASP.NET Core Minimal API endpoints

This catches regressions in the generator's emitted code that unit tests (which verify generated source text) cannot detect.

## Test Infrastructure

| File | Purpose |
|------|---------|
| `IntegrationTestAppFactory.cs` | `WebApplicationFactory<T>` that creates test server |
| `IntegrationTestApp.cs` | Test app configuration and endpoint definitions |
| `IntegrationTestBase.cs` | Base class with `HttpClient` setup via `IClassFixture` |

### Test App Setup

```csharp
// IntegrationTestAppFactory creates the host
var appBuilder = WebApplication.CreateBuilder();
appBuilder.WebHost.UseTestServer();
IntegrationTestApp.ConfigureServices(appBuilder.Services);  // Auth, ErrorOrEndpoints
var app = appBuilder.Build();
IntegrationTestApp.Configure(app);  // MapErrorOrEndpoints(), middleware
app.Start();
```

## Test Categories

### Parameter Binding Parity

| Test | Verifies |
|------|----------|
| `Missing_Required_Query_Returns_400` | Required query parameter missing → 400 |
| `Query_Parse_Failure_Returns_400` | Query parse failure (e.g., `?id=bad`) → 400 |
| `Route_Mismatch_Returns_404` | Non-existent route → 404 |

### Content-Type Handling

| Test | Verifies |
|------|----------|
| `Body_Wrong_ContentType_Returns_415` | JSON body with wrong Content-Type → 415 |
| `Form_Wrong_ContentType_Returns_415` | Form binding with wrong Content-Type → 415 |

### Success Responses

| Test | Verifies |
|------|----------|
| `Returning_T_From_GET_Returns_200` | `ErrorOr<T>` success on GET → 200 |
| `Returning_T_From_POST_Returns_200` | `ErrorOr<T>` success on POST → 200 |
| `Explicit_Created_TypedResult_Returns_201` | `ErrorOr<Created>` → 201 |

### Middleware Integration

| Test | Verifies |
|------|----------|
| `Cookie_Auth_Known_API_Endpoint_Returns_401_No_Redirect` | `[Authorize]` on API returns 401 (not redirect) |

## Test Endpoints

Defined in `IntegrationTestApp.cs`:

```csharp
public static class TestEndpoints
{
    [Get("/parity/query-required")]
    public static ErrorOr<string> QueryRequired(string q) => q;

    [Post("/parity/body-json")]
    public static ErrorOr<string> BodyJson(TestDto dto) => dto.Name;

    [Get("/parity/query-int")]
    public static ErrorOr<int> QueryInt(int id) => id;

    [Post("/parity/form")]
    public static ErrorOr<string> FormValue([FromForm] string name) => name;

    [Get("/parity/return-t")]
    public static ErrorOr<string> ReturnT() => "success";

    [Post("/parity/return-t-post")]
    public static ErrorOr<string> ReturnTPost() => "success";

    [Post("/parity/created")]
    public static ErrorOr<Created> ReturnCreated() => Result.Created;

    [Get("/parity/auth/protected")]
    [Authorize]
    public static ErrorOr<string> Protected() => "secure";
}
```

## Configuration

```xml
<PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <!-- Emit generated files for inspection -->
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <!-- Use test app's own JSON context (none in this case) -->
    <ErrorOrGenerateJsonContext>false</ErrorOrGenerateJsonContext>
</PropertyGroup>
```

## Running Tests

```bash
# Run integration tests only
dotnet test --project tests/ErrorOrX.Integration.Tests/ErrorOrX.Integration.Tests.csproj

# Run with verbose output
dotnet test --project tests/ErrorOrX.Integration.Tests/ErrorOrX.Integration.Tests.csproj -v normal
```

## Inspecting Generated Code

Generated files are emitted to:
```
tests/ErrorOrX.Integration.Tests/obj/GeneratedFiles/ErrorOrX.Generators/
├── ErrorOr.Generators.ErrorOrEndpointGenerator/
│   ├── ErrorOrEndpointMappings.cs      # MapGet/MapPost calls
│   ├── ErrorOrEndpointOptions.g.cs     # Fluent config
│   └── ErrorOrEndpoints.GlobalUsings.g.cs
└── ErrorOr.Generators.OpenApiTransformerGenerator/
    └── OpenApiTransformers.g.cs
```

## Adding New Tests

1. **Add endpoint** to `TestEndpoints` in `IntegrationTestApp.cs`
2. **Add test method** to `MinimalApiParityTests.cs`
3. **Verify parity** - ensure the behavior matches native Minimal API

### Test Pattern

```csharp
[Fact]
public async Task Descriptive_Test_Name()
{
    var ct = TestContext.Current.CancellationToken;

    var response = await Client.GetAsync("/parity/your-endpoint", ct);

    response.StatusCode.Should().Be(HttpStatusCode.ExpectedCode);
    // Additional assertions...
}
```

## Current Coverage Gaps

| Gap | Priority |
|-----|----------|
| Error type → status code mapping | High |
| ValidationProblem response format | High |
| Rate limiting middleware | Medium |
| Output caching middleware | Medium |
| CORS middleware | Low |
| Location header on Created | Medium |

## Dependencies

- `xunit.v3.mtp-v2` - xUnit v3 with Microsoft Testing Platform
- `AwesomeAssertions` - Fluent assertions
- `Microsoft.AspNetCore.Mvc.Testing` - `WebApplicationFactory` infrastructure
- `Microsoft.AspNetCore.OpenApi` - OpenAPI support (for future OpenAPI tests)
