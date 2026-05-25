using Microsoft.CodeAnalysis;

namespace ErrorOr.Analyzers;

public static partial class Descriptors
{
    /// <summary>EOE007 — type used by an endpoint is missing from every [JsonSerializable] context; AOT will fail at runtime.</summary>
    public static readonly DiagnosticDescriptor TypeNotInJsonContext = new(
        "EOE007",
        "Type not AOT-serializable",
        "Type '{0}' used by '{1}' is not in any [JsonSerializable] context. Add [JsonSerializable(typeof({0}))] to your JsonSerializerContext.",
        Category,
        DiagnosticSeverity.Error,
        true,
        helpLinkUri: AotRdgUrl);

    /// <summary>EOE025 — user's JsonSerializerContext is missing CamelCase property naming policy.</summary>
    public static readonly DiagnosticDescriptor MissingCamelCasePolicy = new(
        "EOE025",
        "Missing CamelCase policy",
        "JsonSerializerContext '{0}' should use PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase for web API compatibility. " +
        "Add [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)] to the class.",
        Category,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>EOE026 — no JsonSerializerContext found but endpoint uses a request body; AOT will fail without one.</summary>
    public static readonly DiagnosticDescriptor MissingJsonContextForBody = new(
        "EOE026",
        "Missing JsonSerializerContext for AOT",
        "Endpoint '{0}' uses '{1}' as request body but no JsonSerializerContext was found. Create one with [JsonSerializable(typeof({1}))].",
        Category,
        DiagnosticSeverity.Error,
        true,
        helpLinkUri: AotRdgUrl);

    /// <summary>
    ///     EOE034 — endpoint parameter carries [Required]/[StringLength]/etc; Validator.TryValidateObject uses
    ///     reflection, surfaces trim warnings.
    /// </summary>
    public static readonly DiagnosticDescriptor ValidationUsesReflection = new(
        "EOE034",
        "DataAnnotations validation uses reflection",
        "Parameter '{0}' in endpoint '{1}' has validation attributes. " +
        "Validator.TryValidateObject uses reflection and may cause trim warnings. " +
        "Consider FluentValidation with source generators or manual validation.",
        Category,
        DiagnosticSeverity.Info,
        true,
        helpLinkUri: TrimWarningsUrl);

    /// <summary>EOE035 — alias for EOE025; reports the CamelCase policy gap on a specific user-defined JsonSerializerContext.</summary>
    public static readonly DiagnosticDescriptor JsonContextMissingCamelCase = new(
        "EOE035",
        "JsonSerializerContext missing CamelCase",
        "JsonSerializerContext '{0}' should use PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase for ASP.NET Core compatibility",
        Category,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     EOE036 — JsonSerializerContext is missing ProblemDetails and HttpValidationProblemDetails (needed for error
    ///     responses).
    /// </summary>
    public static readonly DiagnosticDescriptor JsonContextMissingProblemDetails = new(
        "EOE036",
        "JsonSerializerContext missing error types",
        "JsonSerializerContext '{0}' should include [JsonSerializable(typeof(ProblemDetails))] and " +
        "[JsonSerializable(typeof(HttpValidationProblemDetails))] for error responses",
        Category,
        DiagnosticSeverity.Warning,
        true);
}
