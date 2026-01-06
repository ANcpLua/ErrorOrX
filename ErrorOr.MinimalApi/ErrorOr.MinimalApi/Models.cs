using ANcpLua.Roslyn.Utilities;
using ANcpLua.Roslyn.Utilities.Models;
using Microsoft.CodeAnalysis;

namespace ErrorOr.MinimalApi;

internal enum EndpointParameterSource
{
    Route,
    Body,
    Query,
    Header,
    Service,
    KeyedService,
    AsParameters,
    HttpContext,
    CancellationToken,
    Form,
    FormFile,
    FormFiles,
    FormCollection,
    Stream,
    PipeReader
}

/// <summary>
///     Represents the custom binding method detected on a parameter type.
/// </summary>
internal enum CustomBindingMethod
{
    None,
    TryParse,
    TryParseWithFormat,
    BindAsync,
    BindAsyncWithParam,
    Bindable
}

internal readonly record struct EndpointParameter(
    string Name,
    string TypeFqn,
    EndpointParameterSource Source,
    string? KeyName,
    bool IsNullable,
    bool IsNonNullableValueType,
    bool IsCollection,
    string? CollectionItemTypeFqn,
    EquatableArray<EndpointParameter> Children,
    CustomBindingMethod CustomBinding = CustomBindingMethod.None);

/// <summary>
///     Represents a custom error detected via Error.Custom() call.
/// </summary>
internal readonly record struct CustomErrorInfo(
    string ErrorCode,
    int SuggestedStatusCode);

/// <summary>
///     Represents a [ProducesError] attribute on an endpoint method.
/// </summary>
internal readonly record struct ProducesErrorInfo(
    int StatusCode,
    string ErrorCode);

internal readonly record struct EndpointDescriptor(
    string HttpMethod,
    string Pattern,
    string SuccessTypeFqn,
    bool IsAsync,
    string HandlerContainingTypeFqn,
    string HandlerMethodName,
    bool IsObsolete,
    string? ObsoleteMessage,
    bool IsObsoleteError,
    EquatableArray<EndpointParameter> HandlerParameters,
    EquatableArray<int> InferredErrorTypes,
    EquatableArray<CustomErrorInfo> DetectedCustomErrors,
    EquatableArray<ProducesErrorInfo> DeclaredProducesErrors,
    bool IsSse = false,
    string? SseItemTypeFqn = null,
    bool UsesSseItem = false,
    bool IsAcceptedResponse = false);

internal readonly record struct EndpointData(
    EquatableArray<EndpointDescriptor> Descriptors,
    EquatableArray<DiagnosticInfo> Diagnostics)
{
    public static EndpointData Empty
    {
        get => new(
            default,
            default);
    }
}

internal readonly record struct ParameterMeta(
    int Index,
    IParameterSymbol Symbol,
    string Name,
    string TypeFqn,
    RoutePrimitiveKind? RouteKind,
    bool HasFromServices,
    bool HasFromKeyedServices,
    string? KeyedServiceKey,
    bool HasFromBody,
    bool HasFromRoute,
    bool HasFromQuery,
    bool HasFromHeader,
    bool HasAsParameters,
    string RouteName,
    string QueryName,
    string HeaderName,
    bool IsCancellationToken,
    bool IsHttpContext,
    bool IsNullable,
    bool IsNonNullableValueType,
    bool IsCollection,
    string? CollectionItemTypeFqn,
    RoutePrimitiveKind? CollectionItemPrimitiveKind,
    bool HasFromForm,
    string FormName,
    bool IsFormFile,
    bool IsFormFileCollection,
    bool IsFormCollection,
    bool IsStream,
    bool IsPipeReader,
    CustomBindingMethod CustomBinding);

internal enum RoutePrimitiveKind
{
    String,
    Int32,
    Int64,
    Int16,
    UInt32,
    UInt64,
    UInt16,
    Byte,
    SByte,
    Boolean,
    Decimal,
    Double,
    Single,
    Guid,
    DateTime,
    DateTimeOffset,
    DateOnly,
    TimeOnly,
    TimeSpan
}

/// <summary>
///     Computes Results&lt;...&gt; union return types for OpenAPI metadata inference.
///     Implements the BCL-forward approach: typed results in union for automatic OpenAPI, deterministic fallback
///     otherwise.
/// </summary>
internal static class ResultsUnionTypeBuilder
{
    // Namespace prefixes
    private const string HttpResultsNs = "global::Microsoft.AspNetCore.Http.HttpResults";
    private const string ResultsNs = "global::Microsoft.AspNetCore.Http";
    private const string ProblemDetailsType = "global::Microsoft.AspNetCore.Mvc.ProblemDetails";

    /// <summary>
    ///     Default max arity for Results union types. Detected from compilation when possible.
    ///     As of .NET 10, the BCL provides Results`2 through Results`6.
    /// </summary>
    private const int DefaultMaxArity = 6;

