using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;

namespace ErrorOr.Generators;

/// <summary>
///     Computes Results&lt;...&gt; union return types for OpenAPI metadata inference.
///     Implements the BCL-forward approach: typed results in union for automatic OpenAPI, deterministic fallback
///     otherwise.
/// </summary>
internal static class ResultsUnionTypeBuilder
{
    /// <summary>
    ///     Default max arity for Results union types. Detected from compilation when possible.
    ///     BCL provides Results`2 through Results`6 (may increase in future versions).
    /// </summary>
    private const int DefaultMaxArity = 6;

    internal static SuccessResponseInfo GetSuccessResponseInfo(
        string successTypeFqn,
        SuccessKind successKind,
        string httpMethod,
        bool isAcceptedResponse = false)
    {
        // Handle [AcceptedResponse] attribute first (highest precedence)
        if (isAcceptedResponse)
            return new SuccessResponseInfo(
                $"{WellKnownTypes.Fqn.HttpResults.Accepted}<{successTypeFqn}>",
                202,
                true,
                $"{WellKnownTypes.Fqn.TypedResults.Accepted}(string.Empty, result.Value)",
                $"value => {WellKnownTypes.Fqn.TypedResults.Accepted}(string.Empty, value)");

        // Map marker types to their correct status codes
        return successKind switch
        {
            SuccessKind.Success => new SuccessResponseInfo(
                WellKnownTypes.Fqn.HttpResults.Ok,
                200,
                false,
                $"{WellKnownTypes.Fqn.TypedResults.Ok}()",
                $"_ => {WellKnownTypes.Fqn.TypedResults.Ok}()"),

            SuccessKind.Created => new SuccessResponseInfo(
                WellKnownTypes.Fqn.HttpResults.Created,
                201,
                false,
                $"{WellKnownTypes.Fqn.TypedResults.Created}(string.Empty)",
                $"_ => {WellKnownTypes.Fqn.TypedResults.Created}(string.Empty)"),

            SuccessKind.Updated => new SuccessResponseInfo(
                WellKnownTypes.Fqn.HttpResults.NoContent,
                204,
                false,
                $"{WellKnownTypes.Fqn.TypedResults.NoContent}()",
                $"_ => {WellKnownTypes.Fqn.TypedResults.NoContent}()"),

            SuccessKind.Deleted => new SuccessResponseInfo(
                WellKnownTypes.Fqn.HttpResults.NoContent,
                204,
                false,
                $"{WellKnownTypes.Fqn.TypedResults.NoContent}()",
                $"_ => {WellKnownTypes.Fqn.TypedResults.NoContent}()"),

            // Not a marker type - determine default response by HTTP method
            SuccessKind.Payload when httpMethod == WellKnownTypes.HttpMethod.Post =>
                new SuccessResponseInfo(
                    $"{WellKnownTypes.Fqn.HttpResults.Created}<{successTypeFqn}>",
                    201,
                    true,
                    $"{WellKnownTypes.Fqn.TypedResults.Created}(string.Empty, result.Value)",
                    $"value => {WellKnownTypes.Fqn.TypedResults.Created}(string.Empty, value)"),

            _ => new SuccessResponseInfo(
                $"{WellKnownTypes.Fqn.HttpResults.Ok}<{successTypeFqn}>",
                200,
                true,
                $"{WellKnownTypes.Fqn.TypedResults.Ok}(result.Value)",
                $"value => {WellKnownTypes.Fqn.TypedResults.Ok}(value)")
        };
    }

    /// <summary>
    ///     Detects the maximum supported Results arity from the compilation.
    ///     Checks for Results`2 through Results`10 and returns the highest found.
    /// </summary>
    public static int DetectMaxArity(Compilation compilation)
    {
        // Check from highest to lowest, return first found
        for (var arity = 10; arity >= 2; arity--)
        {
            var typeName = $"Microsoft.AspNetCore.Http.HttpResults.Results`{arity}";
            var type = compilation.GetBestTypeByMetadataName(typeName);
            if (type is not null)
                return arity;
        }

        return DefaultMaxArity;
    }

