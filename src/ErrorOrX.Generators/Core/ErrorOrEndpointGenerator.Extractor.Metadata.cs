using Microsoft.CodeAnalysis;

namespace ErrorOr.Generators;

public sealed partial class ErrorOrEndpointGenerator
{
    /// <summary>
    ///     Extracts middleware configuration from BCL attributes on the method.
    ///     Detects [Authorize], [AllowAnonymous], [EnableRateLimiting], [DisableRateLimiting],
    ///     [OutputCache], [EnableCors], [DisableCors].
    /// </summary>
    private static MiddlewareInfo ExtractMiddlewareAttributes(ISymbol method)
    {
        var auth = default(AuthInfo);
        var rateLimit = default(RateLimitInfo);
        var cache = default(OutputCacheInfo);
        var cors = default(CorsInfo);

        foreach (var attr in method.GetAttributes())
        {
            if (attr.AttributeClass is not { } attrClass) continue;

            auth = TryExtractAuth(attr, attrClass, auth);
            rateLimit = TryExtractRateLimit(attr, attrClass, rateLimit);
            cache = TryExtractOutputCache(attr, attrClass, cache);
            cors = TryExtractCors(attr, attrClass, cors);
        }

        return new MiddlewareInfo(
            auth.Required, new EquatableArray<string>(auth.Policies), auth.AllowAnonymous,
            rateLimit.Enabled, rateLimit.Policy, rateLimit.Disabled,
            cache.Enabled, cache.Policy, cache.Duration,
            cors.Enabled, cors.Policy, cors.Disabled);
    }

    private static AuthInfo TryExtractAuth(
        AttributeData attr, ISymbol attrClass, AuthInfo current)
    {
        if (ErrorOrContext.MatchesType(attrClass, WellKnownTypes.AuthorizeAttribute))
        {
            var policy = attr.ConstructorArguments is [{ Value: string p }] ? p : null;
            policy ??= attr.NamedArguments.FirstOrDefault(static a => a.Key == "Policy").Value.Value as string;

            // Accumulate policies rather than overwriting
            var policies = policy is not null
                ? current.Policies.IsDefault
                    ? [policy]
                    : current.Policies.Add(policy)
                : current.Policies;

            return current with
            {
                Required = true,
                Policies = policies
            };
        }

        if (ErrorOrContext.MatchesType(attrClass, WellKnownTypes.AllowAnonymousAttribute))
            return current with
            {
                AllowAnonymous = true
            };

        return current;
    }

    private static RateLimitInfo TryExtractRateLimit(
        AttributeData attr, ISymbol attrClass, RateLimitInfo current)
    {
        if (ErrorOrContext.MatchesType(attrClass, WellKnownTypes.EnableRateLimitingAttribute))
        {
            var policy = attr.ConstructorArguments is [{ Value: string p }] ? p : null;
            return current with
            {
                Enabled = true,
                Policy = policy
            };
        }

        if (ErrorOrContext.MatchesType(attrClass, WellKnownTypes.DisableRateLimitingAttribute))
            return current with
            {
                Disabled = true
            };

        return current;
    }

    private static OutputCacheInfo TryExtractOutputCache(
        AttributeData attr, ISymbol attrClass, OutputCacheInfo current)
    {
        if (!ErrorOrContext.MatchesType(attrClass, WellKnownTypes.OutputCacheAttribute))
            return current;

        var result = current with
        {
            Enabled = true
        };
        foreach (var namedArg in attr.NamedArguments)
        {
            if (namedArg is { Key: "PolicyName", Value.Value: string policy })
                result = result with
                {
                    Policy = policy
                };

            if (namedArg is { Key: "Duration", Value.Value: int duration })
                result = result with
                {
                    Duration = duration
                };
        }

        return result;
    }

    private static CorsInfo TryExtractCors(
        AttributeData attr, ISymbol attrClass, CorsInfo current)
    {
        if (ErrorOrContext.MatchesType(attrClass, WellKnownTypes.EnableCorsAttribute))
        {
            var policy = attr.ConstructorArguments is [{ Value: string p }] ? p : null;
            return current with
            {
                Enabled = true,
                Policy = policy
            };
        }

        if (ErrorOrContext.MatchesType(attrClass, WellKnownTypes.DisableCorsAttribute))
            return current with
            {
                Disabled = true
            };

        return current;
    }

