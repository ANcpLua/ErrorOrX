using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ErrorOr.Generators;

/// <summary>
///     Represents a metadata entry for an endpoint.
/// </summary>
internal readonly record struct MetadataEntry(string Key, string Value);

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
///     Flags for parameter binding characteristics.
/// </summary>
[Flags]
internal enum ParameterFlags
{
    None = 0,
    FromServices = 1 << 0,
    FromKeyedServices = 1 << 1,
    FromBody = 1 << 2,
    FromRoute = 1 << 3,
    FromQuery = 1 << 4,
    FromHeader = 1 << 5,
    FromForm = 1 << 6,
    AsParameters = 1 << 7,
    Nullable = 1 << 8,
    NonNullableValueType = 1 << 9,
    Collection = 1 << 10,
    RequiresValidation = 1 << 11
}

/// <summary>
///     Special parameter kinds that have dedicated binding.
/// </summary>
internal enum SpecialParameterKind
{
    None,
    HttpContext,
    CancellationToken,
    FormFile,
    FormFileCollection,
    FormCollection,
    Stream,
    PipeReader
}

/// <summary>
///     Specifies how empty request bodies should be handled.
/// </summary>
internal enum EmptyBodyBehavior
{
    /// <summary>Framework default: Nullable allows empty, non-nullable rejects.</summary>
    Default,

    /// <summary>Empty bodies are valid (null/default assigned).</summary>
    Allow,

    /// <summary>Empty bodies are invalid (400 Bad Request).</summary>
    Disallow
}

/// <summary>
///     Represents a bound endpoint parameter with its source and type information.
/// </summary>
internal readonly record struct EndpointParameter(
    string Name,
    string TypeFqn,
    ParameterSource Source,
    string? KeyName,
    bool IsNullable,
    bool IsNonNullableValueType,
    bool IsCollection,
    string? CollectionItemTypeFqn,
    EquatableArray<EndpointParameter> Children,
    CustomBindingMethod CustomBinding = CustomBindingMethod.None,
    bool RequiresValidation = false,
    EmptyBodyBehavior EmptyBodyBehavior = EmptyBodyBehavior.Default,
    EquatableArray<ValidatablePropertyDescriptor> ValidatableProperties = default);