    /// <summary>
    ///     Computes the optimal return type strategy for an endpoint.
    ///     Strategy: Union-first when total outcomes ≤ maxArity AND no unknown/dynamic outcomes.
    /// </summary>
    public static UnionTypeResult ComputeReturnType(
        string successTypeFqn,
        SuccessKind successKind,
        string httpMethod,
        EquatableArray<string> inferredErrorTypeNames,
        EquatableArray<CustomErrorInfo> inferredCustomErrors,
        EquatableArray<ProducesErrorInfo> declaredProducesErrors,
        int maxArity = DefaultMaxArity,
        bool isAcceptedResponse = false,
        MiddlewareInfo middleware = default)
    {
        // Use list of (StatusCode, TypeFqn) tuples for proper ordering
        var unionEntries = new List<(int Status, string TypeFqn)>(8);
        var includedStatuses = new HashSet<int>();

        // 1 & 2. Success and Binding outcomes (always present)
        AddSuccessAndBindingOutcomes(successTypeFqn, successKind, httpMethod, isAcceptedResponse, unionEntries,
            includedStatuses);

        // 3. Built-in ErrorTypes (mapped to static BCL types)
        var hasCustom = AddInferredErrorOutcomes(inferredErrorTypeNames, unionEntries, includedStatuses) ||
                        !inferredCustomErrors.IsDefaultOrEmpty ||
                        AddDeclaredErrorOutcomes(declaredProducesErrors, includedStatuses);

        // 4. Middleware-induced status codes (401/403 for auth, 429 for rate limiting)
        AddMiddlewareOutcomes(middleware, unionEntries, includedStatuses);

        // 5. Custom errors and declared [ProducesError] (force fallback if unknown)

        // 6. Decision: use union only when outcomes ≤ maxArity AND no dynamic/unknown outcomes
        var canUseUnion = unionEntries.Count >= 2 && unionEntries.Count <= maxArity && !hasCustom;

        if (!canUseUnion)
            return BuildFallbackResult(inferredErrorTypeNames, declaredProducesErrors, middleware);

        // 7. Sort by status code (2xx, then 4xx, then 5xx) for consistent OpenAPI output
        var sortedTypes = unionEntries
            .OrderBy(static e => e.Status)
            .Select(static e => e.TypeFqn);

        // Build Results<T1, ..., Tn> type
        return new UnionTypeResult(
            true,
            $"{WellKnownTypes.Fqn.HttpResults.Results}<{string.Join(", ", sortedTypes)}>",
            default);
    }

    /// <summary>
    ///     Adds status codes induced by middleware attributes to the union.
    /// </summary>
    private static void AddMiddlewareOutcomes(
        in MiddlewareInfo middleware,
        ICollection<(int Status, string TypeFqn)> unionEntries,
        ISet<int> includedStatuses)
    {
        // [Authorize] adds 401 Unauthorized and 403 Forbidden
        if (middleware is { RequiresAuthorization: true, AllowAnonymous: false })
        {
            if (!includedStatuses.Contains(401))
            {
                unionEntries.Add((401, WellKnownTypes.Fqn.HttpResults.UnauthorizedHttpResult));
                includedStatuses.Add(401);
            }

            if (!includedStatuses.Contains(403))
            {
                unionEntries.Add((403, WellKnownTypes.Fqn.HttpResults.ForbidHttpResult));
                includedStatuses.Add(403);
            }
        }

        // [EnableRateLimiting] adds 429 Too Many Requests
        if (middleware is { EnableRateLimiting: true, DisableRateLimiting: false })
            if (!includedStatuses.Contains(429))
            {
                // StatusCodeHttpResult is used for 429 since there's no typed TooManyRequestsHttpResult
                unionEntries.Add((429, WellKnownTypes.Fqn.HttpResults.StatusCodeHttpResult));
                includedStatuses.Add(429);
            }
    }

    private static void AddSuccessAndBindingOutcomes(
        string successTypeFqn,
        SuccessKind successKind,
        string httpMethod,
        bool isAcceptedResponse,
        ICollection<(int Status, string TypeFqn)> unionEntries,
        ISet<int> includedStatuses)
    {
        var successInfo = GetSuccessResponseInfo(successTypeFqn, successKind, httpMethod, isAcceptedResponse);
        unionEntries.Add((successInfo.StatusCode, successInfo.ResultTypeFqn));
        includedStatuses.Add(successInfo.StatusCode);

        // BadRequest<ProblemDetails> for binding failures (always present)
        unionEntries.Add((400, $"{WellKnownTypes.Fqn.HttpResults.BadRequest}<{WellKnownTypes.Fqn.ProblemDetails}>"));
        includedStatuses.Add(400);

        // InternalServerError<ProblemDetails> for unknown/unhandled errors (always present as safety net)
        // This ensures the default switch case always has a valid return type in the union
        unionEntries.Add((500,
            $"{WellKnownTypes.Fqn.HttpResults.InternalServerError}<{WellKnownTypes.Fqn.ProblemDetails}>"));
        includedStatuses.Add(500);
    }

