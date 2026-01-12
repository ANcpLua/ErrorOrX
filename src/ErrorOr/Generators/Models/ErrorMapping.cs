namespace ErrorOr.Generators;

/// <summary>
///     Single source of truth for ErrorType → HTTP mapping.
///     All error-to-status logic MUST derive from this class.
///     Consolidates: error type names, status code titles, and HTTP mappings.
/// </summary>
internal static class ErrorMapping
{
    #region ErrorTypeNames - Canonical error type names matching ErrorOr.ErrorType enum

    /// <summary>
    ///     Canonical error type names matching ErrorOr.ErrorType enum members.
    ///     Single source of truth for the generator - validated at test time against runtime enum.
    ///     Using string constants instead of a mirrored enum eliminates duplication while
    ///     maintaining compile-time safety via constant references.
    /// </summary>
    public const string Failure = nameof(Failure);
    public const string Unexpected = nameof(Unexpected);
    public const string Validation = nameof(Validation);
    public const string Conflict = nameof(Conflict);
    public const string NotFound = nameof(NotFound);
    public const string Unauthorized = nameof(Unauthorized);
    public const string Forbidden = nameof(Forbidden);

    /// <summary>
    ///     All known error type names in deterministic order for reproducible code generation.
    /// </summary>
    public static readonly IReadOnlyList<string> AllErrorTypes =
    [
        Failure,
        Unexpected,
        Validation,
        Conflict,
        NotFound,
        Unauthorized,
        Forbidden
    ];

    /// <summary>
    ///     Fast lookup set for O(1) membership testing.
    /// </summary>
    private static readonly HashSet<string> ErrorTypeSet = new(AllErrorTypes, StringComparer.Ordinal);

    /// <summary>
    ///     Returns true if the name is a known ErrorType member.
    /// </summary>
    public static bool IsKnownErrorType(string name) => ErrorTypeSet.Contains(name);

    #endregion

    #region StatusCodeTitles - RFC 9110 compliant HTTP status code titles

    /// <summary>
    ///     RFC 9110 compliant HTTP status code titles.
    /// </summary>
    private static readonly Dictionary<int, string> StatusTitles = new()
    {
        // 4xx Client Errors
        [400] = "Bad Request",
        [401] = "Unauthorized",
        [402] = "Payment Required",
        [403] = "Forbidden",
        [404] = "Not Found",
        [405] = "Method Not Allowed",
        [406] = "Not Acceptable",
        [407] = "Proxy Authentication Required",
        [408] = "Request Timeout",
        [409] = "Conflict",
        [410] = "Gone",
        [411] = "Length Required",
        [412] = "Precondition Failed",
        [413] = "Content Too Large",
        [414] = "URI Too Long",
        [415] = "Unsupported Media Type",
        [416] = "Range Not Satisfiable",
        [417] = "Expectation Failed",
        [418] = "I'm a teapot",
        [421] = "Misdirected Request",
        [422] = "Unprocessable Content",
        [423] = "Locked",
        [424] = "Failed Dependency",
        [425] = "Too Early",
        [426] = "Upgrade Required",
        [428] = "Precondition Required",
        [429] = "Too Many Requests",
        [431] = "Request Header Fields Too Large",
        [451] = "Unavailable For Legal Reasons",
        // 5xx Server Errors
        [500] = "Internal Server Error",
        [501] = "Not Implemented",
        [502] = "Bad Gateway",
        [503] = "Service Unavailable",
        [504] = "Gateway Timeout",
        [505] = "HTTP Version Not Supported",
        [506] = "Variant Also Negotiates",
        [507] = "Insufficient Storage",
        [508] = "Loop Detected",
        [510] = "Not Extended",
        [511] = "Network Authentication Required"
    };

    /// <summary>
    ///     Gets the RFC 9110 compliant title for a given HTTP status code.
    /// </summary>
    public static string GetStatusTitle(int statusCode) =>
        StatusTitles.TryGetValue(statusCode, out var title) ? title : "Error";

