# ErrorOrX.Integration.Tests

Integration tests verifying ErrorOrX-generated endpoints behave identically to native ASP.NET Core Minimal APIs. Target: `net10.0`.

## Purpose

These tests ensure **behavioral parity** between:
- ErrorOrX-generated endpoints (`MapErrorOrEndpoints()`)
- Native ASP.NET Core Minimal API endpoints

This catches regressions that unit/snapshot tests cannot detect.

## Running Tests

```bash
dotnet test --project tests/ErrorOrX.Integration.Tests
dotnet test --project tests/ErrorOrX.Integration.Tests -v normal
```

## Test Infrastructure

| File | Purpose |
|------|---------|
| `IntegrationTestAppFactory.cs` | `WebApplicationFactory<T>` setup |
| `IntegrationTestApp.cs` | Test app configuration and endpoints |
| `IntegrationTestBase.cs` | Base class with `HttpClient` via `IClassFixture` |

## Test Categories

### Parameter Binding Parity

| Test | Verifies |
|------|----------|
| Missing required query | Required query missing -> 400 |
| Query parse failure | `?id=bad` -> 400 |
| Route mismatch | Non-existent route -> 404 |

### Content-Type Handling

| Test | Verifies |
|------|----------|
| Body wrong Content-Type | JSON body wrong type -> 415 |
| Form wrong Content-Type | Form binding wrong type -> 415 |

### Success Responses

| Test | Verifies |
|------|----------|
| GET returns T | `ErrorOr<T>` success -> 200 |
| POST returns T | `ErrorOr<T>` success -> 200 |
| Created marker | `ErrorOr<Created>` -> 201 |

### Middleware Integration

| Test | Verifies |
|------|----------|
| Cookie auth API endpoint | `[Authorize]` returns 401 (not redirect) |

## Test Endpoints

Defined in `IntegrationTestApp.cs`:

```csharp
public static class TestEndpoints
{
    [Get("/parity/query-required")]
    public static ErrorOr<string> QueryRequired(string q) => q;

    [Post("/parity/body-json")]
    public static ErrorOr<string> BodyJson(TestDto dto) => dto.Name;

    [Get("/parity/auth/protected")]
    [Authorize]
    public static ErrorOr<string> Protected() => "secure";
}
```

## Adding New Tests

1. Add endpoint to `TestEndpoints` in `IntegrationTestApp.cs`
2. Add test method to `MinimalApiParityTests.cs`
3. Verify behavior matches native Minimal API

```csharp
[Fact]
public async Task Descriptive_Test_Name()
{
    var ct = TestContext.Current.CancellationToken;
    var response = await Client.GetAsync("/parity/your-endpoint", ct);
    response.StatusCode.Should().Be(HttpStatusCode.ExpectedCode);
}
```

## Dependencies

| Package | Purpose |
|---------|---------|
| Microsoft.AspNetCore.Mvc.Testing | WebApplicationFactory |
| xunit.v3.mtp-v2 | xUnit v3 with MTP |
| AwesomeAssertions | Fluent assertions |
