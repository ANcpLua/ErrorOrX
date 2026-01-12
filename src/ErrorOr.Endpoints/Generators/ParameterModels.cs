using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;

namespace ErrorOr.Endpoints.Generators;

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
