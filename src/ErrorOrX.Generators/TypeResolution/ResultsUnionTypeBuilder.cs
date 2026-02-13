using System.Collections.Immutable;
using System.Reflection.Metadata;
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
    ///     Default max arity for Results union types.
    ///     BCL provides Results`2 through Results`6 (may increase in future versions).
    /// </summary>
    private const int DefaultMaxArity = 6;

    internal static int
        DetectMaxArity(ImmutableArray<int> referenceArities)
    {
        var maxArity = DefaultMaxArity;

        if (referenceArities.IsDefaultOrEmpty)
        {
            return maxArity;
        }

        foreach (var arity in referenceArities)
        {
            if (arity > maxArity)
            {
                maxArity = arity;
            }
        }

        return maxArity;
    }

    internal static int GetResultsUnionArity(MetadataReference reference)
    {
        if (reference is not PortableExecutableReference peReference)
        {
            return 0;
        }

        try
        {
            if (peReference.GetMetadata() is not AssemblyMetadata assemblyMetadata)
            {
                return 0;
            }

            var maxArity = 0;
            foreach (var module in assemblyMetadata.GetModules())
            {
                var reader = module.GetMetadataReader();
                if (!IsHttpResultsAssembly(reader))
                {
                    continue;
                }

                TryUpdateMaxArity(reader, ref maxArity);
            }

            return maxArity;
        }
        catch (BadImageFormatException)
        {
            return 0;
        }
    }

    internal static SuccessResponseInfo GetSuccessResponseInfo(
        string successTypeFqn,
        SuccessKind successKind,
        bool isAcceptedResponse = false)
    {
        // Handle [AcceptedResponse] attribute first (highest precedence)
        if (isAcceptedResponse)
        {
            return new SuccessResponseInfo(
                $"{WellKnownTypes.Fqn.HttpResults.Accepted}<{successTypeFqn}>",
                202,
                true,
                $"{WellKnownTypes.Fqn.TypedResults.Accepted}(string.Empty, result.Value)");
        }

        // Map marker types to their correct status codes
        return successKind switch
        {
            SuccessKind.Success => new SuccessResponseInfo(
                WellKnownTypes.Fqn.HttpResults.Ok,
                200,
                false,
                $"{WellKnownTypes.Fqn.TypedResults.Ok}()"),

            SuccessKind.Created => new SuccessResponseInfo(
                WellKnownTypes.Fqn.HttpResults.Created,
                201,
                false,
                $"{WellKnownTypes.Fqn.TypedResults.Created}(string.Empty)"),

            SuccessKind.Updated => new SuccessResponseInfo(
                WellKnownTypes.Fqn.HttpResults.NoContent,
                204,
                false,
                $"{WellKnownTypes.Fqn.TypedResults.NoContent}()"),

            SuccessKind.Deleted => new SuccessResponseInfo(
                WellKnownTypes.Fqn.HttpResults.NoContent,
                204,
                false,
                $"{WellKnownTypes.Fqn.TypedResults.NoContent}()"),

            // Not a marker type - use default 200 OK regardless of method (Minimal API parity)
            // 201 Created is only for Created<T> / CreatedAtRoute<T> which users must return explicitly via ErrorOr<Created>
            _ => new SuccessResponseInfo(
                $"{WellKnownTypes.Fqn.HttpResults.Ok}<{successTypeFqn}>",
                200,
                true,
                $"{WellKnownTypes.Fqn.TypedResults.Ok}(result.Value)")
        };
    }

    /// <summary>
    ///     Computes the optimal return type strategy for an endpoint.
    ///     Strategy: Union-first when total outcomes ≤ maxArity AND no unknown/dynamic outcomes.
    /// </summary>
    public static UnionTypeResult ComputeReturnType(
        string successTypeFqn,
        SuccessKind successKind,
        EquatableArray<string> inferredErrorTypeNames,
        EquatableArray<CustomErrorInfo> inferredCustomErrors,
        EquatableArray<ProducesErrorInfo> declaredProducesErrors,
        bool hasBodyBinding,
        int maxArity = DefaultMaxArity,
        bool isAcceptedResponse = false,
        in MiddlewareInfo middleware = default,
        bool hasParameterValidation = false)
    {
        // Use list of (StatusCode, TypeFqn) tuples for proper ordering
        var unionEntries = new List<(int Status, string TypeFqn)>(8);
        var includedStatuses = new HashSet<int>();

        // Cache the array once to avoid repeated AsImmutableArray() calls
        var errorTypeNamesArray = inferredErrorTypeNames.IsDefaultOrEmpty
            ? ImmutableArray<string>.Empty
            : inferredErrorTypeNames.AsImmutableArray();

        // Pre-detect if Validation errors are present (affects 400 response type choice)
        // Include both ErrorOr validation errors AND DataAnnotations validation on parameters
        var hasValidationError = hasParameterValidation || errorTypeNamesArray.Contains(ErrorMapping.Validation);

        // 1 & 2. Success and Binding outcomes (always present)
        AddSuccessAndBindingOutcomes(successTypeFqn, successKind, isAcceptedResponse, hasBodyBinding,
            hasValidationError, unionEntries, includedStatuses);

        // 3. Built-in ErrorTypes (mapped to static BCL types)
        var hasCustom = AddInferredErrorOutcomes(errorTypeNamesArray, unionEntries, includedStatuses) ||
                        !inferredCustomErrors.IsDefaultOrEmpty ||
                        AddDeclaredErrorOutcomes(declaredProducesErrors, includedStatuses);

        // 4. Middleware-induced status codes (401/403 for auth, 429 for rate limiting)
        AddMiddlewareOutcomes(in middleware, unionEntries, includedStatuses);

        // 5. Decision: use union only when outcomes ≤ maxArity AND no dynamic/unknown outcomes
        var canUseUnion = unionEntries.Count >= 2 && unionEntries.Count <= maxArity && !hasCustom;

        if (!canUseUnion)
        {
            return BuildFallbackResult(inferredErrorTypeNames, declaredProducesErrors, middleware, hasValidationError);
        }

        // 6. Sort by status code (2xx, then 4xx, then 5xx) for consistent OpenAPI output
        var sortedTypes = unionEntries
            .OrderBy(static e => e.Status)
            .Select(static e => e.TypeFqn);

        // Build Results<T1, ..., Tn> type
        // Include error status codes for explicit Produces metadata (needed because the wrapper
        // uses RequestDelegate signature, so union types don't provide metadata automatically)
        return new UnionTypeResult(
            true,
            $"{WellKnownTypes.Fqn.HttpResults.Results}<{string.Join(", ", sortedTypes)}>",
            new EquatableArray<int>([.. unionEntries.Where(static e => e.Status >= 400).Select(static e => e.Status).OrderBy(static x => x)]),
            hasValidationError);
    }

    /// <summary>
    ///     Adds status codes induced by middleware attributes to the union.
    /// </summary>
    private static void AddMiddlewareOutcomes(
        in MiddlewareInfo middleware,
        List<(int Status, string TypeFqn)> unionEntries,
        HashSet<int> includedStatuses)
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
        {
            if (!includedStatuses.Contains(429))
            {
                // StatusCodeHttpResult is used for 429 since there's no typed TooManyRequestsHttpResult
                unionEntries.Add((429, WellKnownTypes.Fqn.HttpResults.StatusCodeHttpResult));
                includedStatuses.Add(429);
            }
        }
    }

    private static void AddSuccessAndBindingOutcomes(
        string successTypeFqn,
        SuccessKind successKind,
        bool isAcceptedResponse,
        bool hasBodyBinding,
        bool hasValidationError,
        List<(int Status, string TypeFqn)> unionEntries,
        HashSet<int> includedStatuses)
    {
        var successInfo = GetSuccessResponseInfo(successTypeFqn, successKind, isAcceptedResponse);
        unionEntries.Add((successInfo.StatusCode, successInfo.ResultTypeFqn));
        includedStatuses.Add(successInfo.StatusCode);

        // 400 response: use ValidationProblem if validation errors are possible, otherwise BadRequest<ProblemDetails>
        // ValidationProblem is preferred when validation errors exist because it provides field-level error details
        var badRequestType = hasValidationError
            ? WellKnownTypes.Fqn.HttpResults.ValidationProblem
            : $"{WellKnownTypes.Fqn.HttpResults.BadRequest}<{WellKnownTypes.Fqn.ProblemDetails}>";
        unionEntries.Add((400, badRequestType));
        includedStatuses.Add(400);

        // UnsupportedMediaType for body binding failures (415) - parity with Minimal APIs
        if (hasBodyBinding)
        {
            unionEntries.Add((415, WellKnownTypes.Fqn.HttpResults.StatusCodeHttpResult));
            includedStatuses.Add(415);
        }

        // InternalServerError<ProblemDetails> for unknown/unhandled errors (always present as safety net)
        // This ensures the default switch case always has a valid return type in the union
        unionEntries.Add((500,
            $"{WellKnownTypes.Fqn.HttpResults.InternalServerError}<{WellKnownTypes.Fqn.ProblemDetails}>"));
        includedStatuses.Add(500);
    }

    private static bool IsHttpResultsAssembly(MetadataReader reader)
    {
        var assemblyDef = reader.GetAssemblyDefinition();
        var name = reader.GetString(assemblyDef.Name);
        return string.Equals(name, "Microsoft.AspNetCore.Http.Results", StringComparison.Ordinal);
    }

    private static void TryUpdateMaxArity(MetadataReader reader, ref int maxArity)
    {
        foreach (var handle in reader.TypeDefinitions)
        {
            var typeDef = reader.GetTypeDefinition(handle);
            if (!IsResultsUnionType(reader, typeDef))
            {
                continue;
            }

            var arity = typeDef.GetGenericParameters().Count;
            if (arity > maxArity)
            {
                maxArity = arity;
            }
        }
    }

    private static bool IsResultsUnionType(MetadataReader reader, TypeDefinition typeDef)
    {
        if (!typeDef.Namespace.IsNil)
        {
            var ns = reader.GetString(typeDef.Namespace);
            if (!string.Equals(ns, "Microsoft.AspNetCore.Http.HttpResults", StringComparison.Ordinal))
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        var name = reader.GetString(typeDef.Name);
        return string.Equals(name, "Results", StringComparison.Ordinal);
    }

    private static bool AddInferredErrorOutcomes(
        ImmutableArray<string> errorTypeNamesArray,
        ICollection<(int Status, string TypeFqn)> unionEntries,
        ISet<int> includedStatuses)
    {
        if (errorTypeNamesArray.IsDefaultOrEmpty)
        {
            return false;
        }

        var hasUnknown = false;
        foreach (var errorTypeName in errorTypeNamesArray
                     .Distinct()
                     .OrderBy(static x => x, StringComparer.Ordinal))
        {
            if (!ErrorMapping.IsKnownErrorType(errorTypeName))
            {
                hasUnknown = true;
            }

            AddInferredError(errorTypeName, unionEntries, includedStatuses);
        }

        return hasUnknown;
    }

    private static void AddInferredError(
        string errorTypeName,
        ICollection<(int Status, string TypeFqn)> unionEntries,
        ISet<int> includedStatuses)
    {
        var entry = ErrorMapping.Get(errorTypeName);

        // Validation is now handled in AddSuccessAndBindingOutcomes (uses ValidationProblem for 400)
        // Skip adding here to avoid duplicate 400 entries
        if (errorTypeName == ErrorMapping.Validation)
        {
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
        HashSet<int> includedStatuses)
    {
        if (declaredProducesErrors.IsDefaultOrEmpty)
        {
            return false;
        }

        foreach (var producesError in declaredProducesErrors)
        {
            if (!includedStatuses.Contains(producesError.StatusCode))
            {
                return true; // Found a status code not in our static union mapping
            }
        }

        return false;
    }

    private static UnionTypeResult BuildFallbackResult(
        EquatableArray<string> inferredErrorTypeNames,
        EquatableArray<ProducesErrorInfo> declaredProducesErrors,
        in MiddlewareInfo middleware = default,
        bool hasValidationError = false)
    {
        var allStatuses = new HashSet<int>
        {
            400
        }; // Always include binding failure

        CollectInferredErrorStatuses(inferredErrorTypeNames, allStatuses);
        CollectDeclaredErrorStatuses(declaredProducesErrors, allStatuses);
        CollectMiddlewareStatuses(in middleware, allStatuses);

        return new UnionTypeResult(
            false,
            WellKnownTypes.Fqn.Result,
            new EquatableArray<int>([.. allStatuses.OrderBy(static x => x)]),
            hasValidationError);
    }

    /// <summary>
    ///     Collects HTTP status codes from middleware attributes.
    /// </summary>
    private static void CollectMiddlewareStatuses(
        in MiddlewareInfo middleware,
        HashSet<int> allStatuses)
    {
        if (middleware is { RequiresAuthorization: true, AllowAnonymous: false })
        {
            allStatuses.Add(401);
            allStatuses.Add(403);
        }

        if (middleware is { EnableRateLimiting: true, DisableRateLimiting: false })
        {
            allStatuses.Add(429);
        }
    }

    /// <summary>
    ///     Collects HTTP status codes from inferred error type names.
    /// </summary>
    private static void CollectInferredErrorStatuses(
        EquatableArray<string> inferredErrorTypeNames,
        HashSet<int> allStatuses)
    {
        if (inferredErrorTypeNames.IsDefaultOrEmpty)
        {
            return;
        }

        var array = inferredErrorTypeNames.AsImmutableArray();
        foreach (var errorTypeName in array.Distinct())
            allStatuses.Add(ErrorMapping.GetStatusCode(errorTypeName));
    }

    /// <summary>
    ///     Collects HTTP status codes from declared ProducesError attributes.
    /// </summary>
    private static void CollectDeclaredErrorStatuses(
        EquatableArray<ProducesErrorInfo> declaredProducesErrors,
        HashSet<int> allStatuses)
    {
        if (declaredProducesErrors.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var pe in declaredProducesErrors)
            allStatuses.Add(pe.StatusCode);
    }
}
