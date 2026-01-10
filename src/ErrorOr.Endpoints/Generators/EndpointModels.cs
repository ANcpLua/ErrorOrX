using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;

namespace ErrorOr.Endpoints.Generators;

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
    SuccessKind Kind);

/// <summary>
///     Holds pre-computed method-level information shared across multiple attributes.
/// </summary>
internal readonly record struct MethodAnalysis(
    IMethodSymbol Method,
    ErrorOrReturnTypeInfo ReturnInfo,
    EquatableArray<int> InferredErrors,
    EquatableArray<CustomErrorInfo> InferredCustomErrors,
    EquatableArray<ProducesErrorInfo> ProducesErrors,
    bool IsAcceptedResponse);

internal readonly record struct EndpointDescriptor(
    string HttpMethod,
    string Pattern,
    string SuccessTypeFqn,
    SuccessKind SuccessKind,
    bool IsAsync,
    string HandlerContainingTypeFqn,
    string HandlerMethodName,
    EquatableArray<EndpointParameter> HandlerParameters,
    EquatableArray<int> InferredErrorTypes,
    EquatableArray<CustomErrorInfo> InferredCustomErrors,
    EquatableArray<ProducesErrorInfo> DeclaredProducesErrors,
    bool IsSse = false,
    string? SseItemTypeFqn = null,
    bool IsAcceptedResponse = false);

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
