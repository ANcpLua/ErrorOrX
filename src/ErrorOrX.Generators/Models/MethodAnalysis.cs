namespace ErrorOr.Generators;

/// <summary>
///     Represents a custom error detected via Error.Custom() call.
/// </summary>
internal readonly record struct CustomErrorInfo(
    string ErrorCode);

/// <summary>
///     Represents a [ProducesError] attribute on an endpoint method.
/// </summary>
internal readonly record struct ProducesErrorInfo(
    int StatusCode);

/// <summary>
///     Result of extracting the ErrorOr return type, including SSE detection.
/// </summary>
internal readonly record struct ErrorOrReturnTypeInfo(
    string? SuccessTypeFqn,
    bool IsAsync,
    bool IsSse,
    string? SseItemTypeFqn,
    SuccessKind Kind,
    string? IdPropertyName = null,
    bool IsObjectReturn = false,
    bool IsInaccessibleType = false,
    string? InaccessibleTypeName = null,
    string? InaccessibleTypeAccessibility = null,
    bool IsTypeParameter = false,
    string? TypeParameterName = null);

/// <summary>
///     Pre-computed method-level analysis shared across multiple HTTP method attributes.
/// </summary>
internal readonly struct MethodAnalysis(
    ErrorOrReturnTypeInfo returnInfo,
    EquatableArray<string> inferredErrorTypeNames,
    EquatableArray<CustomErrorInfo> inferredCustomErrors,
    EquatableArray<ProducesErrorInfo> producesErrors,
    bool isAcceptedResponse,
    MiddlewareInfo middleware)
{
    public ErrorOrReturnTypeInfo ReturnInfo { get; } = returnInfo;
    public EquatableArray<string> InferredErrorTypeNames { get; } = inferredErrorTypeNames;
    public EquatableArray<CustomErrorInfo> InferredCustomErrors { get; } = inferredCustomErrors;
    public EquatableArray<ProducesErrorInfo> ProducesErrors { get; } = producesErrors;
    public bool IsAcceptedResponse { get; } = isAcceptedResponse;
    public MiddlewareInfo Middleware { get; } = middleware;
}

/// <summary>
///     SSE (Server-Sent Events) configuration for an endpoint.
/// </summary>
internal readonly record struct SseInfo(
    bool IsSse,
    string? SseItemTypeFqn);

/// <summary>
///     Error inference results for an endpoint.
/// </summary>
internal readonly record struct ErrorInferenceInfo(
    EquatableArray<string> InferredErrorTypeNames,
    EquatableArray<CustomErrorInfo> InferredCustomErrors,
    EquatableArray<ProducesErrorInfo> DeclaredProducesErrors);
