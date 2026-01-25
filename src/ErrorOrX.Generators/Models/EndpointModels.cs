using System.Collections.Immutable;
using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;

namespace ErrorOr.Generators;

/// <summary>
///     Specifies where an endpoint parameter value is bound from.
/// </summary>
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

/// <summary>
///     Primitive types that can be bound from route templates.
/// </summary>
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
///     Classifies the success response type for HTTP status code mapping.
/// </summary>
internal enum SuccessKind
{
    Payload,
    Success,
    Created,
    Updated,
    Deleted
}

/// <summary>
///     Represents a bound endpoint parameter with its source and type information.
/// </summary>
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

/// <summary>
///     Raw metadata extracted from a method parameter for binding classification.
/// </summary>
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
    bool IsAnonymousType = false,
    bool IsInaccessibleType = false,
    string? InaccessibleTypeName = null,
    string? InaccessibleTypeAccessibility = null,
    bool IsTypeParameter = false,
    string? TypeParameterName = null);

/// <summary>
///     Pre-computed method-level analysis shared across multiple HTTP method attributes.
/// </summary>
internal readonly record struct MethodAnalysis(
    IMethodSymbol Method,
    ErrorOrReturnTypeInfo ReturnInfo,
    EquatableArray<string> InferredErrorTypeNames,
    EquatableArray<CustomErrorInfo> InferredCustomErrors,
    EquatableArray<ProducesErrorInfo> ProducesErrors,
    bool IsAcceptedResponse,
    MiddlewareInfo Middleware);

/// <summary>
///     Complete descriptor for an ErrorOr endpoint used for code generation.
/// </summary>
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
    MiddlewareInfo Middleware = default,
    VersioningInfo Versioning = default,
    RouteGroupInfo RouteGroup = default)
{
    /// <summary>
    ///     Returns true if any parameter is bound from body.
    /// </summary>
    private bool HasBodyParam => HandlerParameters.AsImmutableArray().Any(static p => p.Source == EndpointParameterSource.Body);

    /// <summary>
    ///     Returns true if any parameter is bound from form-related sources.
    /// </summary>
    public bool HasFormParams => HandlerParameters.AsImmutableArray().Any(static p =>
        p.Source is EndpointParameterSource.Form or EndpointParameterSource.FormFile
            or EndpointParameterSource.FormFiles or EndpointParameterSource.FormCollection);

    /// <summary>
    ///     Returns true if endpoint has body or form binding (for OpenAPI and validation).
    /// </summary>
    public bool HasBodyOrFormBinding => HasBodyParam || HasFormParams;

    /// <summary>
    ///     Returns true if any parameter uses BindAsync custom binding.
    /// </summary>
    public bool HasBindAsyncParam => HandlerParameters.AsImmutableArray().Any(static p =>
        p.CustomBinding is CustomBindingMethod.BindAsync or CustomBindingMethod.BindAsyncWithParam);
}

/// <summary>
///     Success response information for OpenAPI metadata.
/// </summary>
internal readonly record struct SuccessResponseInfo(
    string ResultTypeFqn,
    int StatusCode,
    bool HasBody,
    string Factory);

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
    public bool HasAny =>
        RequiresAuthorization || AllowAnonymous ||
        EnableRateLimiting || DisableRateLimiting ||
        EnableOutputCache ||
        EnableCors || DisableCors;
}

/// <summary>
///     Information about a route parameter extracted from the route template.
/// </summary>
internal readonly record struct RouteParameterInfo(
    string Name,
    string? Constraint,
    bool IsOptional,
    bool IsCatchAll);

/// <summary>
///     Information about a method parameter relevant to route binding validation.
/// </summary>
internal readonly record struct RouteMethodParameterInfo(
    string Name,
    string? BoundRouteName,
    string? TypeFqn,
    bool IsNullable);

