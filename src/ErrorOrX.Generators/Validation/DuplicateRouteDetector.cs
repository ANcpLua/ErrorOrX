using System.Collections.Immutable;
using System.Text.RegularExpressions;
using ErrorOr.Analyzers;
using Microsoft.CodeAnalysis;

namespace ErrorOr.Generators;

/// <summary>
///     Detects duplicate routes across all registered endpoints.
///     Runs during the final aggregation phase.
/// </summary>
internal static class DuplicateRouteDetector
{
    /// <summary>
    ///     Checks for duplicate routes.
    /// </summary>
    public static ImmutableArray<Diagnostic> Detect(ImmutableArray<EndpointDescriptor> endpoints)
    {
        if (endpoints.IsDefaultOrEmpty || endpoints.Length < 2)
            return ImmutableArray<Diagnostic>.Empty;

        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        DetectDuplicateRoutes(endpoints, diagnostics);

        return diagnostics.ToImmutable();
    }

    private static void DetectDuplicateRoutes(
        ImmutableArray<EndpointDescriptor> endpoints,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        // Key: normalized "METHOD /pattern"
        var routeMap = new Dictionary<string, EndpointDescriptor>(StringComparer.OrdinalIgnoreCase);

        foreach (var ep in endpoints)
        {
            var normalizedPattern = NormalizeRoutePattern(ep.Pattern);
            var key = $"{ep.HttpMethod.ToUpperInvariant()} {normalizedPattern}";

            if (routeMap.TryGetValue(key, out var existing))
                // Duplicate found
                diagnostics.Add(Diagnostic.Create(
                    Descriptors.DuplicateRoute,
                    Location.None, // Would need location info in EndpointDescriptor
                    ep.HttpMethod.ToUpperInvariant(),
                    ep.Pattern,
                    TypeNameHelper.ExtractShortName(existing.HandlerContainingTypeFqn),
                    existing.HandlerMethodName));
            else
                routeMap[key] = ep;
        }
    }

    /// <summary>
    ///     Normalizes route patterns for duplicate detection.
    ///     Replaces parameter names with placeholders since {id} and {userId} are structurally equivalent.
    /// </summary>
    private static string NormalizeRoutePattern(string pattern)
    {
        // Replace {anything} with {_} for comparison
        // This catches /users/{id} vs /users/{userId} as duplicates
        var normalized = Regex.Replace(
            pattern,
            @"\{[^}]+\}",
            "{_}");

        // Ensure leading slash
        if (!normalized.StartsWith("/"))
            normalized = "/" + normalized;

        // Remove trailing slash for consistency
        if (normalized.Length > 1 && normalized.EndsWith("/"))
            normalized = normalized[..^1];

        return normalized;
    }
}