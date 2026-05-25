namespace ErrorOr.Generators;

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
internal readonly struct ParameterMeta(
    string name,
    string typeFqn,
    RoutePrimitiveKind? routeKind,
    ParameterFlags flags,
    SpecialParameterKind specialKind,
    string? serviceKey,
    string boundName,
    string? collectionItemTypeFqn,
    RoutePrimitiveKind? collectionItemPrimitiveKind,
    CustomBindingMethod customBinding,
    EmptyBodyBehavior emptyBodyBehavior = EmptyBodyBehavior.Default,
    EquatableArray<ValidatablePropertyDescriptor> validatableProperties = default)
{
    public string Name { get; } = name;
    public string TypeFqn { get; } = typeFqn;
    public RoutePrimitiveKind? RouteKind { get; } = routeKind;
    public ParameterFlags Flags { get; } = flags;
    public SpecialParameterKind SpecialKind { get; } = specialKind;
    public string? ServiceKey { get; } = serviceKey;
    public string BoundName { get; } = boundName;
    public string? CollectionItemTypeFqn { get; } = collectionItemTypeFqn;
    public RoutePrimitiveKind? CollectionItemPrimitiveKind { get; } = collectionItemPrimitiveKind;
    public CustomBindingMethod CustomBinding { get; } = customBinding;
    public EmptyBodyBehavior EmptyBodyBehavior { get; } = emptyBodyBehavior;
    public EquatableArray<ValidatablePropertyDescriptor> ValidatableProperties { get; } = validatableProperties;

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
}

/// <summary>
///     Result of parameter binding analysis.
/// </summary>
internal readonly record struct ParameterBindingResult(bool IsValid, EquatableArray<EndpointParameter> Parameters)
{
    public static readonly ParameterBindingResult Empty = new(true, default);
    public static readonly ParameterBindingResult Invalid = new(false, default);
}
