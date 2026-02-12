using Asp.Versioning;

namespace DiagnosticsDemos.Demos;

// -------------------------------------------------------------------------
// TRIGGERS EOE031: Invalid version format with "v" prefix
// -------------------------------------------------------------------------
// Uncomment to see the diagnostic:
//
// [ApiVersion("v1")]
// public static class VPrefixApi
// {
//     [Get("/items")]
//     public static ErrorOr<string> GetItems() => "items";
// }

// -------------------------------------------------------------------------
// TRIGGERS EOE031: Invalid semver format (three parts)
// -------------------------------------------------------------------------
// Uncomment to see the diagnostic:
//
// [ApiVersion("1.0.0")]
// public static class SemverApi
// {
//     [Get("/items")]
//     public static ErrorOr<string> GetItems() => "items";
// }

// -------------------------------------------------------------------------
// FIXED: Valid version formats
// -------------------------------------------------------------------------
/// <summary>
/// EOE031: Invalid API version format — [ApiVersion] has invalid format; use "major.minor" or just "major".
/// </summary>
/// <remarks>
/// Valid formats: "1.0", "2.0", "1.1" (major.minor), "1", "2", "3" (major only),
/// "1.0-beta", "2.0-alpha" (with status suffix).
/// Invalid formats: "v1" (v prefix not allowed), "1.0.0" (semver with patch not allowed).
/// </remarks>
[ApiVersion("1.0")] // Major.Minor
[ApiVersion("2.0")]
[ApiVersion("2.1")]
public static class EOE031_InvalidApiVersionFormat
{
    // NOTE: In production with API versioning, the framework handles routing
    // Here we use unique routes for demo to avoid EOE004 (duplicate route)

    [Get("/api/eoe031/v1/items")]
    [MapToApiVersion("1.0")]
    public static ErrorOr<string> GetItemsV1()
    {
        return "items v1.0";
    }

    [Get("/api/eoe031/v2/items")]
    [MapToApiVersion("2.0")]
    public static ErrorOr<string> GetItemsV2()
    {
        return "items v2.0";
    }

    [Get("/api/eoe031/v21/items")]
    [MapToApiVersion("2.1")]
    public static ErrorOr<string> GetItemsV21()
    {
        return "items v2.1";
    }
}

// -------------------------------------------------------------------------
// FIXED: Major-only versions
// -------------------------------------------------------------------------
[ApiVersion("1")]
[ApiVersion("2")]
[ApiVersion("3")]
public static class MajorOnlyVersionApi
{
    [Get("/api/eoe031/major/v1/items")]
    [MapToApiVersion("1")]
    public static ErrorOr<string> GetItemsV1()
    {
        return "items v1";
    }

    [Get("/api/eoe031/major/v2/items")]
    [MapToApiVersion("2")]
    public static ErrorOr<string> GetItemsV2()
    {
        return "items v2";
    }

    [Get("/api/eoe031/major/v3/items")]
    [MapToApiVersion("3")]
    public static ErrorOr<string> GetItemsV3()
    {
        return "items v3";
    }
}

// -------------------------------------------------------------------------
// FIXED: Versions with status suffix (preview, beta, alpha)
// -------------------------------------------------------------------------
[ApiVersion("1.0")]
[ApiVersion("2.0-beta")]
[ApiVersion("2.0-rc1")]
[ApiVersion("2.0")]
public static class PreviewVersionApi
{
    [Get("/api/eoe031/preview/stable/items")]
    [MapToApiVersion("1.0")]
    public static ErrorOr<string> GetItemsStable()
    {
        return "items stable";
    }

    [Get("/api/eoe031/preview/beta/items")]
    [MapToApiVersion("2.0-beta")]
    public static ErrorOr<string> GetItemsBeta()
    {
        return "items beta - new features";
    }

    [Get("/api/eoe031/preview/rc/items")]
    [MapToApiVersion("2.0-rc1")]
    public static ErrorOr<string> GetItemsRc()
    {
        return "items release candidate";
    }

    [Get("/api/eoe031/preview/v2/items")]
    [MapToApiVersion("2.0")]
    public static ErrorOr<string> GetItemsV2Stable()
    {
        return "items v2 stable";
    }
}

// -------------------------------------------------------------------------
// Valid version format reference:
// -------------------------------------------------------------------------
// "1"          -> Major only
// "1.0"        -> Major.Minor
// "1.1"        -> Major.Minor
// "2.0-alpha"  -> With status suffix
// "2.0-beta"   -> With status suffix
// "2.0-rc1"    -> With status suffix
//
// INVALID:
// "v1"         -> No 'v' prefix
// "1.0.0"      -> No patch version (semver)
// "version1"   -> Not numeric