/// <summary>
///     Result of parameter binding analysis.
/// </summary>
internal readonly record struct ParameterBindingResult(bool IsValid, ImmutableArray<EndpointParameter> Parameters)
{
    public static readonly ParameterBindingResult Empty = new(true, ImmutableArray<EndpointParameter>.Empty);
    public static readonly ParameterBindingResult Invalid = new(false, ImmutableArray<EndpointParameter>.Empty);
}

/// <summary>
///     Information about a user-defined JsonSerializerContext.
/// </summary>
internal readonly record struct JsonContextInfo(
    string ClassName,
    string? Namespace,
    EquatableArray<string> SerializableTypes,
    bool HasCamelCasePolicy);

/// <summary>
///     Immutable endpoint info for OpenAPI generation.
/// </summary>
internal readonly record struct OpenApiEndpointInfo(
    string OperationId,
    string TagName,
    string? Summary,
    string? Description,
    string HttpMethod,
    string Pattern,
    EquatableArray<(string ParamName, string Description)> ParameterDocs);

/// <summary>
///     Immutable type metadata for schema generation.
/// </summary>
internal readonly record struct TypeMetadataInfo(string TypeKey,
    string Description);

/// <summary>
///     Result of route binding analysis containing bound parameters and route-specific extraction.
/// </summary>
internal readonly record struct RouteBindingAnalysis(
    EquatableArray<EndpointParameter> Parameters,
    EquatableArray<RouteMethodParameterInfo> RouteParameters);

/// <summary>
///     Represents a single API version extracted from [ApiVersion] attribute.
/// </summary>
internal readonly record struct ApiVersionInfo(
    int MajorVersion,
    int? MinorVersion,
    string? Status,
    bool IsDeprecated)
{
    /// <summary>
    ///     Returns the version string (e.g., "1.0", "2", "1.0-beta").
    /// </summary>
    public string ToVersionString()
    {
        var version = MinorVersion.HasValue
            ? $"{MajorVersion}.{MinorVersion.Value}"
            : MajorVersion.ToString();

        return string.IsNullOrEmpty(Status) ? version : $"{version}-{Status}";
    }
}

/// <summary>
///     API versioning configuration extracted from endpoint class or method.
/// </summary>
internal readonly record struct VersioningInfo(
    EquatableArray<ApiVersionInfo> SupportedVersions,
    EquatableArray<ApiVersionInfo> MappedVersions,
    bool IsVersionNeutral)
{
    /// <summary>
    ///     Returns true if any versioning attributes were found.
    /// </summary>
    public bool HasVersioning => !SupportedVersions.IsDefaultOrEmpty || IsVersionNeutral;

    /// <summary>
    ///     Returns the versions this endpoint should be mapped to.
    ///     Uses MappedVersions if specified, otherwise falls back to SupportedVersions.
    /// </summary>
    public EquatableArray<ApiVersionInfo> EffectiveVersions =>
        MappedVersions.IsDefaultOrEmpty ? SupportedVersions : MappedVersions;
}

/// <summary>
///     Aggregated version set information for all endpoints.
/// </summary>
internal readonly record struct GlobalVersionSet(
    EquatableArray<ApiVersionInfo> AllVersions,
    bool HasVersionNeutralEndpoints);

/// <summary>
///     Classifies how an endpoint relates to API versioning for route group emission.
/// </summary>
internal enum VersionScope
{
    /// <summary>No versioning attributes present.</summary>
    None,

    /// <summary>Available on all declared versions (no [MapToApiVersion]).</summary>
    AllVersions,

    /// <summary>Only specific versions via [MapToApiVersion].</summary>
    Specific,

    /// <summary>Version-neutral via [ApiVersionNeutral].</summary>
    Neutral
}

/// <summary>
///     Route group configuration extracted from [RouteGroup] attribute on containing type.
/// </summary>
internal readonly record struct RouteGroupInfo(
    string? GroupPath,
    string? ApiName,
    bool UseVersionedApi)
{
    /// <summary>
    ///     Returns true if route grouping is enabled for this endpoint.
    /// </summary>
    public bool HasRouteGroup => GroupPath is not null;
}
