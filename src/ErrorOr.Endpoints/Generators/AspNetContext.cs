using System.Collections.Immutable;
using System.Text.RegularExpressions;
using ANcpLua.Roslyn.Utilities;
using ErrorOr.Endpoints.Analyzers;
using Microsoft.CodeAnalysis;

namespace ErrorOr.Endpoints.Generators;

/// <summary>
///     Provides symbol lookups for common ASP.NET Core types.
///     Used by ErrorOrContext for parameter binding analysis.
/// </summary>
internal sealed class AspNetContext
{
    public AspNetContext(Compilation compilation)
    {
        FromBodyAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromBodyAttribute);
        FromServicesAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromServicesAttribute);
        FromRouteAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromRouteAttribute);
        FromQueryAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromQueryAttribute);
        FromHeaderAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromHeaderAttribute);
        FromFormAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromFormAttribute);
        FormFile = compilation.GetBestTypeByMetadataName(WellKnownTypes.FormFile);
        HttpContext = compilation.GetBestTypeByMetadataName(WellKnownTypes.HttpContext);
    }

    public INamedTypeSymbol? FromBodyAttribute { get; }
    public INamedTypeSymbol? FromServicesAttribute { get; }
    public INamedTypeSymbol? FromRouteAttribute { get; }
    public INamedTypeSymbol? FromQueryAttribute { get; }
    public INamedTypeSymbol? FromHeaderAttribute { get; }
    public INamedTypeSymbol? FromFormAttribute { get; }
    public INamedTypeSymbol? FormFile { get; }
    public INamedTypeSymbol? HttpContext { get; }

    public bool IsFormFile(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        if (FormFile is not null && type.IsOrImplements(FormFile))
            return true;

        return type.Name == "IFormFile" &&
               type.ContainingNamespace.ToDisplayString() == "Microsoft.AspNetCore.Http";
    }

    public bool IsHttpContextType(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        if (HttpContext is not null && type.IsOrInheritsFrom(HttpContext))
            return true;

        return type.Name == "HttpContext" &&
               type.ContainingNamespace.ToDisplayString() == "Microsoft.AspNetCore.Http";
    }
}

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
                    ExtractTypeName(existing.HandlerContainingTypeFqn),
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

    private static string ExtractTypeName(string fqn)
    {
        var lastDot = fqn.LastIndexOf('.');
        var name = lastDot >= 0 ? fqn[(lastDot + 1)..] : fqn;

        // Handle global:: prefix
        if (name.StartsWith("::"))
            name = name[2..];

        return name;
    }
}