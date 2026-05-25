using Microsoft.CodeAnalysis;

namespace ErrorOr.Analyzers;

public static partial class Descriptors
{
    /// <summary>EOE027 — endpoint is [ApiVersionNeutral] but also has [MapToApiVersion]; mutually exclusive.</summary>
    public static readonly DiagnosticDescriptor VersionNeutralWithMappings = new(
        "EOE027",
        "Version-neutral with mappings",
        "Endpoint '{0}' is marked [ApiVersionNeutral] but also has [MapToApiVersion]. Remove one or the other.",
        Category,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>EOE028 — endpoint maps to an API version not declared by [ApiVersion] on the class.</summary>
    public static readonly DiagnosticDescriptor MappedVersionNotDeclared = new(
        "EOE028",
        "Mapped version not declared",
        "Endpoint '{0}' maps to version '{1}' which is not declared in [ApiVersion]. Add [ApiVersion(\"{1}\")] to the class.",
        Category,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>EOE029 — [ApiVersion] used but Asp.Versioning.Http package is not referenced.</summary>
    public static readonly DiagnosticDescriptor ApiVersioningPackageNotReferenced = new(
        "EOE029",
        "Asp.Versioning package not referenced",
        "Endpoint '{0}' uses API versioning but Asp.Versioning.Http package is not referenced",
        Category,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>EOE030 — endpoint has no [ApiVersion] but other endpoints in the project use versioning.</summary>
    public static readonly DiagnosticDescriptor EndpointMissingVersioning = new(
        "EOE030",
        "Endpoint missing versioning",
        "Endpoint '{0}' has no version information but other endpoints use API versioning. " +
        "Add [ApiVersion(\"X.Y\")] or [ApiVersionNeutral] to declare its version scope.",
        Category,
        DiagnosticSeverity.Info,
        true);

    /// <summary>EOE031 — [ApiVersion] string has invalid format; use "major.minor" or "major".</summary>
    public static readonly DiagnosticDescriptor InvalidApiVersionFormat = new(
        "EOE031",
        "Invalid API version format",
        "[ApiVersion(\"{0}\")] has invalid format. Use \"major.minor\" (e.g., \"1.0\") or \"major\" (e.g., \"2\").",
        Category,
        DiagnosticSeverity.Error,
        true);
}
