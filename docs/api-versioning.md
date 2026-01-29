# API Versioning

ErrorOrX provides full support for ASP.NET Core API versioning through the `Asp.Versioning.Http` package.

## Quick Start

```csharp
using Asp.Versioning;
using ErrorOrX.Attributes;

[ApiVersion(1.0)]
[ApiVersion(2.0)]
public static class TodoApi
{
    [Get("/api/todos/{id}")]
    [MapToApiVersion(1.0)]
    public static ErrorOr<TodoV1> GetV1(int id) => new TodoV1(id, "Task");

    [Get("/api/todos/{id}")]
    [MapToApiVersion(2.0)]
    public static ErrorOr<TodoV2> GetV2(int id, TimeProvider time)
        => new TodoV2(id, "Task", time.GetUtcNow());
}
```

## Attributes

### `[ApiVersion]`

Declares which API versions an endpoint class or method supports.

```csharp
// Class-level: all endpoints support these versions
[ApiVersion(1.0)]
[ApiVersion(2.0)]
public static class MyApi { }

// Method-level: override class versions
[ApiVersion(3.0)]
[Get("/v3/resource")]
public static ErrorOr<Resource> GetV3() => ...
```

### `[MapToApiVersion]`

Maps a specific endpoint to a specific version. Required when multiple endpoints share the same route but serve different versions.

```csharp
[ApiVersion(1.0)]
[ApiVersion(2.0)]
public static class ProductApi
{
    // Both endpoints have the same route, distinguished by version
    [Get("/api/products")]
    [MapToApiVersion(1.0)]
    public static ErrorOr<List<ProductV1>> ListV1() => ...

    [Get("/api/products")]
    [MapToApiVersion(2.0)]
    public static ErrorOr<List<ProductV2>> ListV2() => ...
}
```

### `[ApiVersionNeutral]`

Marks an endpoint as version-neutral (available on all versions).

```csharp
[ApiVersionNeutral]
[Get("/health")]
public static ErrorOr<string> HealthCheck() => "OK";
```

## Version Formats

ErrorOrX supports multiple version formats:

```csharp
// Numeric versions
[ApiVersion(1.0)]
[ApiVersion(2.5)]

// String versions with status
[ApiVersion("1.0-beta")]
[ApiVersion("2.0-preview.1")]
```

## Generated Code

The generator emits version sets and applies them to endpoints:

```csharp
// Generated version set
private static readonly ApiVersionSet __vs1 = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .HasApiVersion(new ApiVersion(2, 0))
    .Build();

// Applied to endpoint
app.MapGet("/api/todos/{id}", Invoke_Ep1)
    .WithApiVersionSet(__vs1)
    .MapToApiVersion(new ApiVersion(1, 0));
```

## Service Registration

Register API versioning in your `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"),
        new QueryStringApiVersionReader("api-version"));
});

// Add ErrorOr endpoints
builder.Services.AddErrorOrEndpoints();

var app = builder.Build();
app.MapErrorOrEndpoints();
app.Run();
```

## Diagnostics

| ID | Severity | Description | Fix |
|----|----------|-------------|-----|
| EOE050 | Warning | Version-neutral endpoint has version mappings | Remove `[MapToApiVersion]` or remove `[ApiVersionNeutral]` |
| EOE051 | Warning | Mapped version not declared on class/method | Add `[ApiVersion]` for the mapped version |
| EOE052 | Warning | Asp.Versioning package not referenced | Add `<PackageReference Include="Asp.Versioning.Http" />` |
| EOE053 | Info | Endpoint missing versioning | Add `[ApiVersion]` or `[ApiVersionNeutral]` |
| EOE054 | Error | Invalid API version format | Use valid format: `1.0`, `"1.0-beta"` |

### EOE050: Version-Neutral with Mappings

```csharp
// Triggers EOE050
[ApiVersionNeutral]
[MapToApiVersion(1.0)]  // Conflict: neutral + specific mapping
[Get("/health")]
public static ErrorOr<string> Health() => "OK";

// Fixed
[ApiVersionNeutral]
[Get("/health")]
public static ErrorOr<string> Health() => "OK";
```

### EOE051: Mapped Version Not Declared

```csharp
// Triggers EOE051
[ApiVersion(1.0)]
[MapToApiVersion(2.0)]  // Version 2.0 not declared
[Get("/api/resource")]
public static ErrorOr<Resource> Get() => ...

// Fixed
[ApiVersion(1.0)]
[ApiVersion(2.0)]  // Declare version 2.0
[MapToApiVersion(2.0)]
[Get("/api/resource")]
public static ErrorOr<Resource> Get() => ...
```

### EOE052: Missing Package Reference

```xml
<!-- Add to your .csproj -->
<PackageReference Include="Asp.Versioning.Http" Version="8.1.0" />
```

### EOE054: Invalid Version Format

```csharp
// Triggers EOE054
[ApiVersion("v1")]        // Invalid: use numeric
[ApiVersion("1.0.0.0")]   // Invalid: too many segments
[ApiVersion("latest")]    // Invalid: not a version

// Valid formats
[ApiVersion(1.0)]
[ApiVersion("1.0")]
[ApiVersion("1.0-beta")]
```

## OpenAPI Integration

Versioned endpoints automatically include version metadata in OpenAPI:

```json
{
  "paths": {
    "/api/todos/{id}": {
      "get": {
        "tags": ["TodoApi"],
        "operationId": "TodoApi_GetV1",
        "x-api-version": "1.0"
      }
    }
  }
}
```

## Best Practices

1. **Declare versions at class level** when all endpoints share the same versions
2. **Use `[MapToApiVersion]`** only when multiple endpoints share the same route
3. **Use `[ApiVersionNeutral]`** for infrastructure endpoints (health, metrics)
4. **Avoid mixing** version-neutral and version-specific attributes
5. **Document breaking changes** when introducing new major versions
6. **Use `TimeProvider`** instead of banned datetime APIs for testable timestamps

## Example: Complete Versioned API

```csharp
using Asp.Versioning;
using ErrorOrX.Attributes;

[ApiVersion(1.0)]
[ApiVersion(2.0)]
public static class OrderApi
{
    // Available on v1 only
    [Get("/api/orders")]
    [MapToApiVersion(1.0)]
    public static ErrorOr<List<OrderV1>> ListV1(IOrderService svc)
        => svc.GetAllV1();

    // Available on v2 only (breaking change: pagination required)
    [Get("/api/orders")]
    [MapToApiVersion(2.0)]
    public static ErrorOr<PagedResult<OrderV2>> ListV2(
        IOrderService svc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
        => svc.GetPagedV2(page, pageSize);

    // Available on both versions
    [Get("/api/orders/{id}")]
    public static ErrorOr<Order> GetById(Guid id, IOrderService svc)
        => svc.GetById(id);
}

// Health check: version-neutral
[ApiVersionNeutral]
public static class HealthApi
{
    [Get("/health")]
    public static ErrorOr<HealthStatus> Check() => new HealthStatus("Healthy");
}
```
