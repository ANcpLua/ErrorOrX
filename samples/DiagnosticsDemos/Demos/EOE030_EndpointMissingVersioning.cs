// EOE030: Endpoint missing versioning
// =====================================
// Endpoint has no [ApiVersion] attribute but other endpoints in the project use versioning.
// Consider adding version information or marking as [ApiVersionNeutral].
//
// This is an informational diagnostic to ensure consistency across your API.

using Asp.Versioning;

namespace DiagnosticsDemos.Demos.Eoe030;

// -------------------------------------------------------------------------
// This class HAS versioning
// -------------------------------------------------------------------------
[ApiVersion("1.0")]
[ApiVersion("2.0")]
public static class VersionedApi
{
    [Get("/api/eoe030/versioned/v1/items")]
    [MapToApiVersion("1.0")]
    public static ErrorOr<string> GetItemsV1()
    {
        return "versioned items v1";
    }

    [Get("/api/eoe030/versioned/v2/items")]
    [MapToApiVersion("2.0")]
    public static ErrorOr<string> GetItemsV2()
    {
        return "versioned items v2";
    }
}

// -------------------------------------------------------------------------
// TRIGGERS EOE030: This class has NO versioning while others do
// -------------------------------------------------------------------------
// Uncomment to see the diagnostic (info):
//
// public static class UnversionedApi
// {
//     [Get("/unversioned")]
//     public static ErrorOr<string> GetUnversioned() => "unversioned";
// }

// -------------------------------------------------------------------------
// FIXED: Add [ApiVersionNeutral] for version-agnostic endpoints
// -------------------------------------------------------------------------
public static class NeutralApi
{
    [Get("/api/eoe030/health")]
    [ApiVersionNeutral]
    public static ErrorOr<string> GetHealth()
    {
        return "healthy";
    }

    [Get("/api/eoe030/ping")]
    [ApiVersionNeutral]
    public static ErrorOr<string> Ping()
    {
        return "pong";
    }
}

// -------------------------------------------------------------------------
// FIXED: Add [ApiVersion] to include in versioning scheme
// -------------------------------------------------------------------------
[ApiVersion("1.0")]
[ApiVersion("2.0")]
public static class EOE030_EndpointMissingVersioning
{
    [Get("/api/eoe030/data")]
    [MapToApiVersion("1.0")]
    [MapToApiVersion("2.0")]
    public static ErrorOr<string> GetData()
    {
        return "data";
    }
}

// -------------------------------------------------------------------------
// TIP: Versioning strategy options
// -------------------------------------------------------------------------
//
// 1. All endpoints versioned:
//    - Every endpoint class has [ApiVersion]
//    - Clear version boundaries
//
// 2. Mixed with neutral endpoints:
//    - Core endpoints: [ApiVersion("1.0")] etc.
//    - Utility endpoints: [ApiVersionNeutral]
//
// 3. EOE030 helps catch forgotten endpoints:
//    - If you have versioned endpoints, unversioned ones trigger info
//    - Ensures intentional versioning decisions