    /// <summary>
    ///     Maps ErrorType enum value to (ConcreteResultType, HttpStatus, NeedsProblemPayload).
    ///     Index: 0=Failure, 1=Unexpected, 2=Validation, 3=Conflict, 4=NotFound, 5=Unauthorized, 6=Forbidden
    ///     STRICT MODE: Uses exact BCL types so OpenAPI schema matches runtime behavior.
    /// </summary>
    private static readonly (string TypeFqn, int Status, bool NeedsProblem)[] ErrorTypeMapping =
    [
        ($"{HttpResultsNs}.UnprocessableEntity<{ProblemDetailsType}>", 422, true), // 0: Failure
        ($"{HttpResultsNs}.InternalServerError<{ProblemDetailsType}>", 500, true), // 1: Unexpected
        ($"{HttpResultsNs}.ValidationProblem", 400, false), // 2: Validation → HttpValidationProblemDetails
        ($"{HttpResultsNs}.Conflict<{ProblemDetailsType}>", 409, true), // 3: Conflict
        ($"{HttpResultsNs}.NotFound<{ProblemDetailsType}>", 404, true), // 4: NotFound
        ($"{HttpResultsNs}.UnauthorizedHttpResult", 401, false), // 5: Unauthorized (no body)
        ($"{HttpResultsNs}.ForbidHttpResult", 403, false) // 6: Forbidden (no body)
    ];

