// EOE027: Version-neutral with mappings
// =======================================
// Endpoint is version-neutral but has explicit version mappings.
// [ApiVersionNeutral] and [MapToApiVersion] are mutually exclusive.
//
// [ApiVersionNeutral] means the endpoint works across all versions.
// [MapToApiVersion] means the endpoint is specific to certain versions.
// Using both is contradictory.

using Asp.Versioning;

namespace DiagnosticsDemos.Demos;

[ApiVersion("1.0")]
[ApiVersion("2.0")]
public static class EOE027_VersionNeutralWithMappings
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE027: Both [ApiVersionNeutral] and [MapToApiVersion]
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Get("/health")]
    // [ApiVersionNeutral]
    // [MapToApiVersion("1.0")]
    // public static ErrorOr<string> GetHealth() => "healthy";

    // -------------------------------------------------------------------------
    // FIXED: Use only [ApiVersionNeutral] for version-agnostic endpoints
    // -------------------------------------------------------------------------
    [Get("/api/eoe027/health")]
    [ApiVersionNeutral]
    public static ErrorOr<string> GetHealthNeutral() => "healthy";

    [Get("/api/eoe027/ping")]
    [ApiVersionNeutral]
    public static ErrorOr<string> Ping() => "pong";

    // -------------------------------------------------------------------------
    // FIXED: Use only [MapToApiVersion] for version-specific endpoints
    // -------------------------------------------------------------------------
    [Get("/api/eoe027/v1/items")]
    [MapToApiVersion("1.0")]
    public static ErrorOr<string> GetItemsV1() => "items v1";

    [Get("/api/eoe027/v2/items")]
    [MapToApiVersion("2.0")]
    public static ErrorOr<string> GetItemsV2() => "items v2 with new fields";

    // -------------------------------------------------------------------------
    // FIXED: Endpoint available in multiple versions
    // -------------------------------------------------------------------------
    [Get("/api/eoe027/shared")]
    [MapToApiVersion("1.0")]
    [MapToApiVersion("2.0")]
    public static ErrorOr<string> GetShared() => "shared across v1 and v2";
}

// -------------------------------------------------------------------------
// TIP: When to use each attribute
// -------------------------------------------------------------------------
//
// [ApiVersionNeutral]
// - Health checks, ping endpoints
// - Documentation endpoints
// - Endpoints that never change behavior
//
// [MapToApiVersion]
// - Version-specific behavior
// - Breaking changes between versions
// - Deprecated endpoints (only in old version)