    private static bool AddInferredErrorOutcomes(
        EquatableArray<string> inferredErrorTypeNames,
        ICollection<(int Status, string TypeFqn)> unionEntries,
        ISet<int> includedStatuses)
    {
        if (inferredErrorTypeNames.IsDefaultOrEmpty)
            return false;

        foreach (var errorTypeName in inferredErrorTypeNames.AsImmutableArray()
                     .Distinct()
                     .OrderBy(static x => x, StringComparer.Ordinal))
            AddInferredError(errorTypeName, unionEntries, includedStatuses);

        return false;
    }

    private static void AddInferredError(
        string errorTypeName,
        ICollection<(int Status, string TypeFqn)> unionEntries,
        ISet<int> includedStatuses)
    {
        var entry = ErrorMapping.Get(errorTypeName);

        if (errorTypeName ==
            ErrorMapping.Validation) // Validation: uses ValidationProblem (also 400, but different type)
        {
            // ValidationProblem is a special case - it's 400 but different type than BadRequest
            // Use status 400 for sorting but keep as separate entry
            unionEntries.Add((400, entry.TypeFqn));
            return;
        }

        if (!includedStatuses.Contains(entry.StatusCode))
        {
            unionEntries.Add((entry.StatusCode, entry.TypeFqn));
            includedStatuses.Add(entry.StatusCode);
        }
    }

    private static bool AddDeclaredErrorOutcomes(
        EquatableArray<ProducesErrorInfo> declaredProducesErrors,
        ICollection<int> includedStatuses)
    {
        if (declaredProducesErrors.IsDefaultOrEmpty)
            return false;

        foreach (var producesError in declaredProducesErrors)
            if (!includedStatuses.Contains(producesError.StatusCode))
                return true; // Found a status code not in our static union mapping

        return false;
    }

    private static UnionTypeResult BuildFallbackResult(
        EquatableArray<string> inferredErrorTypeNames,
        EquatableArray<ProducesErrorInfo> declaredProducesErrors,
        MiddlewareInfo middleware = default)
    {
        var allStatuses = new HashSet<int> { 400 }; // Always include binding failure

        CollectInferredErrorStatuses(inferredErrorTypeNames, allStatuses);
        CollectDeclaredErrorStatuses(declaredProducesErrors, allStatuses);
        CollectMiddlewareStatuses(middleware, allStatuses);

        return new UnionTypeResult(
            false,
            WellKnownTypes.Fqn.Result,
            new EquatableArray<int>([.. allStatuses.OrderBy(static x => x)]));
    }

    /// <summary>
    ///     Collects HTTP status codes from middleware attributes.
    /// </summary>
    private static void CollectMiddlewareStatuses(
        in MiddlewareInfo middleware,
        ISet<int> allStatuses)
    {
        if (middleware is { RequiresAuthorization: true, AllowAnonymous: false })
        {
            allStatuses.Add(401);
            allStatuses.Add(403);
        }

        if (middleware is { EnableRateLimiting: true, DisableRateLimiting: false })
            allStatuses.Add(429);
    }

    /// <summary>
    ///     Collects HTTP status codes from inferred error type names.
    /// </summary>
    private static void CollectInferredErrorStatuses(
        EquatableArray<string> inferredErrorTypeNames,
        ISet<int> allStatuses)
    {
        if (inferredErrorTypeNames.IsDefaultOrEmpty)
            return;

        foreach (var errorTypeName in inferredErrorTypeNames.AsImmutableArray().Distinct())
            allStatuses.Add(ErrorMapping.GetStatusCode(errorTypeName));
    }

    /// <summary>
    ///     Collects HTTP status codes from declared ProducesError attributes.
    /// </summary>
    private static void CollectDeclaredErrorStatuses(
        EquatableArray<ProducesErrorInfo> declaredProducesErrors,
        ISet<int> allStatuses)
    {
        if (declaredProducesErrors.IsDefaultOrEmpty)
            return;

        foreach (var pe in declaredProducesErrors)
            allStatuses.Add(pe.StatusCode);
    }
}