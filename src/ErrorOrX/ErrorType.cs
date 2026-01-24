namespace ErrorOr;

/// <summary>
///     Error types that map to HTTP status codes.
/// </summary>
public enum ErrorType
{
    /// <summary>General failure (500 Internal Server Error).</summary>
    Failure,

    /// <summary>Unexpected error (500 Internal Server Error).</summary>
    Unexpected,

    /// <summary>Validation error (400 Bad Request).</summary>
    Validation,

    /// <summary>Conflict error (409 Conflict).</summary>
    Conflict,

    /// <summary>Not found error (404 Not Found).</summary>
    NotFound,

    /// <summary>Unauthorized error (401 Unauthorized).</summary>
    Unauthorized,

    /// <summary>Forbidden error (403 Forbidden).</summary>
    Forbidden
}