    /// <summary>
    ///     Extracts API versioning configuration from the method and its containing type.
    ///     Looks for [ApiVersion], [MapToApiVersion], and [ApiVersionNeutral] attributes.
    /// </summary>
    private static VersioningInfo ExtractVersioningAttributes(ISymbol method, ErrorOrContext context)
    {
        // If Asp.Versioning is not referenced, return empty
        if (!context.HasApiVersioningSupport) return default;

        var supportedVersions = new List<ApiVersionInfo>();
        var mappedVersions = new List<ApiVersionInfo>();
        var isVersionNeutral = false;

        // Extract from containing type first (class-level versioning)
        if (method.ContainingType is { } containingType)
            ExtractVersioningFromSymbol(containingType, supportedVersions, ref isVersionNeutral);

        // Extract from method (method-level overrides or additions)
        ExtractVersioningFromSymbol(method, supportedVersions, ref isVersionNeutral);

        // Extract [MapToApiVersion] separately (only applies to method)
        ExtractMappedVersions(method, mappedVersions);

        return new VersioningInfo(
            supportedVersions.Count > 0
                ? new EquatableArray<ApiVersionInfo>([.. supportedVersions.Distinct()])
                : default,
            mappedVersions.Count > 0
                ? new EquatableArray<ApiVersionInfo>([.. mappedVersions.Distinct()])
                : default,
            isVersionNeutral);
    }

    private static void ExtractVersioningFromSymbol(
        ISymbol symbol,
        List<ApiVersionInfo> supportedVersions,
        ref bool isVersionNeutral)
    {
        foreach (var attr in symbol.GetAttributes())
        {
            if (attr.AttributeClass is not { } attrClass) continue;

            // Check for [ApiVersionNeutral]
            if (ErrorOrContext.MatchesType(attrClass, WellKnownTypes.ApiVersionNeutralAttribute))
            {
                isVersionNeutral = true;
                continue;
            }

            // Check for [ApiVersion(...)]
            if (ErrorOrContext.MatchesType(attrClass, WellKnownTypes.ApiVersionAttribute))
            {
                var versionInfo = ParseApiVersionAttribute(attr);
                if (versionInfo.HasValue) supportedVersions.Add(versionInfo.Value);
            }
        }
    }

    private static void ExtractMappedVersions(
        ISymbol method,
        List<ApiVersionInfo> mappedVersions)
    {
        foreach (var attr in method.GetAttributes())
        {
            if (attr.AttributeClass is not { } attrClass) continue;

            // Check for [MapToApiVersion(...)]
            if (ErrorOrContext.MatchesType(attrClass, WellKnownTypes.MapToApiVersionAttribute))
            {
                var versionInfo = ParseApiVersionAttribute(attr);
                if (versionInfo.HasValue) mappedVersions.Add(versionInfo.Value);
            }
        }
    }

    /// <summary>
    ///     Parses version info from [ApiVersion] or [MapToApiVersion] attribute.
    ///     Supports multiple constructor overloads:
    ///     - ApiVersion(string version) - e.g., "1.0", "2", "1.0-beta"
    ///     - ApiVersion(int majorVersion, int minorVersion)
    ///     - ApiVersion(double version) - e.g., 1.0
    /// </summary>
    private static ApiVersionInfo? ParseApiVersionAttribute(AttributeData attr)
    {
        var args = attr.ConstructorArguments;

        if (args.Length is 0) return null;

        // Check for Deprecated named argument
        var isDeprecated = attr.NamedArguments
            .Any(static na => na is
            {
                Key: "Deprecated",
                Value.Value: true
            });

        switch (args)
        {
            // ApiVersion(string version) - most common
            case [{ Value: string versionString }]:
                return ParseVersionString(versionString, isDeprecated);
            // ApiVersion(int majorVersion, int minorVersion)
            case [{ Value: int major }, { Value: int minor }]:
                return new ApiVersionInfo(major, minor, null, isDeprecated);
            // ApiVersion(double version) - e.g., 1.0
            case [{ Value: double doubleVersion }]:
            {
                var majorPart = (int)doubleVersion;
                var minorPart = (int)((doubleVersion - majorPart) * 10);
                return new ApiVersionInfo(majorPart, minorPart > 0 ? minorPart : null, null, isDeprecated);
            }
            default:
                return null;
        }
    }

    /// <summary>
    ///     Parses a version string like "1.0", "2", "1.0-beta" into ApiVersionInfo.
    /// </summary>
    private static ApiVersionInfo? ParseVersionString(string versionString, bool isDeprecated)
    {
        if (string.IsNullOrWhiteSpace(versionString)) return null;

        // Handle status suffix (e.g., "1.0-beta")
        string? status = null;
        var dashIndex = versionString.IndexOf('-');
        if (dashIndex > 0)
        {
            status = versionString[(dashIndex + 1)..];
            versionString = versionString[..dashIndex];
        }

        // Parse major.minor
        var parts = versionString.Split('.');

        if (!int.TryParse(parts[0], out var major)) return null;

        int? minor = null;
        if (parts.Length > 1 && int.TryParse(parts[1], out var minorValue)) minor = minorValue;

        return new ApiVersionInfo(major, minor, status, isDeprecated);
    }

