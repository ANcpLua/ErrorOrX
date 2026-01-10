namespace ErrorOr.Endpoints.Generators;

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

internal enum ErrorType
{
    // Names match ErrorOr.Core.Errors.ErrorType
    // Values are internal IDs, NOT HTTP status codes (because Failure/Unexpected both map to 500)
    Failure,
    Unexpected,
    Validation,
    Conflict,
    NotFound,
    Unauthorized,
    Forbidden
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

internal enum SuccessKind
{
    Payload,
    Success,
    Created,
    Updated,
    Deleted
}
