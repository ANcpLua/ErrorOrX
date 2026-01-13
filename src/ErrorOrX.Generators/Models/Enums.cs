namespace ErrorOr.Generators;

/// <summary>
/// Specifies where an endpoint parameter value is bound from.
/// </summary>
internal enum EndpointParameterSource
{
    /// <summary>Parameter bound from route template segment.</summary>
    Route,

    /// <summary>Parameter bound from request body (JSON).</summary>
    Body,

    /// <summary>Parameter bound from query string.</summary>
    Query,

    /// <summary>Parameter bound from HTTP header.</summary>
    Header,

    /// <summary>Parameter resolved from DI container.</summary>
    Service,

    /// <summary>Parameter resolved from DI container by key.</summary>
    KeyedService,

    /// <summary>Parameter expanded from [AsParameters] type.</summary>
    AsParameters,

    /// <summary>HttpContext injected directly.</summary>
    HttpContext,

    /// <summary>CancellationToken for request cancellation.</summary>
    CancellationToken,

    /// <summary>Parameter bound from form field.</summary>
    Form,

    /// <summary>Single file upload (IFormFile).</summary>
    FormFile,

    /// <summary>Multiple file uploads (IFormFileCollection).</summary>
    FormFiles,

    /// <summary>Entire form collection (IFormCollection).</summary>
    FormCollection,

    /// <summary>Raw request body stream.</summary>
    Stream,

    /// <summary>Request body as PipeReader.</summary>
    PipeReader
}

/// <summary>
/// Represents the custom binding method detected on a parameter type.
/// </summary>
internal enum CustomBindingMethod
{
    /// <summary>No custom binding method detected.</summary>
    None,

    /// <summary>Type has static TryParse(string, out T) method.</summary>
    TryParse,

    /// <summary>Type has static TryParse(string, IFormatProvider, out T) method.</summary>
    TryParseWithFormat,

    /// <summary>Type has static BindAsync(HttpContext) method.</summary>
    BindAsync,

    /// <summary>Type has static BindAsync(HttpContext, ParameterInfo) method.</summary>
    BindAsyncWithParam,

    /// <summary>Type implements IBindableFromHttpContext&lt;T&gt;.</summary>
    Bindable
}

/// <summary>
/// Primitive types that can be bound from route templates.
/// </summary>
/// <remarks>
/// These types are natively supported by ASP.NET Core route binding.
/// Each has implicit TryParse support in the routing infrastructure.
/// </remarks>
internal enum RoutePrimitiveKind
{
    /// <summary>System.String - default for untyped route segments.</summary>
    String,
    /// <summary>System.Int32 (int) - {id:int}.</summary>
    Int32,
    /// <summary>System.Int64 (long) - {id:long}.</summary>
    Int64,
    /// <summary>System.Int16 (short).</summary>
    Int16,
    /// <summary>System.UInt32 (uint).</summary>
    UInt32,
    /// <summary>System.UInt64 (ulong).</summary>
    UInt64,
    /// <summary>System.UInt16 (ushort).</summary>
    UInt16,
    /// <summary>System.Byte.</summary>
    Byte,
    /// <summary>System.SByte.</summary>
    SByte,
    /// <summary>System.Boolean (bool) - {active:bool}.</summary>
    Boolean,
    /// <summary>System.Decimal - {price:decimal}.</summary>
    Decimal,
    /// <summary>System.Double - {rate:double}.</summary>
    Double,
    /// <summary>System.Single (float) - {rate:float}.</summary>
    Single,
    /// <summary>System.Guid - {id:guid}.</summary>
    Guid,
    /// <summary>System.DateTime - {date:datetime}.</summary>
    DateTime,
    /// <summary>System.DateTimeOffset.</summary>
    DateTimeOffset,
    /// <summary>System.DateOnly.</summary>
    DateOnly,
    /// <summary>System.TimeOnly.</summary>
    TimeOnly,
    /// <summary>System.TimeSpan.</summary>
    TimeSpan
}

/// <summary>
/// Classifies the success response type for HTTP status code mapping.
/// </summary>
internal enum SuccessKind
{
    /// <summary>Returns a value payload (200 OK or 201 Created for POST).</summary>
    Payload,

    /// <summary>Success marker without payload (200 OK).</summary>
    Success,

    /// <summary>Created marker (201 Created).</summary>
    Created,

    /// <summary>Updated marker (204 No Content).</summary>
    Updated,

    /// <summary>Deleted marker (204 No Content).</summary>
    Deleted
}