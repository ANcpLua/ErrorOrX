using static ErrorOr.Generators.GroupAggregator;

namespace ErrorOr.Generators.Emitters;

/// <summary>
///     Emits eShop-style route group declarations for versioned APIs.
///     Generates NewVersionedApi() and MapGroup() calls following the eShop pattern.
/// </summary>
internal static class GroupEmitter
{
    /// <summary>
    ///     Emits route group declarations before endpoint mappings.
    ///     Returns a list of group contexts for use when emitting endpoints.
    /// </summary>
    public static ImmutableArray<GroupEmitContext> EmitGroupDeclarations(
        StringBuilder code,
        ImmutableArray<RouteGroupAggregate> groups)
    {
        if (groups.IsDefaultOrEmpty)
        {
            return ImmutableArray<GroupEmitContext>.Empty;
        }

        var contexts = ImmutableArray.CreateBuilder<GroupEmitContext>(groups.Length);

        foreach (var group in groups)
        {
            var groupVarName = EmitGroupDeclaration(code, group, contexts.Count);
            contexts.Add(new GroupEmitContext(groupVarName, group));
        }

        code.AppendLine();
        return contexts.ToImmutable();
    }

    /// <summary>
    ///     Emits a single route group declaration and returns its variable name.
    /// </summary>
    private static string EmitGroupDeclaration(StringBuilder code, RouteGroupAggregate group, int groupIndex)
    {
        var groupVarName = $"group{groupIndex}";
        var vApiVarName = $"vApi{groupIndex}";

        // Comment explaining the group
        code.AppendLine($"            // Route Group: {group.GroupPath}");

        if (group.UseVersionedApi)
        {
            // eShop pattern: NewVersionedApi() + MapGroup()
            var apiNameArg = group.ApiName is not null ? $"\"{group.ApiName}\"" : "null";
            code.AppendLine($"            var {vApiVarName} = app.NewVersionedApi({apiNameArg});");

            // Build MapGroup with HasApiVersion calls
            code.Append($"            var {groupVarName} = {vApiVarName}.MapGroup(\"{group.GroupPath}\")");

            if (!group.Versions.IsDefaultOrEmpty)
            {
                foreach (var v in group.Versions.AsImmutableArray())
                {
                    var versionExpr = v.MinorVersion.HasValue
                        ? $"new {WellKnownTypes.Fqn.ApiVersion}({v.MajorVersion}, {v.MinorVersion.Value})"
                        : $"new {WellKnownTypes.Fqn.ApiVersion}({v.MajorVersion})";

                    code.Append(v.IsDeprecated
                        ? $".HasDeprecatedApiVersion({versionExpr})"
                        : $".HasApiVersion({versionExpr})");
                }
            }

            code.AppendLine(";");
        }
        else
        // Simple group without versioning
        {
            code.AppendLine($"            var {groupVarName} = app.MapGroup(\"{group.GroupPath}\");");
        }

        return groupVarName;
    }

    /// <summary>
    ///     Emits an endpoint mapping within a route group context.
    /// </summary>
    public static void EmitGroupedMapCall(
        StringBuilder code,
        in IndexedEndpoint indexed,
        string groupVarName,
        int maxArity)
    {
        var ep = indexed.Endpoint;
        var globalIndex = indexed.OriginalIndex;

        // Use relative pattern (group prefix already handled by MapGroup)
        var relativePattern = GetRelativePattern(in ep);

        MapCallEmitter.EmitMapCallStart(code, in ep, groupVarName, relativePattern, globalIndex, "            ");

        // For grouped endpoints with specific version mappings, emit MapToApiVersion
        if (!ep.Versioning.MappedVersions.IsDefaultOrEmpty)
        {
            foreach (var v in ep.Versioning.MappedVersions.AsImmutableArray())
            {
                var versionExpr = v.MinorVersion.HasValue
                    ? $"new {WellKnownTypes.Fqn.ApiVersion}({v.MajorVersion}, {v.MinorVersion.Value})"
                    : $"new {WellKnownTypes.Fqn.ApiVersion}({v.MajorVersion})";
                code.AppendLine($"                .MapToApiVersion({versionExpr})");
            }
        }

        // Version-neutral within a versioned group
        if (ep.Versioning.IsVersionNeutral)
        {
            code.AppendLine("                .IsApiVersionNeutral()");
        }

        EndpointMetadataEmitter.EmitEndpointMetadata(code, in ep, "                ", maxArity);

        MapCallEmitter.EmitMapCallEnd(code, globalIndex, "            ");
    }

    /// <summary>
    ///     Context for emitting a route group's endpoints.
    /// </summary>
    internal readonly record struct GroupEmitContext(
        string GroupVariableName,
        RouteGroupAggregate Group);
}