    #endregion

    #region ErrorType → HTTP Mappings

    /// <summary>
    ///     All ErrorType name → Entry mappings. This is the CANONICAL source.
    /// </summary>
    private static readonly Dictionary<string, Entry> Mappings = new(StringComparer.Ordinal)
    {
        // 4xx Client Errors
        [Validation] = new Entry(
            WellKnownTypes.Fqn.HttpResults.ValidationProblem,
            $"{WellKnownTypes.Fqn.TypedResults.ValidationProblem}(errors)",
            400, false),
        [Unauthorized] = new Entry(
            WellKnownTypes.Fqn.HttpResults.UnauthorizedHttpResult,
            $"{WellKnownTypes.Fqn.TypedResults.Unauthorized}()",
            401, false),
        [Forbidden] = new Entry(
            WellKnownTypes.Fqn.HttpResults.ForbidHttpResult,
            $"{WellKnownTypes.Fqn.TypedResults.Forbid}()",
            403, false),
        [NotFound] = new Entry(
            $"{WellKnownTypes.Fqn.HttpResults.NotFound}<{WellKnownTypes.Fqn.ProblemDetails}>",
            $"{WellKnownTypes.Fqn.TypedResults.NotFound}(problem)",
            404, true),
        [Conflict] = new Entry(
            $"{WellKnownTypes.Fqn.HttpResults.Conflict}<{WellKnownTypes.Fqn.ProblemDetails}>",
            $"{WellKnownTypes.Fqn.TypedResults.Conflict}(problem)",
            409, true),
        // 5xx Server Errors (RFC 9110 §15.6.1)
        [Failure] = new Entry(
            $"{WellKnownTypes.Fqn.HttpResults.InternalServerError}<{WellKnownTypes.Fqn.ProblemDetails}>",
            $"{WellKnownTypes.Fqn.TypedResults.InternalServerError}(problem)",
            500, true),
        [Unexpected] = new Entry(
            $"{WellKnownTypes.Fqn.HttpResults.InternalServerError}<{WellKnownTypes.Fqn.ProblemDetails}>",
            $"{WellKnownTypes.Fqn.TypedResults.InternalServerError}(problem)",
            500, true)
    };

    /// <summary>
    ///     Default entry for unknown ErrorTypes (500 Internal Server Error).
    /// </summary>
    private static readonly Entry DefaultEntry = new(
        $"{WellKnownTypes.Fqn.HttpResults.InternalServerError}<{WellKnownTypes.Fqn.ProblemDetails}>",
        $"{WellKnownTypes.Fqn.TypedResults.InternalServerError}(problem)",
        500,
        true);

    /// <summary>
    ///     Gets all defined ErrorType names in canonical order for code generation.
    /// </summary>
    public static IEnumerable<string> AllErrorTypeNames => Mappings.Keys;

    /// <summary>
    ///     Gets the mapping entry for an ErrorType name.
    /// </summary>
    public static Entry Get(string errorTypeName)
    {
        return Mappings.TryGetValue(errorTypeName, out var entry) ? entry : DefaultEntry;
    }

    /// <summary>
    ///     Gets the HTTP status code for an ErrorType name.
    /// </summary>
    public static int GetStatusCode(string errorTypeName)
    {
        return Get(errorTypeName).StatusCode;
    }