    /// <summary>
    ///     Detects the maximum supported Results arity from the compilation.
    ///     Checks for Results`2 through Results`10 and returns the highest found.
    /// </summary>
    /// <param name="compilation">The compilation to check for Results types.</param>
    /// <returns>The maximum supported arity (2-10), or DefaultMaxArity if detection fails.</returns>
    public static int DetectMaxArity(Compilation compilation)
    {
        // Check from highest to lowest, return first found
        for (var arity = 10; arity >= 2; arity--)
        {
            var typeName = $"Microsoft.AspNetCore.Http.HttpResults.Results`{arity}";
            var type = compilation.GetTypeByMetadataName(typeName);
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
        string httpMethod,
        bool isNoContent,
        EquatableArray<int> inferredErrorTypes,
        EquatableArray<ProducesErrorInfo> declaredProducesErrors,
        int maxArity = DefaultMaxArity,
        bool isAcceptedResponse = false)
    {
        // Build ordered outcome set: Success → BadRequest (binding) → mapped ErrorTypes
        var unionTypes = new List<string>(8);
        var includedStatuses = new HashSet<int>();
        var inferredTypesInUnion = new List<int>();
        var hasCustomStatusCodes = false;

        // 1. Success type (always first in union)
        if (isNoContent)
        {
            unionTypes.Add($"{HttpResultsNs}.NoContent");
            includedStatuses.Add(204);
        }
        else if (isAcceptedResponse)
        {
            // 202 Accepted for async/queued operations (takes precedence over POST's 201)
            unionTypes.Add($"{HttpResultsNs}.Accepted<{successTypeFqn}>");
            includedStatuses.Add(202);
        }
        else if (httpMethod == WellKnownTypes.HttpMethod.Post)
        {
            unionTypes.Add($"{HttpResultsNs}.Created<{successTypeFqn}>");
            includedStatuses.Add(201);
        }
        else
        {
            unionTypes.Add($"{HttpResultsNs}.Ok<{successTypeFqn}>");
            includedStatuses.Add(200);
        }

        // 2. BadRequest<ProblemDetails> for binding failures (ALWAYS present - binding can fail for any endpoint)
        // This is a transport error, not a domain error, but must be in contract for OpenAPI truthfulness
        // STRICT: This produces ProblemDetails schema in OpenAPI (not HttpValidationProblemDetails)
        unionTypes.Add($"{HttpResultsNs}.BadRequest<{ProblemDetailsType}>");
        includedStatuses.Add(400);

        // 3. Add typed results for built-in ErrorTypes (stable, static outcomes)
        // STRICT: Each ErrorType maps to its exact BCL type for truthful OpenAPI schemas
        if (!inferredErrorTypes.IsDefaultOrEmpty)
        {
            foreach (var errorType in inferredErrorTypes.AsImmutableArray().Distinct().OrderBy(static x => x))
            {
                if (errorType >= 0 && errorType < ErrorTypeMapping.Length)
                {
                    var (typeFqn, status, _) = ErrorTypeMapping[errorType];

                    // STRICT: Validation (errorType 2) uses ValidationProblem which is ALSO 400
                    // but with HttpValidationProblemDetails schema. This is a DIFFERENT type than BadRequest.
                    // We add it to the union separately - BCL handles the status code collision correctly.
                    // OpenAPI will show 400 with HttpValidationProblemDetails schema (RFC 7807 validation format).
                    if (errorType == 2)
                    {
                        // ValidationProblem is its own type, add it even though status 400 is "already included"
                        // The union type will contain both BadRequest<ProblemDetails> AND ValidationProblem
                        unionTypes.Add(typeFqn);
                        inferredTypesInUnion.Add(errorType);
                        continue;
                    }

                    if (!includedStatuses.Contains(status))
                    {
                        unionTypes.Add(typeFqn);
                        includedStatuses.Add(status);
                        inferredTypesInUnion.Add(errorType);
                    }
                }
                else
                {
                    // Unknown ErrorType value - force fallback (can't statically enumerate)
                    hasCustomStatusCodes = true;
                }
            }
        }

        // 4. Check for custom status codes from [ProducesError] - these force fallback mode
        // Custom codes require ProblemHttpResult which is a dynamic bucket, poisoning union mode
        if (!declaredProducesErrors.IsDefaultOrEmpty)
        {
            foreach (var producesError in declaredProducesErrors)
            {
                if (!includedStatuses.Contains(producesError.StatusCode))
                    hasCustomStatusCodes = true;
            }
        }

        // 5. Decision: use union only when outcomes ≤ maxArity AND no dynamic/unknown outcomes
        var canUseUnion = unionTypes.Count >= 2 && unionTypes.Count <= maxArity && !hasCustomStatusCodes;

        if (!canUseUnion)
        {
            // Fallback: IResult + explicit metadata for all error responses
            var allStatuses = new List<int> { 400 }; // Always include binding failure

            if (!inferredErrorTypes.IsDefaultOrEmpty)
            {
                foreach (var errorType in inferredErrorTypes.AsImmutableArray().Distinct())
                {
                    if (errorType >= 0 && errorType < ErrorTypeMapping.Length)
                    {
                        var status = ErrorTypeMapping[errorType].Status;
                        if (!allStatuses.Contains(status))
                            allStatuses.Add(status);
                    }
                }
            }

            if (!declaredProducesErrors.IsDefaultOrEmpty)
            {
                foreach (var pe in declaredProducesErrors)
                {
                    if (!allStatuses.Contains(pe.StatusCode))
                        allStatuses.Add(pe.StatusCode);
                }
            }

            return new UnionTypeResult(
                false,
                $"{ResultsNs}.IResult",
                new EquatableArray<int>([.. allStatuses.Distinct().OrderBy(static x => x)]),
                hasCustomStatusCodes,
                default);
        }

        // 6. Build Results<T1, ..., Tn> type
        var resultsType = $"{HttpResultsNs}.Results<{string.Join(", ", unionTypes)}>";

        return new UnionTypeResult(
            true,
            resultsType,
            default, // Union types provide metadata automatically
            false,
            new EquatableArray<int>([.. inferredTypesInUnion]));
    }

    /// <summary>
    ///     Gets the success result factory expression.
    /// </summary>
    public static string GetSuccessFactory(string httpMethod, bool isNoContent, bool isAcceptedResponse = false)
    {
        if (isNoContent)
            return "TypedResults.NoContent()";
        if (isAcceptedResponse)
            return "TypedResults.Accepted(string.Empty, result.Value)";
        if (httpMethod == WellKnownTypes.HttpMethod.Post)
            return "TypedResults.Created(string.Empty, result.Value)";
        return "TypedResults.Ok(result.Value)";
    }

    /// <summary>
    ///     Gets the error result factory expression for a given ErrorType.
    /// </summary>
    public static string GetErrorFactory(int errorType)
    {
        return errorType switch
        {
            2 => "TypedResults.ValidationProblem(validationDict)", // Validation needs special handling
            5 => "TypedResults.Unauthorized()", // No body
            6 => "TypedResults.Forbid()", // No body
            _ => "TypedResults.Problem(problem)" // Others use ProblemDetails
        };
    }

    /// <summary>
    ///     Gets the ErrorType enum name from its integer value.
    /// </summary>
    public static string GetErrorTypeName(int errorType)
    {
        return errorType switch
        {
            0 => "Failure",
            1 => "Unexpected",
            2 => "Validation",
            3 => "Conflict",
            4 => "NotFound",
            5 => "Unauthorized",
            6 => "Forbidden",
            _ => "Failure"
        };
    }

    /// <summary>
    ///     Gets the HTTP status code for an ErrorType.
    /// </summary>
    public static int GetStatusCode(int errorType)
    {
        if (errorType >= 0 && errorType < ErrorTypeMapping.Length)
            return ErrorTypeMapping[errorType].Status;
        return 500;
    }

    /// <summary>
    ///     Checks if an ErrorType needs a ProblemDetails payload.
    /// </summary>
    public static bool NeedsProblemPayload(int errorType)
    {
        if (errorType >= 0 && errorType < ErrorTypeMapping.Length)
            return ErrorTypeMapping[errorType].NeedsProblem;
        return true;
    }

    /// <summary>
    ///     Result of union type computation.
    /// </summary>
    internal readonly record struct UnionTypeResult(
        bool CanUseUnion,
        string ReturnTypeFqn,
        EquatableArray<int> ExplicitProduceCodes,
        bool HasCustomStatusCodes,
        EquatableArray<int> InferredErrorTypesInUnion);
}
