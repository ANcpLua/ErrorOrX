using System.Collections.Immutable;
using System.Text.RegularExpressions;
using ANcpLua.Roslyn.Utilities.Models;
using ErrorOr.Analyzers;
using Microsoft.CodeAnalysis;

namespace ErrorOr.Generators;

/// <summary>
///     Validates API versioning configuration for endpoints.
///     Reports EOE027-EOE031 diagnostics.
/// </summary>
internal static class ApiVersioningValidator
{
    // Valid formats: "1", "1.0", "2", "2.0-beta", "1.0-preview.1"
    // Invalid: "v1", "v2.0", "1.0.0" (semver), "version1", empty
    private static readonly Regex ValidVersionPattern = new(
        @"^(\d+)(?:\.(\d+))?(?:-[a-zA-Z0-9.]+)?$",
        RegexOptions.Compiled);

    // Versioning attribute names for EOE029 detection (without type resolution)
    private static readonly string[] VersioningAttributeNames =
    [
        "ApiVersionAttribute",
        "ApiVersionNeutralAttribute",
        "MapToApiVersionAttribute"
    ];

    /// <summary>
    ///     Validates API versioning configuration for an endpoint.
    ///     Returns diagnostics for any versioning issues found.
    /// </summary>
    /// <param name="methodName">The endpoint method name for diagnostic messages.</param>
    /// <param name="versioning">The extracted versioning info.</param>
    /// <param name="rawClassVersions">Raw version strings from class [ApiVersion] attributes.</param>
    /// <param name="rawMethodVersions">Raw version strings from method [MapToApiVersion] attributes.</param>
    /// <param name="location">Location for diagnostic reporting.</param>
    /// <param name="hasApiVersioningSupport">Whether the Asp.Versioning package is referenced.</param>
    /// <param name="method">The method symbol for additional attribute checks.</param>
    /// <returns>Collection of diagnostics found.</returns>
    public static ImmutableArray<DiagnosticInfo> Validate(
        string methodName,
        VersioningInfo versioning,
        ImmutableArray<string> rawClassVersions,
        ImmutableArray<string> rawMethodVersions,
        Location location,
        bool hasApiVersioningSupport = true,
        ISymbol? method = null)
    {
        var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

        // EOE029: Package not referenced but versioning attributes used
        if (!hasApiVersioningSupport && method is not null)
        {
            ValidatePackageReferenced(methodName, method, location, diagnostics);
        }

        // EOE027: Version-neutral with mappings
        ValidateVersionNeutralWithMappings(methodName, versioning, location, diagnostics);

        // EOE031: Invalid version format (check raw strings)
        ValidateVersionFormats(rawClassVersions, rawMethodVersions, location, diagnostics);

        // EOE028: Mapped version not declared
        ValidateMappedVersionsDeclared(methodName, versioning, location, diagnostics);

        // Note: EOE030 is handled in EmitMappingsAndRunAnalysis because it requires cross-file analysis

        return diagnostics.ToImmutable();
    }

    /// <summary>
    ///     EOE027: Endpoint has [ApiVersionNeutral] but also [MapToApiVersion].
    ///     These are mutually exclusive - neutral means "applies to all versions".
    /// </summary>
    private static void ValidateVersionNeutralWithMappings(
        string methodName,
        VersioningInfo versioning,
        Location location,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        if (versioning.IsVersionNeutral && !versioning.MappedVersions.IsDefaultOrEmpty)
        {
            diagnostics.Add(DiagnosticInfo.Create(
                Descriptors.VersionNeutralWithMappings,
                location,
                methodName));
        }
    }

    /// <summary>
    ///     EOE028: Endpoint maps to a version not declared in [ApiVersion] on the class.
    /// </summary>
    private static void ValidateMappedVersionsDeclared(
        string methodName,
        VersioningInfo versioning,
        Location location,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        if (versioning.MappedVersions.IsDefaultOrEmpty || versioning.SupportedVersions.IsDefaultOrEmpty)
            return;

        // Build set of declared versions for fast lookup
        var declaredVersions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var v in versioning.SupportedVersions.AsImmutableArray())
        {
            declaredVersions.Add(FormatVersion(v));
        }