/// <summary>
///     Raw metadata extracted from a method parameter for binding classification.
/// </summary>
internal readonly record struct ParameterMeta(
    IParameterSymbol Symbol,
    string Name,
    string TypeFqn,
    RoutePrimitiveKind? RouteKind,
    ParameterFlags Flags,
    SpecialParameterKind SpecialKind,
    string? ServiceKey,
    string BoundName,
    string? CollectionItemTypeFqn,
    RoutePrimitiveKind? CollectionItemPrimitiveKind,
    CustomBindingMethod CustomBinding,
    EquatableArray<ValidatablePropertyDescriptor> ValidatableProperties = default)
{
    public bool HasFromBody => Flags.HasFlag(ParameterFlags.FromBody);
    public bool HasFromRoute => Flags.HasFlag(ParameterFlags.FromRoute);
    public bool HasFromQuery => Flags.HasFlag(ParameterFlags.FromQuery);
    public bool HasFromHeader => Flags.HasFlag(ParameterFlags.FromHeader);
    public bool HasFromForm => Flags.HasFlag(ParameterFlags.FromForm);
    public bool HasFromServices => Flags.HasFlag(ParameterFlags.FromServices);
    public bool HasFromKeyedServices => Flags.HasFlag(ParameterFlags.FromKeyedServices);
    public bool HasAsParameters => Flags.HasFlag(ParameterFlags.AsParameters);
    public bool IsNullable => Flags.HasFlag(ParameterFlags.Nullable);
    public bool IsNonNullableValueType => Flags.HasFlag(ParameterFlags.NonNullableValueType);
    public bool IsCollection => Flags.HasFlag(ParameterFlags.Collection);
    public bool RequiresValidation => Flags.HasFlag(ParameterFlags.RequiresValidation);

    public bool IsHttpContext => SpecialKind == SpecialParameterKind.HttpContext;
    public bool IsCancellationToken => SpecialKind == SpecialParameterKind.CancellationToken;
    public bool IsFormFile => SpecialKind == SpecialParameterKind.FormFile;
    public bool IsFormFileCollection => SpecialKind == SpecialParameterKind.FormFileCollection;
    public bool IsFormCollection => SpecialKind == SpecialParameterKind.FormCollection;
    public bool IsStream => SpecialKind == SpecialParameterKind.Stream;
    public bool IsPipeReader => SpecialKind == SpecialParameterKind.PipeReader;

    public bool HasExplicitBinding => (Flags & (
        ParameterFlags.FromBody | ParameterFlags.FromRoute | ParameterFlags.FromQuery |
        ParameterFlags.FromHeader | ParameterFlags.FromForm | ParameterFlags.FromServices |
        ParameterFlags.FromKeyedServices | ParameterFlags.AsParameters)) != ParameterFlags.None;

    /// <summary>Gets the keyed service key (legacy alias for ServiceKey).</summary>
    public string? KeyedServiceKey => ServiceKey;

    /// <summary>Gets bound name for route context.</summary>
    public string RouteName => BoundName;

    /// <summary>Gets bound name for query context.</summary>
    public string QueryName => BoundName;

    /// <summary>Gets bound name for header context.</summary>
    public string HeaderName => BoundName;

    /// <summary>Gets bound name for form context.</summary>
    public string FormName => BoundName;
}

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
    HttpVerb HttpVerb,
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
    RouteGroupInfo RouteGroup = default,
    EquatableArray<MetadataEntry> Metadata = default,
    string? CustomHttpMethod = null)
{
    /// <summary>Gets the HTTP method string for emission (e.g., "GET", "POST", or custom like "CONNECT").</summary>
    public string HttpMethod => CustomHttpMethod ?? HttpVerb.ToHttpString();

    /// <summary>
    ///     Returns true if any parameter is bound from body.
    /// </summary>
    public bool HasBodyParam
    {
        get
        {
            foreach (var p in HandlerParameters.AsImmutableArray())
            {
                if (p.Source == ParameterSource.Body)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    ///     Returns true if any parameter is bound from form-related sources.
    /// </summary>
    public bool HasFormParams
    {
        get
        {
            foreach (var p in HandlerParameters.AsImmutableArray())
            {
                if (p.Source.IsFormRelated)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    ///     Returns true if endpoint has body or form binding (for OpenAPI and validation).
    ///     Uses single-pass enumeration to avoid multiple iterations.
    /// </summary>
    public bool HasBodyOrFormBinding
    {
        get
        {
            foreach (var p in HandlerParameters.AsImmutableArray())
            {
                if (p.Source == ParameterSource.Body || p.Source.IsFormRelated)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    ///     Returns true if any parameter uses BindAsync custom binding.
    /// </summary>
    public bool HasBindAsyncParam
    {
        get
        {
            foreach (var p in HandlerParameters.AsImmutableArray())
            {
                if (p.CustomBinding is CustomBindingMethod.BindAsync or CustomBindingMethod.BindAsyncWithParam)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    ///     Returns true if any parameter requires DataAnnotations validation.
    /// </summary>
    public bool HasParameterValidation
    {
        get
        {
            foreach (var p in HandlerParameters.AsImmutableArray())
            {
                if (p.RequiresValidation)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Gets metadata value by key, or null if not found.</summary>
    public string? GetMetadata(string key)
    {
        foreach (var entry in Metadata.AsImmutableArray())
        {
            if (entry.Key == key)
            {
                return entry.Value;
            }
        }

        return null;
    }

    /// <summary>Returns true if metadata with the given key exists.</summary>
    public bool HasMetadata(string key)
    {
        foreach (var entry in Metadata.AsImmutableArray())
        {
            if (entry.Key == key)
            {
                return true;
            }
        }

        return false;
    }
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
    EquatableArray<int> ExplicitProduceCodes,
    bool UsesValidationProblemFor400 = false);

/// <summary>
///     Middleware configuration extracted from BCL attributes.
/// </summary>
internal readonly record struct MiddlewareInfo(
    bool RequiresAuthorization,
    EquatableArray<string> AuthorizationPolicies,
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
///     Represents a parameter for OpenAPI documentation.
/// </summary>
internal readonly record struct OpenApiParameterInfo(
    string Name,
    string Location,
    bool Required,
    string SchemaType,
    string? SchemaFormat);

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
    EquatableArray<(string ParamName, string Description)> ParameterDocs,
    EquatableArray<OpenApiParameterInfo> Parameters);

/// <summary>
///     Immutable type metadata for schema generation.
/// </summary>
internal readonly record struct TypeMetadataInfo(
    string TypeKey,
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
    bool IsDeprecated);

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
///     Route group configuration extracted from [RouteGroup] attribute on containing type.
/// </summary>
internal readonly record struct RouteGroupInfo(
    string? GroupPath,
    string? ApiName)
{
    /// <summary>
    ///     Returns true if route grouping is enabled for this endpoint.
    /// </summary>
    public bool HasRouteGroup => GroupPath is not null;
}

/// <summary>
///     Represents a validation attribute extracted from a property for the IValidatableInfoResolver emitter.
/// </summary>
/// <summary>
///     A named argument literal for a validation attribute (e.g., MinimumLength = 1).
/// </summary>
internal readonly record struct NamedArgLiteral(string Name, string Value);

/// <summary>
///     Represents a validation attribute extracted from a property for the IValidatableInfoResolver emitter.
/// </summary>
internal readonly record struct ValidatableAttributeInfo(
    string AttributeTypeFqn,
    EquatableArray<string> ConstructorArgLiterals,
    EquatableArray<NamedArgLiteral> NamedArgLiterals);

/// <summary>
///     Represents a property on a validatable type, with its validation attribute metadata.
/// </summary>
internal readonly record struct ValidatablePropertyDescriptor(
    string Name,
    string TypeFqn,
    string DisplayName,
    EquatableArray<ValidatableAttributeInfo> ValidationAttributes);

/// <summary>
///     Represents a type that requires validation, along with its validatable properties.
/// </summary>
internal readonly record struct ValidatableTypeDescriptor(
    string TypeFqn,
    EquatableArray<ValidatablePropertyDescriptor> Properties);

/// <summary>
///     Well-known metadata key constants.
/// </summary>
internal static class MetadataKeys
{
    public const string Deprecated = "erroror:deprecated";
    public const string DeprecatedMessage = "erroror:deprecated-message";
    public const string OpenApiExtension = "openapi:x-";
    public const string CustomTag = "openapi:tag";
}
