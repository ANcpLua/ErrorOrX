using Asp.Versioning;

namespace DiagnosticsDemos.Demos;

// -------------------------------------------------------------------------
// TRIGGERS EOE028: Class declares 1.0 but endpoint maps to 2.0
// -------------------------------------------------------------------------
// Uncomment to see the diagnostic (warning):
//
// [ApiVersion("1.0")]
// public static class UndeclaredVersionApi
// {
//     [Get("/items")]
//     [MapToApiVersion("2.0")]  // 2.0 not declared!
//     public static ErrorOr<string> GetItems() => "items";
// }

// -------------------------------------------------------------------------
// FIXED: Declare all versions used in [MapToApiVersion]
// -------------------------------------------------------------------------
/// <summary>
/// EOE028: Mapped version not declared — Endpoint has [MapToApiVersion] for a version not declared with [ApiVersion].
/// </summary>
/// <remarks>
/// If you map to version "2.0" but only declare [ApiVersion("1.0")] on the class,
/// the endpoint won't be reachable in version 2.0.
/// </remarks>
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[ApiVersion("3.0")]
public static class EOE028_MappedVersionNotDeclared
{
    // All mapped versions are declared on the class
    // NOTE: In production, you'd use URL versioning (/api/v1/items, /api/v2/items)
    // or query string versioning (?api-version=1.0)
    // Here we use unique routes for demo purposes to avoid EOE004 (duplicate route)

    [Get("/api/eoe028/v1/items")]
    [MapToApiVersion("1.0")]
    public static ErrorOr<string> GetItemsV1()
    {
        return "items v1";
    }

    [Get("/api/eoe028/v2/items")]
    [MapToApiVersion("2.0")]
    public static ErrorOr<string> GetItemsV2()
    {
        return "items v2";
    }

    [Get("/api/eoe028/v3/items")]
    [MapToApiVersion("3.0")]
    public static ErrorOr<string> GetItemsV3()
    {
        return "items v3";
    }

    // -------------------------------------------------------------------------
    // FIXED: Multiple versions on single endpoint (both v1 and v2 use same route)
    // -------------------------------------------------------------------------
    [Get("/api/eoe028/legacy")]
    [MapToApiVersion("1.0")]
    [MapToApiVersion("2.0")]
    public static ErrorOr<string> GetLegacy()
    {
        return "legacy - works in v1 and v2";
    }

    [Get("/api/eoe028/modern")]
    [MapToApiVersion("3.0")]
    public static ErrorOr<string> GetModern()
    {
        return "modern - only in v3";
    }
}

// -------------------------------------------------------------------------
// TIP: Organizing versioned APIs
// -------------------------------------------------------------------------
//
// Option 1: Single class with all versions
// [ApiVersion("1.0")]
// [ApiVersion("2.0")]
// public static class ItemsApi { ... }
//
// Option 2: Separate classes per version
// [ApiVersion("1.0")]
// public static class ItemsApiV1 { ... }
//
// [ApiVersion("2.0")]
// public static class ItemsApiV2 { ... }
