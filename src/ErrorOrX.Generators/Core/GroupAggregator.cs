using System.Collections.Immutable;
using ANcpLua.Roslyn.Utilities;

namespace ErrorOr.Generators;

/// <summary>
///     Aggregates endpoints by route group for eShop-style versioned API grouping.
///     Groups endpoints that share a [RouteGroup] attribute for optimized emission.
/// </summary>
internal static class GroupAggregator
{
    /// <summary>
    ///     Groups endpoints by their [RouteGroup] attribute.
    ///     Endpoints without a route group are returned in UngroupedEndpoints.
    ///     Each endpoint retains its original index for invoker naming.
    /// </summary>
    public static GroupingResult GroupEndpoints(ImmutableArray<EndpointDescriptor> endpoints)
    {
        // Create indexed endpoints with original positions
        var indexed = endpoints
            .Select(static (ep, i) => new IndexedEndpoint(ep, i))
            .ToImmutableArray();

        // Partition into grouped and ungrouped
        var grouped = indexed
            .Where(static x => x.Endpoint.RouteGroup.HasRouteGroup)
            .GroupBy(
                static x => x.Endpoint.RouteGroup.GroupPath!,
                StringComparer.OrdinalIgnoreCase)
            .Select(static g => CreateAggregate(g.Key, g.ToImmutableArray()))
            .OrderBy(static g => g.GroupPath, StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();

        var ungrouped = indexed
            .Where(static x => !x.Endpoint.RouteGroup.HasRouteGroup)
            .ToImmutableArray();

        return new GroupingResult(grouped, ungrouped);
    }

    /// <summary>
    ///     Creates a route group aggregate from indexed endpoints.
    ///     Computes the union of all versions across endpoints in the group.
    ///     API name is taken from the first endpoint's RouteGroup (should be consistent within group).
    /// </summary>
    private static RouteGroupAggregate CreateAggregate(string groupPath, ImmutableArray<IndexedEndpoint> endpoints)
    {
        // Get API name from first endpoint (assumed consistent within group)
        var apiName = endpoints.FirstOrDefault().Endpoint.RouteGroup.ApiName;

        // Check for version-neutral endpoints
        var hasVersionNeutral = endpoints.Any(static x => x.Endpoint.Versioning.IsVersionNeutral);

        // Compute union of all versions, sorted by major.minor
        var sortedVersions = endpoints
            .SelectMany(static x => x.Endpoint.Versioning.SupportedVersions.AsImmutableArray())
            .Distinct()
            .OrderBy(static v => v.MajorVersion)
            .ThenBy(static v => v.MinorVersion ?? 0)
            .ToImmutableArray();

        return new RouteGroupAggregate(
            groupPath,
            apiName,
            new EquatableArray<ApiVersionInfo>(sortedVersions),
            hasVersionNeutral,
            endpoints);
    }

    /// <summary>
    ///     Determines the effective route pattern for an endpoint within a group.
    ///     If the endpoint pattern starts with the group path, returns the relative portion.
    ///     Otherwise returns the full pattern as-is.
    /// </summary>
    public static string GetRelativePattern(in EndpointDescriptor ep)
    {
        if (!ep.RouteGroup.HasRouteGroup || ep.RouteGroup.GroupPath is not { } groupPath)
            return ep.Pattern;

        // Normalize paths for comparison (remove leading slashes)
        var normalizedGroup = groupPath.TrimStart('/');
        var normalizedPattern = ep.Pattern.TrimStart('/');

        // If pattern starts with group path, return the relative portion
        if (normalizedPattern.StartsWith(normalizedGroup, StringComparison.OrdinalIgnoreCase))
        {
            var relative = normalizedPattern[normalizedGroup.Length..].TrimStart('/');
            return string.IsNullOrEmpty(relative) ? "/" : "/" + relative;
        }

        // Pattern doesn't include group prefix (already relative)
        return ep.Pattern;
    }

    /// <summary>
    ///     An endpoint with its original index for invoker naming.
    /// </summary>
    internal readonly record struct IndexedEndpoint(EndpointDescriptor Endpoint, int OriginalIndex);

    /// <summary>
    ///     Represents a route group with its endpoints and computed metadata.
    /// </summary>
    internal readonly record struct RouteGroupAggregate(
        string GroupPath,
        string? ApiName,
        EquatableArray<ApiVersionInfo> Versions,
        bool HasVersionNeutral,
        ImmutableArray<IndexedEndpoint> Endpoints)
    {
        /// <summary>
        ///     Returns true if this group uses versioned API (NewVersionedApi pattern).
        /// </summary>
        public bool UseVersionedApi => !Versions.IsDefaultOrEmpty || HasVersionNeutral;
    }

    /// <summary>
    ///     Result of grouping endpoints by route group.
    /// </summary>
    internal readonly record struct GroupingResult(
        ImmutableArray<RouteGroupAggregate> Groups,
        ImmutableArray<IndexedEndpoint> UngroupedEndpoints)
    {
        /// <summary>
        ///     Returns true if any endpoints use route grouping.
        /// </summary>
        public bool HasGroups => !Groups.IsDefaultOrEmpty;
    }
}
