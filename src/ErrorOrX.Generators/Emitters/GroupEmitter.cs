using System.Collections.Immutable;
using System.Text;
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
            return ImmutableArray<GroupEmitContext>.Empty;

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
                foreach (var v in group.Versions.AsImmutableArray())
                {
                    var versionExpr = v.MinorVersion.HasValue
                        ? $"new {WellKnownTypes.Fqn.ApiVersion}({v.MajorVersion}, {v.MinorVersion.Value})"
                        : $"new {WellKnownTypes.Fqn.ApiVersion}({v.MajorVersion})";

                    code.Append(v.IsDeprecated ? $".HasDeprecatedApiVersion({versionExpr})" : $".HasApiVersion({versionExpr})");
                }

            code.AppendLine(";");
        }
        else
            // Simple group without versioning
            code.AppendLine($"            var {groupVarName} = app.MapGroup(\"{group.GroupPath}\");");

        return groupVarName;
    }

    /// <summary>
    ///     Emits an endpoint mapping within a route group context.
    /// </summary>
    public static void EmitGroupedMapCall(
        StringBuilder code,
        in IndexedEndpoint indexed,
        string groupVarName,
        int maxArity,
        bool hasGlobalVersionSet)
    {
        var ep = indexed.Endpoint;
        var globalIndex = indexed.OriginalIndex;

        // Use relative pattern (group prefix already handled by MapGroup)
        var relativePattern = GetRelativePattern(in ep);

        // Emit the map call against the group variable
        code.Append($"            {groupVarName}.{GetMapMethod(ep.HttpMethod)}(@\"{relativePattern}\", Invoke_Ep{globalIndex})");

        // Emit operation name
        code.AppendLine();
        var operationId = $"{ep.HandlerContainingTypeFqn.Split('.').LastOrDefault()?.TrimStart('@') ?? "Unknown"}_{ep.HandlerMethodName}";
        code.AppendLine($"                .WithName(\"{operationId}\")");

        // For grouped endpoints with specific version mappings, emit MapToApiVersion
        if (!ep.Versioning.MappedVersions.IsDefaultOrEmpty)
            foreach (var v in ep.Versioning.MappedVersions.AsImmutableArray())
            {
                var versionExpr = v.MinorVersion.HasValue
                    ? $"new {WellKnownTypes.Fqn.ApiVersion}({v.MajorVersion}, {v.MinorVersion.Value})"
                    : $"new {WellKnownTypes.Fqn.ApiVersion}({v.MajorVersion})";
                code.AppendLine($"                .MapToApiVersion({versionExpr})");
            }

        // Version-neutral within a versioned group
        if (ep.Versioning.IsVersionNeutral)
            code.AppendLine("                .IsApiVersionNeutral()");

        // Emit middleware (same as ungrouped)
        EmitMiddlewareCalls(code, in ep);

        code.AppendLine("                ;");
    }

    /// <summary>
    ///     Gets the Map method name for the HTTP verb.
    /// </summary>
    private static string GetMapMethod(string httpMethod) => httpMethod switch
    {
        "GET" => "MapGet",
        "POST" => "MapPost",
        "PUT" => "MapPut",
        "DELETE" => "MapDelete",
        "PATCH" => "MapPatch",
        _ => "MapMethods"
    };

    /// <summary>
    ///     Emits middleware fluent calls (authorization, rate limiting, etc.)
    /// </summary>
    private static void EmitMiddlewareCalls(StringBuilder code, in EndpointDescriptor ep)
    {
        var middleware = ep.Middleware;

        // Authorization
        if (middleware.RequiresAuthorization)
            code.AppendLine(middleware.AuthorizationPolicy is not null ? $"                .RequireAuthorization(\"{middleware.AuthorizationPolicy}\")" : "                .RequireAuthorization()");
        else if (middleware.AllowAnonymous)
            code.AppendLine("                .AllowAnonymous()");

        // Rate limiting
        if (middleware.EnableRateLimiting)
            code.AppendLine(middleware.RateLimitingPolicy is not null ? $"                .RequireRateLimiting(\"{middleware.RateLimitingPolicy}\")" : "                .RequireRateLimiting()");
        else if (middleware.DisableRateLimiting)
            code.AppendLine("                .DisableRateLimiting()");

        // Output cache
        if (middleware.EnableOutputCache)
        {
            if (middleware.OutputCachePolicy is not null)
                code.AppendLine($"                .CacheOutput(\"{middleware.OutputCachePolicy}\")");
            else if (middleware.OutputCacheDuration.HasValue)
                code.AppendLine(
                    $"                .CacheOutput(p => p.Expire(TimeSpan.FromSeconds({middleware.OutputCacheDuration.Value})))");
            else
                code.AppendLine("                .CacheOutput()");
        }

        // CORS
        if (middleware.EnableCors)
            code.AppendLine(middleware.CorsPolicy is not null ? $"                .RequireCors(\"{middleware.CorsPolicy}\")" : "                .RequireCors()");
        else if (middleware.DisableCors)
            code.AppendLine("                .DisableCors()");
    }

    /// <summary>
    ///     Context for emitting a route group's endpoints.
    /// </summary>
    internal readonly record struct GroupEmitContext(
        string GroupVariableName,
        RouteGroupAggregate Group);
}
