namespace ErrorOr.Generators;

/// <summary>
///     Emits Asp.Versioning.Http calls — global version set, per-endpoint version mapping,
///     and version-neutral marker. Triggered when at least one endpoint declares
///     <c>[ApiVersion]</c> or <c>[ApiVersionNeutral]</c>.
/// </summary>
public sealed partial class ErrorOrEndpointGenerator
{
    /// <summary>
    ///     Computes the global version set from all endpoints.
    /// </summary>
    private static VersionSetContext ComputeGlobalVersionSet(ImmutableArray<EndpointDescriptor> endpoints)
    {
        var hasVersionNeutral = endpoints.Any(static ep => ep.Versioning.IsVersionNeutral);

        var sortedVersions = endpoints
            .SelectMany(static ep => ep.Versioning.SupportedVersions.AsImmutableArray())
            .Distinct()
            .OrderBy(static v => v.MajorVersion)
            .ThenBy(static v => v.MinorVersion ?? 0)
            .ToImmutableArray();

        return new VersionSetContext(sortedVersions, hasVersionNeutral);
    }

    /// <summary>
    ///     Emits the version set builder before endpoint mappings.
    /// </summary>
    private static void EmitVersionSet(StringBuilder code, VersionSetContext versionSet)
    {
        code.AppendLine("            // API Versioning: Build version set for all endpoints");
        code.AppendLine("            var versionSet = app.NewApiVersionSet()");

        foreach (var v in versionSet.AllVersions)
        {
            var versionExpr = v.MinorVersion.HasValue
                ? $"new {WellKnownTypes.Fqn.ApiVersion}({v.MajorVersion}, {v.MinorVersion.Value})"
                : $"new {WellKnownTypes.Fqn.ApiVersion}({v.MajorVersion})";

            code.AppendLine(v.IsDeprecated
                ? $"                .HasDeprecatedApiVersion({versionExpr})"
                : $"                .HasApiVersion({versionExpr})");
        }

        code.AppendLine("                .ReportApiVersions()");
        code.AppendLine("                .Build();");
        code.AppendLine();
    }

    /// <summary>
    ///     Emits API versioning fluent calls for an endpoint.
    /// </summary>
    private static void EmitVersioningCalls(StringBuilder code, in VersioningInfo versioning, bool hasGlobalVersionSet)
    {
        // If no global version set exists, don't emit anything
        if (!hasGlobalVersionSet) return;

        // Version-neutral endpoints don't map to any specific version
        if (versioning.IsVersionNeutral)
        {
            code.AppendLine("                .IsApiVersionNeutral()");
            return;
        }

        // Apply the version set to the endpoint
        code.AppendLine("                .WithApiVersionSet(versionSet)");

        // If endpoint has specific versions to map to, emit MapToApiVersion calls
        var effectiveVersions = versioning.EffectiveVersions;
        if (!effectiveVersions.IsDefaultOrEmpty)
        {
            foreach (var v in effectiveVersions.AsImmutableArray())
            {
                var versionExpr = v.MinorVersion.HasValue
                    ? $"new {WellKnownTypes.Fqn.ApiVersion}({v.MajorVersion}, {v.MinorVersion.Value})"
                    : $"new {WellKnownTypes.Fqn.ApiVersion}({v.MajorVersion})";
                code.AppendLine($"                .MapToApiVersion({versionExpr})");
            }
        }
    }

    /// <summary>
    ///     Aggregated version set information with hasVersioning flag.
    /// </summary>
    private readonly record struct VersionSetContext(
        ImmutableArray<ApiVersionInfo> AllVersions,
        bool HasVersionNeutralEndpoints)
    {
        public bool HasVersioning => !AllVersions.IsDefaultOrEmpty || HasVersionNeutralEndpoints;
    }
}
