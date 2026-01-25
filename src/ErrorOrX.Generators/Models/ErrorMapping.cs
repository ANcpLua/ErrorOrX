namespace ErrorOr.Generators;

/// <summary>
///     Single source of truth for ErrorType → HTTP mapping.
///     All error-to-status logic MUST derive from this class.
///     Consolidates: error type names, status code titles, and HTTP mappings.
/// </summary>
internal static class ErrorMapping
{
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
    ///     All ErrorType name → Entry mappings. This is the CANONICAL source.
    /// </summary>
    private static readonly Dictionary<string, Entry> Mappings = new(StringComparer.Ordinal)
    {
        // 4xx Client Errors
        // Validation: errors parameter is Dictionary<string, string[]>?
        // Conversion from List<Error> to validation errors dict handled by ToProblem() emitted helper
        [Validation] = new Entry(
            WellKnownTypes.Fqn.HttpResults.ValidationProblem,
            $"{WellKnownTypes.Fqn.TypedResults.ValidationProblem}(errors)",
            400),
        [Unauthorized] = new Entry(
            WellKnownTypes.Fqn.HttpResults.UnauthorizedHttpResult,
            $"{WellKnownTypes.Fqn.TypedResults.Unauthorized}()",
            401),
        [Forbidden] = new Entry(
            WellKnownTypes.Fqn.HttpResults.ForbidHttpResult,
            $"{WellKnownTypes.Fqn.TypedResults.Forbid}()",
            403),
        [NotFound] = new Entry(
            $"{WellKnownTypes.Fqn.HttpResults.NotFound}<{WellKnownTypes.Fqn.ProblemDetails}>",
            $"{WellKnownTypes.Fqn.TypedResults.NotFound}(problem)",
            404),
        [Conflict] = new Entry(
            $"{WellKnownTypes.Fqn.HttpResults.Conflict}<{WellKnownTypes.Fqn.ProblemDetails}>",
            $"{WellKnownTypes.Fqn.TypedResults.Conflict}(problem)",
            409),
        // 5xx Server Errors (RFC 9110 §15.6.1)
        [Failure] = new Entry(
            $"{WellKnownTypes.Fqn.HttpResults.InternalServerError}<{WellKnownTypes.Fqn.ProblemDetails}>",
            $"{WellKnownTypes.Fqn.TypedResults.InternalServerError}(problem)",
            500),
        [Unexpected] = new Entry(
            $"{WellKnownTypes.Fqn.HttpResults.InternalServerError}<{WellKnownTypes.Fqn.ProblemDetails}>",
            $"{WellKnownTypes.Fqn.TypedResults.InternalServerError}(problem)",
            500)
    };

    /// <summary>
    ///     Default entry for unknown ErrorTypes (500 Internal Server Error).
    /// </summary>
    private static readonly Entry DefaultEntry = new(
        $"{WellKnownTypes.Fqn.HttpResults.InternalServerError}<{WellKnownTypes.Fqn.ProblemDetails}>",
        $"{WellKnownTypes.Fqn.TypedResults.InternalServerError}(problem)",
        500);

    /// <summary>
    ///     Canonical mapping of HTTP status codes to TypedResults factories.
    ///     Maps both core ErrorType status codes and custom error status codes (via Error.Custom()).
    ///     Includes codes not in the core ErrorType enum (e.g., 422 Unprocessable Entity) to support
    ///     additional HTTP semantics for specialized validation scenarios.
    ///     Used by GenerateStatusToFactoryCases().
    /// </summary>
    private static readonly Dictionary<int, CustomEntry> StatusToFactory = new()
    {
        [400] = new CustomEntry($"{WellKnownTypes.Fqn.TypedResults.BadRequest}(problem)"),
        [401] = new CustomEntry($"{WellKnownTypes.Fqn.TypedResults.Unauthorized}()"),
        [403] = new CustomEntry($"{WellKnownTypes.Fqn.TypedResults.Forbid}()"),
        [404] = new CustomEntry($"{WellKnownTypes.Fqn.TypedResults.NotFound}(problem)"),
        [409] = new CustomEntry($"{WellKnownTypes.Fqn.TypedResults.Conflict}(problem)"),
        [422] = new CustomEntry($"{WellKnownTypes.Fqn.TypedResults.UnprocessableEntity}(problem)"),
        [500] = new CustomEntry($"{WellKnownTypes.Fqn.TypedResults.InternalServerError}(problem)")
    };

    /// <summary>
    ///     Returns true if the name is a known ErrorType member.
    /// </summary>
    public static bool IsKnownErrorType(string name) => ErrorTypeSet.Contains(name);

    /// <summary>
    ///     Gets the mapping entry for an ErrorType name.
    /// </summary>
    public static Entry Get(string errorTypeName) => Mappings.TryGetValue(errorTypeName, out var entry) ? entry : DefaultEntry;

    /// <summary>
    ///     Gets the HTTP status code for an ErrorType name.
    /// </summary>
    public static int GetStatusCode(string errorTypeName) => Get(errorTypeName).StatusCode;

    /// <summary>
    ///     Gets the TypedResults factory call for an ErrorType name.
    /// </summary>
    public static string GetFactory(string errorTypeName) => Get(errorTypeName).Factory;

    /// <summary>
    ///     Generates the switch expression body for ErrorType → Status mapping.
    ///     Derives from the canonical mappings.
    /// </summary>
    public static string GenerateStatusSwitch(string errorTypeFqn, string variableName = "first")
    {
        var cases = Mappings
            .Select(kvp => $"{errorTypeFqn}.{kvp.Key} => {kvp.Value.StatusCode}");
        return string.Join(", ", cases) + $", _ => {variableName}.NumericType is >= 100 and <= 599 ? {variableName}.NumericType : 500";
    }

    /// <summary>
    ///     Generates switch cases for status code → factory mapping.
    ///     For use in emitted support methods.
    /// </summary>
    public static IEnumerable<string> GenerateStatusToFactoryCases()
    {
        foreach (var kvp in StatusToFactory.OrderBy(static x => x.Key))
            yield return $"{kvp.Key} => {kvp.Value.Factory}";
    }

    /// <summary>
    ///     Gets the default Problem() factory expression for unknown status codes.
    /// </summary>
    public static string GetDefaultProblemFactory() => $"{WellKnownTypes.Fqn.TypedResults.Problem}(detail: first.Description, statusCode: problem.Status ?? 500, title: first.Code, type: problem.Type)";

    /// <summary>
    ///     Entry representing an ErrorType mapping.
    /// </summary>
    internal readonly record struct Entry(
        string TypeFqn,
        string Factory,
        int StatusCode);

    /// <summary>
    ///     Custom error mapping for Error.Custom() status codes.
    /// </summary>
    internal readonly record struct CustomEntry(
        string Factory);
}
