namespace ErrorOr.Generators;

/// <summary>
///     Single source of truth for ErrorType → HTTP mapping.
///     All error-to-status logic MUST derive from this class.
/// </summary>
internal static class ErrorMapping
{
    private const string HttpResultsNs = "global::Microsoft.AspNetCore.Http.HttpResults";
    private const string ProblemDetailsType = "global::Microsoft.AspNetCore.Mvc.ProblemDetails";

    /// <summary>
    ///     All ErrorType → Entry mappings. This is the CANONICAL source.
    /// </summary>
    private static readonly Dictionary<ErrorType, Entry> Mappings = new()
    {
        // 4xx Client Errors
        [ErrorType.Validation] = new Entry($"{HttpResultsNs}.ValidationProblem", 400, false),
        [ErrorType.Unauthorized] = new Entry($"{HttpResultsNs}.UnauthorizedHttpResult", 401, false),
        [ErrorType.Forbidden] = new Entry($"{HttpResultsNs}.ForbidHttpResult", 403, false),
        [ErrorType.NotFound] = new Entry($"{HttpResultsNs}.NotFound<{ProblemDetailsType}>", 404, true),
        [ErrorType.Conflict] = new Entry($"{HttpResultsNs}.Conflict<{ProblemDetailsType}>", 409, true),
        // 5xx Server Errors (RFC 9110 §15.6.1)
        [ErrorType.Failure] = new Entry($"{HttpResultsNs}.InternalServerError<{ProblemDetailsType}>", 500, true),
        [ErrorType.Unexpected] = new Entry($"{HttpResultsNs}.InternalServerError<{ProblemDetailsType}>", 500, true)
    };

    /// <summary>
    ///     Default entry for unknown ErrorTypes (500 Internal Server Error).
    /// </summary>
    private static readonly Entry DefaultEntry = new(
        $"{HttpResultsNs}.InternalServerError<{ProblemDetailsType}>",
        500,
        true);

    /// <summary>
    ///     Gets all defined ErrorTypes in canonical order for code generation.
    /// </summary>
    public static IEnumerable<ErrorType> AllErrorTypes => Mappings.Keys;

    /// <summary>
    ///     Gets the mapping entry for an ErrorType.
    /// </summary>
    public static Entry Get(ErrorType errorType)
    {
        return Mappings.TryGetValue(errorType, out var entry) ? entry : DefaultEntry;
    }

    /// <summary>
    ///     Gets the HTTP status code for an ErrorType.
    /// </summary>
    public static int GetStatusCode(ErrorType errorType)
    {
        return Get(errorType).StatusCode;
    }

    /// <summary>
    ///     Generates the switch expression body for ErrorType → Status mapping.
    ///     Derives from the canonical mappings.
    /// </summary>
    public static string GenerateStatusSwitch(string errorTypeFqn)
    {
        var cases = Mappings
            .Select(kvp => $"{errorTypeFqn}.{kvp.Key} => {kvp.Value.StatusCode}");
        return string.Join(", ", cases) + ", _ => first.NumericType is >= 100 and <= 599 ? first.NumericType : 500";
    }

    /// <summary>
    ///     Maps a custom error numeric type to the appropriate TypedResults factory.
    /// </summary>
    public static CustomEntry GetCustom(int numericType)
    {
        return numericType switch
        {
            400 => new CustomEntry("global::Microsoft.AspNetCore.Http.TypedResults.BadRequest(problem)",
                $"{HttpResultsNs}.BadRequest<{ProblemDetailsType}>", true),
            401 => new CustomEntry("global::Microsoft.AspNetCore.Http.TypedResults.Unauthorized()", $"{HttpResultsNs}.UnauthorizedHttpResult", false),
            403 => new CustomEntry("global::Microsoft.AspNetCore.Http.TypedResults.Forbid()", $"{HttpResultsNs}.ForbidHttpResult", false),
            404 => new CustomEntry("global::Microsoft.AspNetCore.Http.TypedResults.NotFound(problem)", $"{HttpResultsNs}.NotFound<{ProblemDetailsType}>",
                true),
            409 => new CustomEntry("global::Microsoft.AspNetCore.Http.TypedResults.Conflict(problem)", $"{HttpResultsNs}.Conflict<{ProblemDetailsType}>",
                true),
            422 => new CustomEntry("global::Microsoft.AspNetCore.Http.TypedResults.UnprocessableEntity(problem)",
                $"{HttpResultsNs}.UnprocessableEntity<{ProblemDetailsType}>", true),
            500 => new CustomEntry("global::Microsoft.AspNetCore.Http.TypedResults.InternalServerError(problem)",
                $"{HttpResultsNs}.InternalServerError<{ProblemDetailsType}>", true),
            // Fallback to Problem() for all other valid HTTP status codes
            >= 400 and < 600 => new CustomEntry(
                $"global::Microsoft.AspNetCore.Http.TypedResults.Problem(detail: first.Description, statusCode: {numericType}, title: \"{StatusCodeTitles.Get(numericType)}\")",
                $"{HttpResultsNs}.ProblemHttpResult", true),
            // Invalid status code → default to 500
            _ => new CustomEntry("global::Microsoft.AspNetCore.Http.TypedResults.InternalServerError(problem)",
                $"{HttpResultsNs}.InternalServerError<{ProblemDetailsType}>", true)
        };
    }

    /// <summary>
    ///     Entry representing an ErrorType mapping.
    /// </summary>
    internal readonly record struct Entry(
        string TypeFqn,
        int StatusCode,
        bool NeedsProblem);

    /// <summary>
    ///     Custom error mapping for Error.Custom() status codes.
    /// </summary>
    internal readonly record struct CustomEntry(
        string Factory,
        string TypeFqn,
        bool HasBody);
}