using System.Collections.Generic;
using System.Linq;

namespace ErrorOr.Endpoints.Generators;

/// <summary>
///     Single source of truth for ErrorType → HTTP mapping.
///     All error-to-status logic MUST derive from this class.
/// </summary>
internal static class ErrorMapping
{
    private const string HttpResultsNs = "global::Microsoft.AspNetCore.Http.HttpResults";
    private const string ProblemDetailsType = "global::Microsoft.AspNetCore.Mvc.ProblemDetails";

    /// <summary>
    ///     Entry representing an ErrorType mapping.
    /// </summary>
    internal readonly record struct Entry(
        string TypeFqn,
        int StatusCode,
        bool NeedsProblem);

    /// <summary>
    ///     All ErrorType → Entry mappings. This is the CANONICAL source.
    /// </summary>
    private static readonly Dictionary<ErrorType, Entry> Mappings = new()
    {
        // 4xx Client Errors
        [ErrorType.Validation] = new($"{HttpResultsNs}.ValidationProblem", 400, false),
        [ErrorType.Unauthorized] = new($"{HttpResultsNs}.UnauthorizedHttpResult", 401, false),
        [ErrorType.Forbidden] = new($"{HttpResultsNs}.ForbidHttpResult", 403, false),
        [ErrorType.NotFound] = new($"{HttpResultsNs}.NotFound<{ProblemDetailsType}>", 404, true),
        [ErrorType.Conflict] = new($"{HttpResultsNs}.Conflict<{ProblemDetailsType}>", 409, true),
        // 5xx Server Errors (RFC 9110 §15.6.1)
        [ErrorType.Failure] = new($"{HttpResultsNs}.InternalServerError<{ProblemDetailsType}>", 500, true),
        [ErrorType.Unexpected] = new($"{HttpResultsNs}.InternalServerError<{ProblemDetailsType}>", 500, true)
    };

    /// <summary>
    ///     Default entry for unknown ErrorTypes (500 Internal Server Error).
    /// </summary>
    private static readonly Entry DefaultEntry = new(
        $"{HttpResultsNs}.InternalServerError<{ProblemDetailsType}>",
        500,
        true);

    /// <summary>
    ///     Gets the mapping entry for an ErrorType.
    /// </summary>
    public static Entry Get(ErrorType errorType) =>
        Mappings.TryGetValue(errorType, out var entry) ? entry : DefaultEntry;

    /// <summary>
    ///     Gets the HTTP status code for an ErrorType.
    /// </summary>
    public static int GetStatusCode(ErrorType errorType) =>
        Get(errorType).StatusCode;

    /// <summary>
    ///     Gets all defined ErrorTypes in canonical order for code generation.
    /// </summary>
    public static IEnumerable<ErrorType> AllErrorTypes => Mappings.Keys;

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
    ///     Custom error mapping for Error.Custom() status codes.
    /// </summary>
    internal readonly record struct CustomEntry(
        string Factory,
        string TypeFqn,
        bool HasBody);

    /// <summary>
    ///     Maps a custom error numeric type to the appropriate TypedResults factory.
    /// </summary>
    public static CustomEntry GetCustom(int numericType)
    {
        return numericType switch
        {
            400 => new("TypedResults.BadRequest(problem)", $"{HttpResultsNs}.BadRequest<{ProblemDetailsType}>", true),
            401 => new("TypedResults.Unauthorized()", $"{HttpResultsNs}.UnauthorizedHttpResult", false),
            403 => new("TypedResults.Forbid()", $"{HttpResultsNs}.ForbidHttpResult", false),
            404 => new("TypedResults.NotFound(problem)", $"{HttpResultsNs}.NotFound<{ProblemDetailsType}>", true),
            409 => new("TypedResults.Conflict(problem)", $"{HttpResultsNs}.Conflict<{ProblemDetailsType}>", true),
            422 => new("TypedResults.UnprocessableEntity(problem)",
                $"{HttpResultsNs}.UnprocessableEntity<{ProblemDetailsType}>", true),
            500 => new("TypedResults.InternalServerError(problem)",
                $"{HttpResultsNs}.InternalServerError<{ProblemDetailsType}>", true),
            // Fallback to Problem() for all other valid HTTP status codes
            >= 400 and < 600 => new(
                $"TypedResults.Problem(detail: first.Description, statusCode: {numericType}, title: \"{StatusCodeTitles.Get(numericType)}\")",
                $"{HttpResultsNs}.ProblemHttpResult", true),
            // Invalid status code → default to 500
            _ => new("TypedResults.InternalServerError(problem)",
                $"{HttpResultsNs}.InternalServerError<{ProblemDetailsType}>", true)
        };
    }
}
