using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;

namespace ErrorOr.Generators;

#region Parameter Models

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
    CustomBindingMethod CustomBinding = CustomBindingMethod.None,
    bool RequiresValidation = false);

internal readonly record struct ParameterMeta(
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
    CustomBindingMethod CustomBinding,
    bool RequiresValidation = false);

#endregion

#region Endpoint Models

/// <summary>
///     Represents a custom error detected via Error.Custom() call.
/// </summary>
internal readonly record struct CustomErrorInfo(
    int StatusCode,
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
    string? IdPropertyName = null);

/// <summary>
///     Holds pre-computed method-level information shared across multiple attributes.
/// </summary>
internal readonly record struct MethodAnalysis(
    IMethodSymbol Method,
    ErrorOrReturnTypeInfo ReturnInfo,
    EquatableArray<string> InferredErrorTypeNames,
    EquatableArray<CustomErrorInfo> InferredCustomErrors,
    EquatableArray<ProducesErrorInfo> ProducesErrors,
    bool IsAcceptedResponse,
    MiddlewareInfo Middleware);

internal readonly record struct EndpointDescriptor(
    string HttpMethod,
    string Pattern,
    string SuccessTypeFqn,
    SuccessKind SuccessKind,
    bool IsAsync,
    string HandlerContainingTypeFqn,
    string HandlerMethodName,
    EquatableArray<EndpointParameter> HandlerParameters,
    EquatableArray<string> InferredErrorTypeNames,
    EquatableArray<CustomErrorInfo> InferredCustomErrors,
    EquatableArray<ProducesErrorInfo> DeclaredProducesErrors,
    bool IsSse = false,
    string? SseItemTypeFqn = null,
    bool IsAcceptedResponse = false,
    string? LocationIdPropertyName = null,
    MiddlewareInfo Middleware = default);

internal readonly record struct EndpointData(
    EquatableArray<EndpointDescriptor> Descriptors)
{
    public static EndpointData Empty =>
        new(default);
}

/// <summary>
///     Success response information for OpenAPI metadata.
/// </summary>
internal readonly record struct SuccessResponseInfo(
    string ResultTypeFqn,
    int StatusCode,
    bool HasBody,
    string Factory,
    string MatchFactory);

/// <summary>
///     Result of union type computation.
/// </summary>
internal readonly record struct UnionTypeResult(
    bool CanUseUnion,
    string ReturnTypeFqn,
    EquatableArray<int> ExplicitProduceCodes);

/// <summary>
///     Middleware configuration extracted from BCL attributes.
/// </summary>
internal readonly record struct MiddlewareInfo(
    bool RequiresAuthorization,
    string? AuthorizationPolicy,
    bool AllowAnonymous,
    bool EnableRateLimiting,
    string? RateLimitingPolicy,
    bool DisableRateLimiting,
    bool EnableOutputCache,
    string? OutputCachePolicy,
    int? OutputCacheDuration,
    bool EnableCors,
    string? CorsPolicy,
    bool DisableCors)
{
    public static MiddlewareInfo Empty => default;

    public bool HasAny =>
        RequiresAuthorization || AllowAnonymous ||
        EnableRateLimiting || DisableRateLimiting ||
        EnableOutputCache ||
        EnableCors || DisableCors;
}

#endregion