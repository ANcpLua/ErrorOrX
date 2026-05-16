namespace ErrorOr.Generators;

/// <summary>
///     Represents a single API version extracted from [ApiVersion] attribute.
/// </summary>
internal readonly record struct ApiVersionInfo(
    int MajorVersion,
    int? MinorVersion,
    string? Status,
    bool IsDeprecated);

/// <summary>
///     API versioning configuration extracted from endpoint class or method.
/// </summary>
internal readonly record struct VersioningInfo(
    EquatableArray<ApiVersionInfo> SupportedVersions,
    EquatableArray<ApiVersionInfo> MappedVersions,
    bool IsVersionNeutral)
{
    /// <summary>
    ///     Returns true if any versioning attributes were found.
    /// </summary>
    public bool HasVersioning => !SupportedVersions.IsDefaultOrEmpty || IsVersionNeutral;

    /// <summary>
    ///     Returns the versions this endpoint should be mapped to.
    ///     Uses MappedVersions if specified, otherwise falls back to SupportedVersions.
    /// </summary>
    public EquatableArray<ApiVersionInfo> EffectiveVersions =>
        MappedVersions.IsDefaultOrEmpty ? SupportedVersions : MappedVersions;
}
