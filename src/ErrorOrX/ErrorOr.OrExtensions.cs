namespace ErrorOr;

/// <summary>
///     Extension methods for converting nullable values to ErrorOr with specific error types.
/// </summary>
public static class ErrorOrOrExtensions
{
    /// <summary>
    ///     Returns the value if not null; otherwise returns a NotFound error.
    ///     The error code is auto-generated from the type name (e.g., "Todo.NotFound").
    /// </summary>
    public static ErrorOr<TValue> OrNotFound<TValue>(
        this TValue? value,
        string? description = null) where TValue : class
        => value is not null ? value : Error.NotFound($"{typeof(TValue).Name}.NotFound", description ?? $"{typeof(TValue).Name} not found");

    /// <summary>
    ///     Returns the value if not null; otherwise returns a NotFound error.
    /// </summary>
    public static ErrorOr<TValue> OrNotFound<TValue>(
        this TValue? value,
        string? description = null) where TValue : struct
        => value.HasValue ? value.Value : Error.NotFound($"{typeof(TValue).Name}.NotFound", description ?? $"{typeof(TValue).Name} not found");

    /// <summary>
    ///     Returns the value if not null; otherwise returns a Validation error.
    /// </summary>
    public static ErrorOr<TValue> OrValidation<TValue>(
        this TValue? value,
        string? description = null) where TValue : class
        => value is not null ? value : Error.Validation($"{typeof(TValue).Name}.Invalid", description ?? $"{typeof(TValue).Name} is invalid");

    /// <summary>
    ///     Returns the value if not null; otherwise returns a Validation error.
    /// </summary>
    public static ErrorOr<TValue> OrValidation<TValue>(
        this TValue? value,
        string? description = null) where TValue : struct
        => value.HasValue ? value.Value : Error.Validation($"{typeof(TValue).Name}.Invalid", description ?? $"{typeof(TValue).Name} is invalid");

    /// <summary>
    ///     Returns the value if not null; otherwise returns an Unauthorized error.
    /// </summary>
    public static ErrorOr<TValue> OrUnauthorized<TValue>(
        this TValue? value,
        string? description = null) where TValue : class
        => value is not null ? value : Error.Unauthorized($"{typeof(TValue).Name}.Unauthorized", description ?? "Unauthorized");

    /// <summary>
    ///     Returns the value if not null; otherwise returns an Unauthorized error.
    /// </summary>
    public static ErrorOr<TValue> OrUnauthorized<TValue>(
        this TValue? value,
        string? description = null) where TValue : struct
        => value.HasValue ? value.Value : Error.Unauthorized($"{typeof(TValue).Name}.Unauthorized", description ?? "Unauthorized");

    /// <summary>
    ///     Returns the value if not null; otherwise returns a Forbidden error.
    /// </summary>
    public static ErrorOr<TValue> OrForbidden<TValue>(
        this TValue? value,
        string? description = null) where TValue : class
        => value is not null ? value : Error.Forbidden($"{typeof(TValue).Name}.Forbidden", description ?? "Forbidden");

    /// <summary>
    ///     Returns the value if not null; otherwise returns a Forbidden error.
    /// </summary>
    public static ErrorOr<TValue> OrForbidden<TValue>(
        this TValue? value,
        string? description = null) where TValue : struct
        => value.HasValue ? value.Value : Error.Forbidden($"{typeof(TValue).Name}.Forbidden", description ?? "Forbidden");

    /// <summary>
    ///     Returns the value if not null; otherwise returns a Conflict error.
    /// </summary>
    public static ErrorOr<TValue> OrConflict<TValue>(
        this TValue? value,
        string? description = null) where TValue : class
        => value is not null ? value : Error.Conflict($"{typeof(TValue).Name}.Conflict", description ?? $"{typeof(TValue).Name} conflict");

    /// <summary>
    ///     Returns the value if not null; otherwise returns a Conflict error.
    /// </summary>
    public static ErrorOr<TValue> OrConflict<TValue>(
        this TValue? value,
        string? description = null) where TValue : struct
        => value.HasValue ? value.Value : Error.Conflict($"{typeof(TValue).Name}.Conflict", description ?? $"{typeof(TValue).Name} conflict");

    /// <summary>
    ///     Returns the value if not null; otherwise returns a Failure error.
    /// </summary>
    public static ErrorOr<TValue> OrFailure<TValue>(
        this TValue? value,
        string? description = null) where TValue : class
        => value is not null ? value : Error.Failure($"{typeof(TValue).Name}.Failure", description ?? $"{typeof(TValue).Name} operation failed");

    /// <summary>
    ///     Returns the value if not null; otherwise returns a Failure error.
    /// </summary>
    public static ErrorOr<TValue> OrFailure<TValue>(
        this TValue? value,
        string? description = null) where TValue : struct
        => value.HasValue ? value.Value : Error.Failure($"{typeof(TValue).Name}.Failure", description ?? $"{typeof(TValue).Name} operation failed");

    /// <summary>
    ///     Returns the value if not null; otherwise returns an Unexpected error.
    /// </summary>
    public static ErrorOr<TValue> OrUnexpected<TValue>(
        this TValue? value,
        string? description = null) where TValue : class
        => value is not null ? value : Error.Unexpected($"{typeof(TValue).Name}.Unexpected", description ?? "An unexpected error occurred");

    /// <summary>
    ///     Returns the value if not null; otherwise returns an Unexpected error.
    /// </summary>
    public static ErrorOr<TValue> OrUnexpected<TValue>(
        this TValue? value,
        string? description = null) where TValue : struct
        => value.HasValue ? value.Value : Error.Unexpected($"{typeof(TValue).Name}.Unexpected", description ?? "An unexpected error occurred");

    /// <summary>
    ///     Returns the value if not null; otherwise returns the specified error.
    /// </summary>
    public static ErrorOr<TValue> OrError<TValue>(
        this TValue? value,
        Error error) where TValue : class
        => value is not null ? value : error;

    /// <summary>
    ///     Returns the value if not null; otherwise returns the specified error.
    /// </summary>
    public static ErrorOr<TValue> OrError<TValue>(
        this TValue? value,
        Error error) where TValue : struct
        => value.HasValue ? value.Value : error;

    /// <summary>
    ///     Returns the value if not null; otherwise invokes the error factory.
    ///     Use this when error creation is expensive or requires computation.
    /// </summary>
    public static ErrorOr<TValue> OrError<TValue>(
        this TValue? value,
        Func<Error> errorFactory) where TValue : class
        => value is not null ? value : errorFactory();

    /// <summary>
    ///     Returns the value if not null; otherwise invokes the error factory.
    ///     Use this when error creation is expensive or requires computation.
    /// </summary>
    public static ErrorOr<TValue> OrError<TValue>(
        this TValue? value,
        Func<Error> errorFactory) where TValue : struct
        => value.HasValue ? value.Value : errorFactory();
}