    /// <summary>
    ///     Gets the TypedResults factory call for an ErrorType name.
    /// </summary>
    public static string GetFactory(string errorTypeName)
    {
        return Get(errorTypeName).Factory;
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
    ///     Canonical mapping of HTTP status codes to TypedResults factories.
    ///     Used by both GetCustom() and GenerateStatusToFactorySwitch().
    /// </summary>
    private static readonly Dictionary<int, CustomEntry> StatusToFactory = new()
    {
        [400] = new CustomEntry(
            $"{WellKnownTypes.Fqn.TypedResults.BadRequest}(problem)",
            $"{WellKnownTypes.Fqn.HttpResults.BadRequest}<{WellKnownTypes.Fqn.ProblemDetails}>",
            true),
        [401] = new CustomEntry(
            $"{WellKnownTypes.Fqn.TypedResults.Unauthorized}()",
            WellKnownTypes.Fqn.HttpResults.UnauthorizedHttpResult,
            false),
        [403] = new CustomEntry(
            $"{WellKnownTypes.Fqn.TypedResults.Forbid}()",
            WellKnownTypes.Fqn.HttpResults.ForbidHttpResult,
            false),
        [404] = new CustomEntry(
            $"{WellKnownTypes.Fqn.TypedResults.NotFound}(problem)",
            $"{WellKnownTypes.Fqn.HttpResults.NotFound}<{WellKnownTypes.Fqn.ProblemDetails}>",
            true),
        [409] = new CustomEntry(
            $"{WellKnownTypes.Fqn.TypedResults.Conflict}(problem)",
            $"{WellKnownTypes.Fqn.HttpResults.Conflict}<{WellKnownTypes.Fqn.ProblemDetails}>",
            true),
        [422] = new CustomEntry(
            $"{WellKnownTypes.Fqn.TypedResults.UnprocessableEntity}(problem)",
            $"{WellKnownTypes.Fqn.HttpResults.UnprocessableEntity}<{WellKnownTypes.Fqn.ProblemDetails}>",
            true),
        [500] = new CustomEntry(
            $"{WellKnownTypes.Fqn.TypedResults.InternalServerError}(problem)",
            $"{WellKnownTypes.Fqn.HttpResults.InternalServerError}<{WellKnownTypes.Fqn.ProblemDetails}>",
            true)
    };

    /// <summary>
    ///     Default factory for unknown status codes using Problem().
    /// </summary>
    private static CustomEntry GetProblemFactory(int statusCode)
    {
        return new CustomEntry(
            $"{WellKnownTypes.Fqn.TypedResults.Problem}(detail: first.Description, statusCode: {statusCode}, title: \"{GetStatusTitle(statusCode)}\")",
            WellKnownTypes.Fqn.HttpResults.ProblemHttpResult,
            true);
    }

    /// <summary>
    ///     Generates switch cases for status code → factory mapping.
    ///     For use in emitted support methods.
    /// </summary>
    public static IEnumerable<string> GenerateStatusToFactoryCases()
    {
        foreach (var kvp in StatusToFactory.OrderBy(static x => x.Key))
            if (kvp.Key != 400) // Skip 400 - handled by validation
                yield return $"{kvp.Key} => {kvp.Value.Factory}";
    }

    /// <summary>
    ///     Gets the default Problem() factory expression for unknown status codes.
    /// </summary>
    public static string GetDefaultProblemFactory()
    {
        return $"{WellKnownTypes.Fqn.TypedResults.Problem}(detail: first.Description, statusCode: problem.Status ?? 500, title: first.Code, type: problem.Type)";
    }

    /// <summary>
    ///     Maps a custom error numeric type to the appropriate TypedResults factory.
    /// </summary>
    public static CustomEntry GetCustom(int numericType)
    {
        if (StatusToFactory.TryGetValue(numericType, out var entry))
            return entry;

        // Fallback to Problem() for all other valid HTTP status codes
        return numericType is >= 400 and < 600
            ? GetProblemFactory(numericType)
            : StatusToFactory[500]; // Invalid status code → default to 500
    }

    #endregion

    #region Data Structures

    /// <summary>
    ///     Entry representing an ErrorType mapping.
    /// </summary>
    internal readonly record struct Entry(
        string TypeFqn,
        string Factory,
        int StatusCode,
        bool NeedsProblem);

    /// <summary>
    ///     Custom error mapping for Error.Custom() status codes.
    /// </summary>
    internal readonly record struct CustomEntry(
        string Factory,
        string TypeFqn,
        bool HasBody);

    #endregion
}
