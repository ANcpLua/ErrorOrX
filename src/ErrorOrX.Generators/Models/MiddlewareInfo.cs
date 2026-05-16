namespace ErrorOr.Generators;

/// <summary>
///     Middleware configuration extracted from BCL attributes.
/// </summary>
internal readonly record struct MiddlewareInfo(
    bool RequiresAuthorization,
    EquatableArray<string> AuthorizationPolicies,
    bool AllowAnonymous,
    bool EnableRateLimiting,
    string? RateLimitingPolicy,
    bool DisableRateLimiting,
    bool EnableOutputCache,
    string? OutputCachePolicy,
    int? OutputCacheDuration,
    bool EnableCors,
    string? CorsPolicy,
    bool DisableCors)
{
    public bool HasAny =>
        RequiresAuthorization || AllowAnonymous ||
        EnableRateLimiting || DisableRateLimiting ||
        EnableOutputCache ||
        EnableCors || DisableCors;
}
