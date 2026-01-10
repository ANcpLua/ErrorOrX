namespace ErrorOr.Core.Errors;

/// <summary>
///     Error types.
///     Note: Enum values are NOT HTTP status codes because Failure/Unexpected both map to 500.
///     Use Error.NumericType or explicit mapping for HTTP status.
/// </summary>
public enum ErrorType
{
    Failure,
    Unexpected,
    Validation,
    Conflict,
    NotFound,
    Unauthorized,
    Forbidden
}