    /// <summary>
    ///     Extracts raw version strings from [ApiVersion] attributes on the containing type.
    ///     Used for format validation (EOE031).
    /// </summary>
    private static ImmutableArray<string> ExtractRawClassVersionStrings(ISymbol method, ErrorOrContext context)
    {
        if (!context.HasApiVersioningSupport || method.ContainingType is not { } containingType)
            return [];

        var versions = ImmutableArray.CreateBuilder<string>();

        foreach (var attr in containingType.GetAttributes())
        {
            if (attr.AttributeClass is not { } attrClass) continue;

            if (ErrorOrContext.MatchesType(attrClass, WellKnownTypes.ApiVersionAttribute) &&
                attr.ConstructorArguments is [{ Value: string versionString }])
                versions.Add(versionString);
        }

        return versions.ToImmutable();
    }

    /// <summary>
    ///     Extracts raw version strings from [MapToApiVersion] attributes on the method.
    ///     Used for format validation (EOE031).
    /// </summary>
    private static ImmutableArray<string> ExtractRawMethodVersionStrings(ISymbol method, ErrorOrContext context)
    {
        if (!context.HasApiVersioningSupport) return ImmutableArray<string>.Empty;

        var versions = ImmutableArray.CreateBuilder<string>();

        foreach (var attr in method.GetAttributes())
        {
            if (attr.AttributeClass is not { } attrClass) continue;

            if (ErrorOrContext.MatchesType(attrClass, WellKnownTypes.MapToApiVersionAttribute) &&
                attr.ConstructorArguments is [{ Value: string versionString }])
                versions.Add(versionString);
        }

        return versions.ToImmutable();
    }

    /// <summary>
    ///     Extracts route group configuration from the containing type's [RouteGroup] attribute.
    ///     This enables eShop-style route grouping with NewVersionedApi() and MapGroup().
    /// </summary>
    private static RouteGroupInfo ExtractRouteGroupInfo(ISymbol method)
    {
        // RouteGroup is only applied at class level
        if (method.ContainingType is not { } containingType) return default;

        var attrs = containingType.GetAttributes();
        foreach (var attr in attrs)
        {
            if (attr.AttributeClass is not { } attrClass) continue;

            if (!ErrorOrContext.MatchesType(attrClass, WellKnownTypes.RouteGroupAttribute)) continue;

            // [RouteGroup(string path)]
            // Optional: ApiName named argument
            var args = attr.ConstructorArguments;
            if (args is not [{ Value: string groupPath }]) continue;

            // Extract optional ApiName from named arguments
            string? apiName = null;
            foreach (var namedArg in attr.NamedArguments)
                if (namedArg is
                    {
                        Key: "ApiName",
                        Value.Value: string name
                    })
                    apiName = name;

            return new RouteGroupInfo(groupPath, apiName);
        }

        return default;
    }

    /// <summary>
    ///     Extracts metadata from [EndpointMetadata] attributes and [Obsolete] attribute.
    /// </summary>
    private static EquatableArray<MetadataEntry> ExtractMetadata(ISymbol method)
    {
        var metadata = ImmutableArray.CreateBuilder<MetadataEntry>();

        foreach (var attr in method.GetAttributes())
        {
            if (attr.AttributeClass is not { } attrClass) continue;

            switch (attrClass.Name)
            {
                // [Obsolete] → deprecated metadata
                case "ObsoleteAttribute":
                {
                    metadata.Add(new MetadataEntry(MetadataKeys.Deprecated, "true"));
                    if (attr.ConstructorArguments is [{ Value: string msg }, ..])
                        metadata.Add(new MetadataEntry(MetadataKeys.DeprecatedMessage, msg));

                    continue;
                }
                // [EndpointMetadata(key, value)]
                case "EndpointMetadataAttribute" when
                    attr.ConstructorArguments is [{ Value: string key }, { Value: string value }]:
                    metadata.Add(new MetadataEntry(key, value));
                    break;
            }
        }

        return metadata.Count > 0
            ? new EquatableArray<MetadataEntry>(metadata.ToImmutable())
            : default;
    }

    // Helper records for middleware extraction
    private readonly record struct AuthInfo(bool Required, ImmutableArray<string> Policies, bool AllowAnonymous);

    private readonly record struct RateLimitInfo(bool Enabled, string? Policy, bool Disabled);

    private readonly record struct OutputCacheInfo(bool Enabled, string? Policy, int? Duration);

    private readonly record struct CorsInfo(bool Enabled, string? Policy, bool Disabled);
}
