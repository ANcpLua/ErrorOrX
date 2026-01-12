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
    private const string HttpResultsNs = "global::Microsoft.AspNetCore.Http.HttpResults";
    private const string ResultsNs = "global::Microsoft.AspNetCore.Http";
    private const string ProblemDetailsType = "global::Microsoft.AspNetCore.Mvc.ProblemDetails";

    /// <summary>
    ///     Default max arity for Results union types. Detected from compilation when possible.
    ///     As of .NET 10, the BCL provides Results`2 through Results`6.
    /// </summary>
    private const int DefaultMaxArity = 6;

    /// <summary>
    ///     Gets the HTTP status code for an ErrorType.
    ///     Delegates to ErrorMapping for single source of truth.
    /// </summary>
    public static int GetStatusCodeForErrorType(ErrorType errorType)
    {
        return ErrorMapping.GetStatusCode(errorType);
    }

    /// <summary>
    ///     Generates the switch expression body for ErrorType → Status mapping.
    ///     Delegates to ErrorMapping for single source of truth.
    /// </summary>
    public static string GenerateErrorTypeToStatusSwitch(string errorTypeFqn)
    {
        return ErrorMapping.GenerateStatusSwitch(errorTypeFqn);
    }

    /// <summary>
    ///     Gets the RFC 9110 compliant title for a given HTTP status code.
    ///     Delegates to StatusCodeTitles for single source of truth.
    /// </summary>
    public static string GetTitleForStatusCode(int statusCode)
    {
        return StatusCodeTitles.Get(statusCode);
    }

    /// <summary>
    ///     Maps a custom error numeric type to the appropriate TypedResults factory.
    ///     Delegates to ErrorMapping for single source of truth.
    /// </summary>
    public static (string Factory, string TypeFqn, bool HasBody) GetCustomErrorMapping(int numericType)
    {
        var entry = ErrorMapping.GetCustom(numericType);
        return (entry.Factory, entry.TypeFqn, entry.HasBody);
    }

    internal static SuccessResponseInfo GetSuccessResponseInfo(
        string successTypeFqn,
        SuccessKind successKind,
        string httpMethod,
        bool isAcceptedResponse = false)
    {
        // Handle [AcceptedResponse] attribute first (highest precedence)
        if (isAcceptedResponse)
            return new SuccessResponseInfo(
                $"{HttpResultsNs}.Accepted<{successTypeFqn}>",
                202,
                true,
                "global::Microsoft.AspNetCore.Http.TypedResults.Accepted(string.Empty, result.Value)",
                "value => global::Microsoft.AspNetCore.Http.TypedResults.Accepted(string.Empty, value)");

        // Map marker types to their correct status codes
        return successKind switch
        {
            SuccessKind.Success => new SuccessResponseInfo(
                $"{HttpResultsNs}.Ok",
                200,
                false,
                "global::Microsoft.AspNetCore.Http.TypedResults.Ok()",
                "_ => global::Microsoft.AspNetCore.Http.TypedResults.Ok()"),

            SuccessKind.Created => new SuccessResponseInfo(
                $"{HttpResultsNs}.Created",
                201,
                false,
                "global::Microsoft.AspNetCore.Http.TypedResults.Created(string.Empty)",
                "_ => global::Microsoft.AspNetCore.Http.TypedResults.Created(string.Empty)"),

            SuccessKind.Updated => new SuccessResponseInfo(
                $"{HttpResultsNs}.NoContent",
                204,
                false,
                "global::Microsoft.AspNetCore.Http.TypedResults.NoContent()",
                "_ => global::Microsoft.AspNetCore.Http.TypedResults.NoContent()"),

            SuccessKind.Deleted => new SuccessResponseInfo(
                $"{HttpResultsNs}.NoContent",
                204,
                false,
                "global::Microsoft.AspNetCore.Http.TypedResults.NoContent()",
                "_ => global::Microsoft.AspNetCore.Http.TypedResults.NoContent()"),

            // Not a marker type - determine default response by HTTP method
            SuccessKind.Payload when httpMethod == WellKnownTypes.HttpMethod.Post =>
                new SuccessResponseInfo(
                    $"{HttpResultsNs}.Created<{successTypeFqn}>",
                    201,
                    true,
                    "global::Microsoft.AspNetCore.Http.TypedResults.Created(string.Empty, result.Value)",
                    "value => global::Microsoft.AspNetCore.Http.TypedResults.Created(string.Empty, value)"),

            _ => new SuccessResponseInfo(
                $"{HttpResultsNs}.Ok<{successTypeFqn}>",
                200,
                true,
                "global::Microsoft.AspNetCore.Http.TypedResults.Ok(result.Value)",
                "value => global::Microsoft.AspNetCore.Http.TypedResults.Ok(value)")
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
        EquatableArray<int> inferredErrorTypes,
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
        var hasCustom = AddInferredErrorOutcomes(inferredErrorTypes, unionEntries, includedStatuses) ||
                        !inferredCustomErrors.IsDefaultOrEmpty ||
                        AddDeclaredErrorOutcomes(declaredProducesErrors, includedStatuses);

        // 4. Middleware-induced status codes (401/403 for auth, 429 for rate limiting)
        AddMiddlewareOutcomes(middleware, unionEntries, includedStatuses);

        // 5. Custom errors and declared [ProducesError] (force fallback if unknown)

        // 6. Decision: use union only when outcomes ≤ maxArity AND no dynamic/unknown outcomes
        var canUseUnion = unionEntries.Count >= 2 && unionEntries.Count <= maxArity && !hasCustom;

        if (!canUseUnion)
            return BuildFallbackResult(inferredErrorTypes, declaredProducesErrors, middleware);

        // 7. Sort by status code (2xx, then 4xx, then 5xx) for consistent OpenAPI output
        var sortedTypes = unionEntries
            .OrderBy(static e => e.Status)
            .Select(static e => e.TypeFqn);

        // Build Results<T1, ..., Tn> type
        return new UnionTypeResult(
            true,
            $"{HttpResultsNs}.Results<{string.Join(", ", sortedTypes)}>",
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
        if (middleware.RequiresAuthorization && !middleware.AllowAnonymous)
        {
            if (!includedStatuses.Contains(401))
            {
                unionEntries.Add((401, $"{HttpResultsNs}.UnauthorizedHttpResult"));
                includedStatuses.Add(401);
            }

            if (!includedStatuses.Contains(403))
            {
                unionEntries.Add((403, $"{HttpResultsNs}.ForbidHttpResult"));
                includedStatuses.Add(403);
            }
        }

        // [EnableRateLimiting] adds 429 Too Many Requests
        if (middleware.EnableRateLimiting && !middleware.DisableRateLimiting)
            if (!includedStatuses.Contains(429))
            {
                // StatusCodeHttpResult is used for 429 since there's no typed TooManyRequestsHttpResult
                unionEntries.Add((429, $"{HttpResultsNs}.StatusCodeHttpResult"));
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
        unionEntries.Add((400, $"{HttpResultsNs}.BadRequest<{ProblemDetailsType}>"));
        includedStatuses.Add(400);

        // InternalServerError<ProblemDetails> for unknown/unhandled errors (always present as safety net)
        // This ensures the default switch case always has a valid return type in the union
        unionEntries.Add((500, $"{HttpResultsNs}.InternalServerError<{ProblemDetailsType}>"));
        includedStatuses.Add(500);
    }

    private static bool AddInferredErrorOutcomes(
        EquatableArray<int> inferredErrorTypes,
        ICollection<(int Status, string TypeFqn)> unionEntries,
        ISet<int> includedStatuses)
    {
        if (inferredErrorTypes.IsDefaultOrEmpty)
            return false;

        foreach (var errorTypeInt in inferredErrorTypes.AsImmutableArray().Distinct().OrderBy(static x => x))
        {
            var errorType = Enum.IsDefined(typeof(ErrorType), errorTypeInt)
                ? (ErrorType)errorTypeInt
                : ErrorType.Failure;
            AddInferredError(errorType, unionEntries, includedStatuses);
        }

        return false;
    }

    private static void AddInferredError(
        ErrorType errorType,
        ICollection<(int Status, string TypeFqn)> unionEntries,
        ISet<int> includedStatuses)
    {
        var entry = ErrorMapping.Get(errorType);

        if (errorType == ErrorType.Validation) // Validation: uses ValidationProblem (also 400, but different type)
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
        EquatableArray<int> inferredErrorTypes,
        EquatableArray<ProducesErrorInfo> declaredProducesErrors,
        MiddlewareInfo middleware = default)
    {
        var allStatuses = new HashSet<int> { 400 }; // Always include binding failure

        CollectInferredErrorStatuses(inferredErrorTypes, allStatuses);
        CollectDeclaredErrorStatuses(declaredProducesErrors, allStatuses);
        CollectMiddlewareStatuses(middleware, allStatuses);

        return new UnionTypeResult(
            false,
            $"{ResultsNs}.IResult",
            new EquatableArray<int>([.. allStatuses.OrderBy(static x => x)]));
    }

    /// <summary>
    ///     Collects HTTP status codes from middleware attributes.
    /// </summary>
    private static void CollectMiddlewareStatuses(
        in MiddlewareInfo middleware,
        ISet<int> allStatuses)
    {
        if (middleware.RequiresAuthorization && !middleware.AllowAnonymous)
        {
            allStatuses.Add(401);
            allStatuses.Add(403);
        }

        if (middleware.EnableRateLimiting && !middleware.DisableRateLimiting)
            allStatuses.Add(429);
    }

    /// <summary>
    ///     Collects HTTP status codes from inferred error types.
    /// </summary>
    private static void CollectInferredErrorStatuses(
        EquatableArray<int> inferredErrorTypes,
        ISet<int> allStatuses)
    {
        if (inferredErrorTypes.IsDefaultOrEmpty)
            return;

        foreach (var errorTypeInt in inferredErrorTypes.AsImmutableArray().Distinct())
        {
            var errorType = Enum.IsDefined(typeof(ErrorType), errorTypeInt)
                ? (ErrorType)errorTypeInt
                : ErrorType.Failure;
            allStatuses.Add(ErrorMapping.GetStatusCode(errorType));
        }
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

    /// <summary>
    ///     Gets the ErrorType enum name from its value.
    /// </summary>
    public static string GetErrorTypeName(ErrorType errorType)
    {
        return errorType.ToString();
    }

    /// <summary>
    ///     Gets the ErrorType enum name from its integer value.
    /// </summary>
    public static string GetErrorTypeName(int errorTypeInt)
    {
        var errorType = Enum.IsDefined(typeof(ErrorType), errorTypeInt) ? (ErrorType)errorTypeInt : ErrorType.Failure;
        return errorType.ToString();
    }
}