        // Check each mapped version
        foreach (var mapped in versioning.MappedVersions.AsImmutableArray())
        {
            var mappedStr = FormatVersion(mapped);
            if (!declaredVersions.Contains(mappedStr))
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    Descriptors.MappedVersionNotDeclared,
                    location,
                    methodName,
                    mappedStr));
            }
        }
    }

    /// <summary>
    ///     EOE031: Invalid API version format.
    ///     Valid: "1", "1.0", "2.0-beta", "1.0-preview.1"
    ///     Invalid: "v1", "v2.0", "1.0.0" (semver), "version1"
    /// </summary>
    private static void ValidateVersionFormats(
        ImmutableArray<string> rawClassVersions,
        ImmutableArray<string> rawMethodVersions,
        Location location,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        var reported = new HashSet<string>(StringComparer.Ordinal);

        if (!rawClassVersions.IsDefaultOrEmpty)
        {
            foreach (var version in rawClassVersions)
            {
                if (!IsValidVersionFormat(version) && reported.Add(version))
                {
                    diagnostics.Add(DiagnosticInfo.Create(
                        Descriptors.InvalidApiVersionFormat,
                        location,
                        version));
                }
            }
        }

        if (!rawMethodVersions.IsDefaultOrEmpty)
        {
            foreach (var version in rawMethodVersions)
            {
                if (!IsValidVersionFormat(version) && reported.Add(version))
                {
                    diagnostics.Add(DiagnosticInfo.Create(
                        Descriptors.InvalidApiVersionFormat,
                        location,
                        version));
                }
            }
        }
    }

    /// <summary>
    ///     Validates if a version string has valid format.
    /// </summary>
    /// <remarks>
    ///     Valid formats per Asp.Versioning:
    ///     - "1" (major only)
    ///     - "1.0" (major.minor)
    ///     - "2.0-beta" (with status suffix)
    ///     - "1.0-preview.1" (with complex status)
    ///
    ///     Invalid formats:
    ///     - "v1", "v2.0" (v prefix)
    ///     - "1.0.0" (semver with patch)
    ///     - "version1" (text prefix)
    ///     - empty or whitespace
    /// </remarks>
    internal static bool IsValidVersionFormat(string? version)
    {
        // Null check with explicit pattern to satisfy nullability analysis
        if (version is not { Length: > 0 } v || string.IsNullOrWhiteSpace(v))
            return false;

        // Check for "v" prefix which is invalid
        if (v.Length > 1 &&
            (v[0] == 'v' || v[0] == 'V') &&
            char.IsDigit(v[1]))
        {
            return false;
        }

        // Check for semver format (3 parts like 1.0.0)
        var parts = v.Split('.');
        if (parts.Length > 2)
        {
            // Allow if third part is a status suffix (contains non-digits at start)
            // "1.0-beta" splits as ["1", "0-beta"] - 2 parts, valid
            // "1.0.0" splits as ["1", "0", "0"] - 3 parts, invalid
            // "1.0.0-beta" splits as ["1", "0", "0-beta"] - 3 parts, still invalid (semver)
            return false;
        }

        return ValidVersionPattern.IsMatch(v);
    }

    /// <summary>
    ///     EOE029: Detects versioning attribute usage when the Asp.Versioning package is not referenced.
    ///     Checks for attribute names by string matching since types can't be resolved without the package.
    /// </summary>
    private static void ValidatePackageReferenced(
        string methodName,
        ISymbol method,
        Location location,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        // Check method and containing type for versioning attributes by name
        if (HasVersioningAttributeByName(method) ||
            (method.ContainingType is { } containingType && HasVersioningAttributeByName(containingType)))
        {
            diagnostics.Add(DiagnosticInfo.Create(
                Descriptors.ApiVersioningPackageNotReferenced,
                location,
                methodName));
        }
    }

    /// <summary>
    ///     Checks if a symbol has any versioning-related attribute by checking attribute class names.
    ///     This works even when the Asp.Versioning package is not referenced.
    /// </summary>
    private static bool HasVersioningAttributeByName(ISymbol symbol)
    {
        foreach (var attr in symbol.GetAttributes())
        {
            if (attr.AttributeClass is not { } attrClass)
                continue;

            // Check by name since we can't resolve the type
            var attrName = attrClass.Name;
            foreach (var versioningAttrName in VersioningAttributeNames)
            {
                if (string.Equals(attrName, versioningAttrName, StringComparison.Ordinal))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Formats an ApiVersionInfo for comparison/display.
    /// </summary>
    private static string FormatVersion(ApiVersionInfo version)
    {
        var result = version.MinorVersion.HasValue
            ? $"{version.MajorVersion}.{version.MinorVersion}"
            : version.MajorVersion.ToString();

        if (!string.IsNullOrEmpty(version.Status))
            result += $"-{version.Status}";

        return result;
    }

    /// <summary>
    ///     Detects endpoints missing versioning in a project that uses API versioning.
    ///     Reports EOE030 for endpoints without version info when other endpoints use versioning.
    /// </summary>
    /// <param name="endpoints">All endpoints in the project.</param>
    /// <returns>Collection of diagnostics for endpoints missing versioning.</returns>
    public static ImmutableArray<Diagnostic> DetectMissingVersioning(
        ImmutableArray<EndpointDescriptor> endpoints)
    {
        if (endpoints.IsDefaultOrEmpty || endpoints.Length < 2)
            return ImmutableArray<Diagnostic>.Empty;

        // Check if ANY endpoint has versioning
        var hasVersionedEndpoints = endpoints.Any(static ep => ep.Versioning.HasVersioning);
        if (!hasVersionedEndpoints)
            return ImmutableArray<Diagnostic>.Empty;

        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        foreach (var ep in endpoints)
        {
            // Skip endpoints that already have versioning
            if (ep.Versioning.HasVersioning)
                continue;

            diagnostics.Add(Diagnostic.Create(
                Descriptors.EndpointMissingVersioning,
                Location.None,
                ep.HandlerMethodName));
        }

        return diagnostics.ToImmutable();
    }
}
