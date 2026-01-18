using Microsoft.CodeAnalysis;

namespace ErrorOr.Analyzers;

/// <summary>
///     Diagnostic descriptors for ErrorOr.Endpoints analyzer.
///     Provides real-time IDE feedback for ErrorOr.Endpoints endpoints.
/// </summary>
/// <remarks>
///     Note: These descriptors are intentionally duplicated in ErrorOr.Endpoints/Generators/Diagnostics.cs
///     because the analyzer and generator are loaded in different contexts (IDE vs build) and must be
///     self-contained. Keep both files in sync when modifying diagnostic IDs or messages.
/// </remarks>
public static class Descriptors
{
    private const string Category = "ErrorOr.Endpoints";


    // ═══════════════════════════════════════════════════════════════════════════
    // Errors - Must fix, won't compile or will fail at runtime
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    ///     Handler method must return ErrorOr&lt;T&gt;, Task&lt;ErrorOr&lt;T&gt;&gt;, or ValueTask&lt;ErrorOr&lt;T&gt;&gt;.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidReturnType = new(
        "EOE001",
        "Invalid return type",
        "Method '{0}' must return ErrorOr<T>, Task<ErrorOr<T>>, or ValueTask<ErrorOr<T>>",
        Category,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     Handler methods must be static for source generation.
    /// </summary>
    public static readonly DiagnosticDescriptor NonStaticHandler = new(
        "EOE002",
        "Handler must be static",
        "Method '{0}' must be static. Instance methods cannot be used with ErrorOr.Endpoints source generation.",
        Category,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     Route template has a parameter that no method parameter captures.
    /// </summary>
    public static readonly DiagnosticDescriptor RouteParameterNotBound = new(
        "EOE003",
        "Route parameter not bound",
        "Route '{0}' has parameter '{{{1}}}' but no method parameter captures it",
        Category,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     Same route + HTTP method registered by multiple handlers.
    ///     (Generator-only: requires cross-file analysis)
    /// </summary>
    public static readonly DiagnosticDescriptor DuplicateRoute = new(
        "EOE004",
        "Duplicate route",
        "Route '{0} {1}' is already registered by '{2}.{3}'",
        Category,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     Route pattern syntax is invalid.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidRoutePattern = new(
        "EOE005",
        "Invalid route pattern",
        "Route pattern '{0}' is invalid: {1}",
        Category,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     Endpoint has multiple body sources (FromBody, FromForm, Stream, PipeReader).
    ///     Only one is allowed per endpoint.
    /// </summary>
    public static readonly DiagnosticDescriptor MultipleBodySources = new(
        "EOE006",
        "Multiple body sources",
        "Endpoint '{0}' has multiple body sources. Use only one of: [FromBody], [FromForm], Stream, or PipeReader.",
        Category,
        DiagnosticSeverity.Error,
        true);

    // ═══════════════════════════════════════════════════════════════════════════
    // Warnings - Should review, potential issues
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    ///     Type used in endpoint not registered in JsonSerializerContext for AOT.
    ///     (Generator-only: requires cross-file analysis)
    /// </summary>
    public static readonly DiagnosticDescriptor TypeNotInJsonContext = new(
        "EOE007",
        "Type not AOT-serializable",
        "Type '{0}' used by '{1}' is not in any [JsonSerializable] context. Add it for NativeAOT support.",
        Category,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     GET, HEAD, DELETE, OPTIONS should not have request bodies per HTTP semantics.
    /// </summary>
    public static readonly DiagnosticDescriptor BodyOnReadOnlyMethod = new(
        "EOE009",
        "Body on read-only HTTP method",
        "Endpoint '{0}' uses {1} with a request body. Consider using POST/PUT/PATCH instead.",
        Category,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     [AcceptedResponse] on GET/DELETE is semantically unusual.
    ///     202 Accepted is for async operations that will be processed later.
    /// </summary>
    public static readonly DiagnosticDescriptor AcceptedOnReadOnlyMethod = new(
        "EOE010",
        "[AcceptedResponse] on read-only method",
        "Endpoint '{0}' uses [AcceptedResponse] with {1}. 202 Accepted is typically for async POST/PUT operations.",
        Category,
        DiagnosticSeverity.Warning,
        true);

    // ═══════════════════════════════════════════════════════════════════════════
    // Parameter Binding Diagnostics (EOE011-EOE019)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    ///     [FromRoute] parameter type is not a supported primitive and has no TryParse.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidFromRouteType = new(
        "EOE011",
        "Invalid [FromRoute] type",
        "Parameter '{0}' with [FromRoute] must be a primitive type or implement TryParse. Type '{1}' is not supported.",
        Category,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     [FromQuery] parameter type is not a supported primitive or collection of primitives.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidFromQueryType = new(
        "EOE012",
        "Invalid [FromQuery] type",
        "Parameter '{0}' with [FromQuery] must be a primitive or collection of primitives. Type '{1}' is not supported.",
        Category,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     [AsParameters] used on non-class/struct type.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidAsParametersType = new(
        "EOE013",
        "Invalid [AsParameters] type",
        "Parameter '{0}' with [AsParameters] must be a class or struct type, not '{1}'",
        Category,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     [AsParameters] type has no accessible constructor.
    /// </summary>
    public static readonly DiagnosticDescriptor AsParametersNoConstructor = new(
        "EOE014",
        "[AsParameters] type has no constructor",
        "Type '{0}' used with [AsParameters] must have an accessible constructor",
        Category,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     [FromHeader] with non-string type requires TryParse.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidFromHeaderType = new(
        "EOE016",
        "Invalid [FromHeader] type",
        "Parameter '{0}' with [FromHeader] must be string, a primitive with TryParse, or a collection thereof. Type '{1}' is not supported.",
        Category,
        DiagnosticSeverity.Error,
        true);

    // ═══════════════════════════════════════════════════════════════════════════
    // Route Constraint Diagnostics (EOE020-EOE029)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    ///     Route constraint type does not match parameter type.
    ///     For example, {id:int} requires int parameter, not string.
    /// </summary>
    public static readonly DiagnosticDescriptor RouteConstraintTypeMismatch = new(
        "EOE023",
        "Route constraint type mismatch",
        "Route parameter '{{{0}:{1}}}' expects {2}, but method parameter '{3}' is {4}",
        Category,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     Complex type parameter on GET/DELETE requires explicit binding attribute.
    ///     GET/DELETE cannot have implicit body binding, so the user must specify
    ///     [FromQuery], [AsParameters], or [FromServices].
    /// </summary>
    public static readonly DiagnosticDescriptor AmbiguousParameterBinding = new(
        "EOE025",
        "Ambiguous parameter binding",
        "Parameter '{0}' of type '{1}' on {2} endpoint requires explicit binding attribute. " +
        "Use [FromQuery] for query string, [AsParameters] for expanded binding, " +
        "or [FromServices] for DI injection.",
        Category,
        DiagnosticSeverity.Error,
        true);

    // ═══════════════════════════════════════════════════════════════════════════
    // OpenAPI / Documentation Diagnostics (EOE030-EOE039)
    // Generator-only: require cross-file or complex analysis
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    ///     Endpoint has too many result types for Results&lt;...&gt; union.
    ///     (Generator-only: requires computing all possible outcomes)
    /// </summary>
    public static readonly DiagnosticDescriptor TooManyResultTypes = new(
        "EOE030",
        "Too many result types",
        "Endpoint '{0}' has {1} possible response types, exceeding Results<...> max arity of {2}. OpenAPI documentation may be incomplete.",
        Category,
        DiagnosticSeverity.Info,
        true);

    /// <summary>
    ///     Endpoint calls interface/abstract method returning ErrorOr without error documentation.
    ///     This is an ERROR because OpenAPI would lie about possible responses.
    ///     (Generator-only: requires call graph analysis)
    /// </summary>
    public static readonly DiagnosticDescriptor UndocumentedInterfaceCall = new(
        "EOE033",
        "Undocumented interface call",
        "Endpoint '{0}' calls '{1}' which returns ErrorOr<T> but has no error documentation. " +
        "Add [ProducesError(...)] to the endpoint or [ReturnsError(...)] to the interface method. " +
        "OpenAPI cannot infer errors through interfaces.",
        Category,
        DiagnosticSeverity.Error, // ERROR, not Warning - keine Lüge!
        true);

    /// <summary>
    ///     Error factory method is not a known ErrorType.
    /// </summary>
    public static readonly DiagnosticDescriptor UnknownErrorFactory = new(
        "EOE032",
        "Unknown error factory",
        "Error.Or factory method '{0}' is not a known ErrorType. Supported types: Failure, Unexpected, Validation, Conflict, NotFound, Unauthorized, Forbidden.",
        Category,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     User's JsonSerializerContext is missing CamelCase property naming policy.
    ///     Web APIs typically use camelCase for JSON properties.
    /// </summary>
    public static readonly DiagnosticDescriptor MissingCamelCasePolicy = new(
        "EOE040",
        "Missing CamelCase policy",
        "JsonSerializerContext '{0}' should use PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase for web API compatibility. " +
        "Add [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)] to the class.",
        Category,
        DiagnosticSeverity.Warning,
        true);


    // ═══════════════════════════════════════════════════════════════════════════
    // AOT Safety Diagnostics (AOT001-AOT009)
    // ═══════════════════════════════════════════════════════════════════════════

    private const string AotSafetyCategory = "ErrorOr.AotSafety";

    /// <summary>
    ///     Activator.CreateInstance is not AOT-compatible.
    ///     Use factory methods or explicit construction instead.
    /// </summary>
    public static readonly DiagnosticDescriptor ActivatorCreateInstance = new(
        "AOT001",
        "Activator.CreateInstance is not AOT-safe",
        "Activator.CreateInstance<{0}>() uses reflection and is not compatible with NativeAOT. Use explicit construction or factory methods.",
        AotSafetyCategory,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     Type.GetType(string) is not AOT-compatible.
    ///     Types may be trimmed and unavailable at runtime.
    /// </summary>
    public static readonly DiagnosticDescriptor TypeGetType = new(
        "AOT002",
        "Type.GetType is not AOT-safe",
        "Type.GetType(\"{0}\") uses runtime type lookup and is not compatible with NativeAOT. Types may be trimmed.",
        AotSafetyCategory,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     Reflection over type members is not AOT-compatible.
    ///     Members may be trimmed and unavailable at runtime.
    /// </summary>
    public static readonly DiagnosticDescriptor ReflectionOverMembers = new(
        "AOT003",
        "Reflection over members is not AOT-safe",
        "typeof({0}).{1}() uses reflection and is not compatible with NativeAOT. Members may be trimmed. Consider source generators.",
        AotSafetyCategory,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     Expression.Compile() generates code at runtime.
    ///     This is not supported in NativeAOT.
    /// </summary>
    public static readonly DiagnosticDescriptor ExpressionCompile = new(
        "AOT004",
        "Expression.Compile is not AOT-safe",
        "Expression.Compile() generates code at runtime and is not compatible with NativeAOT. Use compiled delegates or source generators.",
        AotSafetyCategory,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     The 'dynamic' keyword uses runtime binding.
    ///     This is not supported in NativeAOT.
    /// </summary>
    public static readonly DiagnosticDescriptor DynamicKeyword = new(
        "AOT005",
        "'dynamic' is not AOT-safe",
        "The 'dynamic' keyword uses runtime binding and is not compatible with NativeAOT. Use strongly-typed code instead.",
        AotSafetyCategory,
        DiagnosticSeverity.Warning,
        true);
}
