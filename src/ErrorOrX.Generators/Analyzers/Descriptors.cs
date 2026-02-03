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

    // Help link URLs for AOT diagnostics
    private const string TrimWarningsUrl = "https://learn.microsoft.com/dotnet/core/deploying/trimming/fixing-warnings";
    private const string IntrinsicApisUrl = "https://learn.microsoft.com/dotnet/core/deploying/trimming/trimming-intrinsic";

    // ============================================================================
    // EOE001-007: Core validation
    // ============================================================================

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

    /// <summary>
    ///     Type used in endpoint not registered in JsonSerializerContext for AOT.
    ///     This is an ERROR because AOT compilation will fail at runtime with cryptic errors.
    ///     (Generator-only: requires cross-file analysis)
    /// </summary>
    public static readonly DiagnosticDescriptor TypeNotInJsonContext = new(
        "EOE007",
        "Type not AOT-serializable",
        "Type '{0}' used by '{1}' is not in any [JsonSerializable] context. Add [JsonSerializable(typeof({0}))] to your JsonSerializerContext.",
        Category,
        DiagnosticSeverity.Error,
        true,
        helpLinkUri: "https://learn.microsoft.com/aspnet/core/fundamentals/aot/request-delegate-generator/rdg");

    // ============================================================================
    // EOE008-014: Parameter binding validation
    // ============================================================================

    /// <summary>
    ///     GET, HEAD, DELETE, OPTIONS should not have request bodies per HTTP semantics.
    /// </summary>
    public static readonly DiagnosticDescriptor BodyOnReadOnlyMethod = new(
        "EOE008",
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
        "EOE009",
        "[AcceptedResponse] on read-only method",
        "Endpoint '{0}' uses [AcceptedResponse] with {1}. 202 Accepted is typically for async POST/PUT operations.",
        Category,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     [FromRoute] parameter type is not a supported primitive and has no TryParse.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidFromRouteType = new(
        "EOE010",
        "Invalid [FromRoute] type",
        "Parameter '{0}' with [FromRoute] must be a primitive type or implement TryParse. Type '{1}' is not supported.",
        Category,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     [FromQuery] parameter type is not a supported primitive or collection of primitives.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidFromQueryType = new(
        "EOE011",
        "Invalid [FromQuery] type",
        "Parameter '{0}' with [FromQuery] must be a primitive or collection of primitives. Type '{1}' is not supported.",
        Category,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     [AsParameters] used on non-class/struct type.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidAsParametersType = new(
        "EOE012",
        "Invalid [AsParameters] type",
        "Parameter '{0}' with [AsParameters] must be a class or struct type, not '{1}'",
        Category,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     [AsParameters] type has no accessible constructor.
    /// </summary>
    public static readonly DiagnosticDescriptor AsParametersNoConstructor = new(
        "EOE013",
        "[AsParameters] type has no constructor",
        "Type '{0}' used with [AsParameters] must have an accessible constructor",
        Category,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     [FromHeader] with non-string type requires TryParse.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidFromHeaderType = new(
        "EOE014",
        "Invalid [FromHeader] type",
        "Parameter '{0}' with [FromHeader] must be string, a primitive with TryParse, or a collection thereof. Type '{1}' is not supported.",
        Category,
        DiagnosticSeverity.Error,
        true);

    // ============================================================================
    // EOE015-019: Return type and parameter type validation
    // ============================================================================

    /// <summary>
    ///     Anonymous types cannot be used as ErrorOr value types.
    ///     They have no stable identity for JSON serialization.
    /// </summary>
    public static readonly DiagnosticDescriptor AnonymousReturnTypeNotSupported = new(
        "EOE015",
        "Anonymous return type not supported",
        "Method '{0}' returns ErrorOr with anonymous type. Anonymous types cannot be serialized. Use a named type instead.",
        Category,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     [AsParameters] types cannot contain nested [AsParameters] properties.
    ///     Recursive parameter expansion is not supported.
    /// </summary>
    public static readonly DiagnosticDescriptor NestedAsParametersNotSupported = new(
        "EOE016",
        "Nested [AsParameters] not supported",
        "Type '{0}' used with [AsParameters] has property '{1}' also marked [AsParameters]. Nested parameter expansion is not supported.",
        Category,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     [AsParameters] cannot be applied to nullable types.
    ///     Parameter expansion requires a concrete instance.
    /// </summary>
    public static readonly DiagnosticDescriptor NullableAsParametersNotSupported = new(
        "EOE017",
        "Nullable [AsParameters] not supported",
        "Parameter '{0}' with [AsParameters] cannot be nullable. Remove the '?' or use a non-nullable type.",
        Category,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     Private or protected types cannot be used in endpoint signatures.
    ///     Generated code cannot access them.
    /// </summary>
    public static readonly DiagnosticDescriptor InaccessibleTypeNotSupported = new(
        "EOE018",
        "Inaccessible type in endpoint",
        "Type '{0}' used by endpoint '{1}' is {2} and cannot be accessed by generated code. Make it internal or public.",
        Category,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     Type parameters (open generics) cannot be used in endpoint return types.
    ///     The generator cannot emit code for unbound generic types.
    /// </summary>
    public static readonly DiagnosticDescriptor TypeParameterNotSupported = new(
        "EOE019",
        "Type parameter not supported",
        "Method '{0}' uses type parameter '{1}' in return type. Generic type parameters cannot be used with ErrorOr endpoints.",
        Category,
        DiagnosticSeverity.Error,
        true);

    // ============================================================================
    // EOE020-021: Route constraint validation
    // ============================================================================

    /// <summary>
    ///     Route constraint type does not match parameter type.
    ///     For example, {id:int} requires int parameter, not string.
    /// </summary>
    public static readonly DiagnosticDescriptor RouteConstraintTypeMismatch = new(
        "EOE020",
        "Route constraint type mismatch",
        "Route parameter '{{{0}:{1}}}' expects {2}, but method parameter '{3}' is {4}",
        Category,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     Complex type parameter on bodyless/custom method requires explicit binding attribute.
    ///     Bodyless methods cannot have implicit body binding, so the user must specify
    ///     [AsParameters], [FromBody], or [FromServices].
    /// </summary>
    public static readonly DiagnosticDescriptor AmbiguousParameterBinding = new(
        "EOE021",
        "Ambiguous parameter binding",
        "Parameter '{0}' of type '{1}' on {2} endpoint requires explicit binding attribute. " +
        "Use [AsParameters] for query/route expansion, [FromBody] to force body binding, " +
        "or [FromServices] for DI injection.",
        Category,
        DiagnosticSeverity.Error,
        true);

    // ============================================================================
    // EOE022-024: Result types and error factories
    // ============================================================================

    /// <summary>
    ///     Endpoint has too many result types for Results&lt;...&gt; union.
    ///     (Generator-only: requires computing all possible outcomes)
    /// </summary>
    public static readonly DiagnosticDescriptor TooManyResultTypes = new(
        "EOE022",
        "Too many result types",
        "Endpoint '{0}' has {1} possible response types, exceeding Results<...> max arity of {2}. OpenAPI documentation may be incomplete.",
        Category,
        DiagnosticSeverity.Info,
        true);

    /// <summary>
    ///     Error factory method is not a known ErrorType.
    /// </summary>
    public static readonly DiagnosticDescriptor UnknownErrorFactory = new(
        "EOE023",
        "Unknown error factory",
        "Error.Or factory method '{0}' is not a known ErrorType. Supported types: Failure, Unexpected, Validation, Conflict, NotFound, Unauthorized, Forbidden.",
        Category,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     Endpoint calls interface/abstract method returning ErrorOr without error documentation.
    ///     This is an ERROR because OpenAPI would lie about possible responses.
    ///     (Generator-only: requires call graph analysis)
    /// </summary>
    public static readonly DiagnosticDescriptor UndocumentedInterfaceCall = new(
        "EOE024",
        "Undocumented interface call",
        "Endpoint '{0}' calls '{1}' which returns ErrorOr<T> but has no error documentation. " +
        "Add [ProducesError(...)] to the endpoint or [ReturnsError(...)] to the interface method. " +
        "OpenAPI cannot infer errors through interfaces.",
        Category,
        DiagnosticSeverity.Error,
        true);

    // ============================================================================
    // EOE025-026: JSON/AOT validation
    // ============================================================================

    /// <summary>
    ///     User's JsonSerializerContext is missing CamelCase property naming policy.
    ///     Web APIs typically use camelCase for JSON properties.
    /// </summary>
    public static readonly DiagnosticDescriptor MissingCamelCasePolicy = new(
        "EOE025",
        "Missing CamelCase policy",
        "JsonSerializerContext '{0}' should use PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase for web API compatibility. " +
        "Add [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)] to the class.",
        Category,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     No JsonSerializerContext found but endpoint uses request body.
    ///     Without a user-defined context, AOT serialization will fail.
    ///     This is an ERROR because the generated JsonContext cannot be used by System.Text.Json source generator.
    /// </summary>
    public static readonly DiagnosticDescriptor MissingJsonContextForBody = new(
        "EOE026",
        "Missing JsonSerializerContext for AOT",
        "Endpoint '{0}' uses '{1}' as request body but no JsonSerializerContext was found. Create one with [JsonSerializable(typeof({1}))].",
        Category,
        DiagnosticSeverity.Error,
        true,
        helpLinkUri: "https://learn.microsoft.com/aspnet/core/fundamentals/aot/request-delegate-generator/rdg");

    // ============================================================================
    // EOE027-031: API versioning
    // ============================================================================

    /// <summary>
    ///     Endpoint is version-neutral but has explicit version mappings.
    ///     [ApiVersionNeutral] and [MapToApiVersion] are mutually exclusive.
    /// </summary>
    public static readonly DiagnosticDescriptor VersionNeutralWithMappings = new(
        "EOE027",
        "Version-neutral with mappings",
        "Endpoint '{0}' is marked [ApiVersionNeutral] but also has [MapToApiVersion]. Remove one or the other.",
        Category,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     Endpoint has [MapToApiVersion] for a version not declared with [ApiVersion].
    ///     The mapped version should be one of the supported versions.
    /// </summary>
    public static readonly DiagnosticDescriptor MappedVersionNotDeclared = new(
        "EOE028",
        "Mapped version not declared",
        "Endpoint '{0}' maps to version '{1}' which is not declared in [ApiVersion]. Add [ApiVersion(\"{1}\")] to the class.",
        Category,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     Asp.Versioning.Http package is not referenced but [ApiVersion] attributes are used.
    ///     Install the package: dotnet add package Asp.Versioning.Http
    /// </summary>
    public static readonly DiagnosticDescriptor ApiVersioningPackageNotReferenced = new(
        "EOE029",
        "Asp.Versioning package not referenced",
        "Endpoint '{0}' uses API versioning but Asp.Versioning.Http package is not referenced",
        Category,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     Endpoint has no [ApiVersion] attribute but other endpoints in the project use versioning.
    ///     Consider adding version information or marking as [ApiVersionNeutral].
    /// </summary>
    public static readonly DiagnosticDescriptor EndpointMissingVersioning = new(
        "EOE030",
        "Endpoint missing versioning",
        "Endpoint '{0}' has no version information but other endpoints use API versioning. " +
        "Add [ApiVersion(\"X.Y\")] or [ApiVersionNeutral] to declare its version scope.",
        Category,
        DiagnosticSeverity.Info,
        true);

    /// <summary>
    ///     [ApiVersion] has invalid format. Use "major.minor" or just "major".
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidApiVersionFormat = new(
        "EOE031",
        "Invalid API version format",
        "[ApiVersion(\"{0}\")] has invalid format. Use \"major.minor\" (e.g., \"1.0\") or \"major\" (e.g., \"2\").",
        Category,
        DiagnosticSeverity.Error,
        true);

    // ============================================================================
    // EOE032-033: Route parameter and naming validation
    // ============================================================================

    /// <summary>
    ///     Multiple method parameters bind to the same route parameter name.
    ///     Only the first parameter will be used for route binding.
    /// </summary>
    public static readonly DiagnosticDescriptor DuplicateRouteParameterBinding = new(
        "EOE032",
        "Duplicate route parameter binding",
        "Multiple parameters bind to route parameter '{0}'. Only the first parameter ('{1}') will be bound; '{2}' will be ignored.",
        Category,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     Handler method name should follow PascalCase convention.
    ///     The first character should be uppercase, and no underscores should be used.
    /// </summary>
    public static readonly DiagnosticDescriptor MethodNameNotPascalCase = new(
        "EOE033",
        "Handler method name not PascalCase",
        "Method '{0}' should follow PascalCase naming convention. Consider renaming to '{1}'.",
        Category,
        DiagnosticSeverity.Warning,
        true);

    // ============================================================================
    // EOE034-038: AOT safety (formerly AOT001-AOT005)
    // ============================================================================

    /// <summary>
    ///     Activator.CreateInstance is not AOT-compatible.
    ///     Use factory methods, explicit construction, or annotate with DynamicallyAccessedMembers.
    /// </summary>
    public static readonly DiagnosticDescriptor ActivatorCreateInstance = new(
        "EOE034",
        "Activator.CreateInstance is not AOT-safe",
        "Activator.CreateInstance<{0}>() uses reflection and is not compatible with NativeAOT. " +
        "Use explicit construction, factory methods, or if the Type is a parameter, annotate it with " +
        "[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructors)].",
        Category,
        DiagnosticSeverity.Warning,
        true,
        helpLinkUri: TrimWarningsUrl);

    /// <summary>
    ///     Type.GetType(string) with dynamic or case-insensitive lookup is not AOT-compatible.
    ///     String literals are safe because the trimmer can analyze them at compile-time.
    ///     Dynamic lookups or case-insensitive searches prevent the trimmer from knowing which types to preserve.
    /// </summary>
    public static readonly DiagnosticDescriptor TypeGetType = new(
        "EOE035",
        "Type.GetType is not AOT-safe",
        "Type.GetType with {0} prevents the trimmer from statically analyzing which types to preserve. " +
        "Use typeof() for compile-time type references, or use a string literal without case-insensitive search. " +
        "For unavoidable dynamic patterns, annotate with [RequiresUnreferencedCode].",
        Category,
        DiagnosticSeverity.Warning,
        true,
        helpLinkUri: IntrinsicApisUrl);

    /// <summary>
    ///     Reflection over type members is not AOT-compatible.
    ///     Members may be trimmed and unavailable at runtime.
    ///     Annotate the type parameter with [DynamicallyAccessedMembers] using the appropriate members flag.
    /// </summary>
    public static readonly DiagnosticDescriptor ReflectionOverMembers = new(
        "EOE036",
        "Reflection over members is not AOT-safe",
        "typeof({0}).{1}() uses reflection and is not compatible with NativeAOT. " +
        "Annotate the type parameter with [DynamicallyAccessedMembers({2})] or use source generators.",
        Category,
        DiagnosticSeverity.Warning,
        true,
        helpLinkUri: TrimWarningsUrl);

    /// <summary>
    ///     Expression.Compile() generates code at runtime.
    ///     This is not supported in NativeAOT.
    /// </summary>
    public static readonly DiagnosticDescriptor ExpressionCompile = new(
        "EOE037",
        "Expression.Compile is not AOT-safe",
        "Expression.Compile() generates code at runtime and is not compatible with NativeAOT. Use compiled delegates or source generators.",
        Category,
        DiagnosticSeverity.Warning,
        true,
        helpLinkUri: TrimWarningsUrl);

    /// <summary>
    ///     The 'dynamic' keyword uses runtime binding.
    ///     This is not supported in NativeAOT.
    /// </summary>
    public static readonly DiagnosticDescriptor DynamicKeyword = new(
        "EOE038",
        "'dynamic' is not AOT-safe",
        "The 'dynamic' keyword uses runtime binding and is not compatible with NativeAOT. Use strongly-typed code instead.",
        Category,
        DiagnosticSeverity.Warning,
        true,
        helpLinkUri: TrimWarningsUrl);

    /// <summary>
    ///     Parameter has validation attributes from System.ComponentModel.DataAnnotations.
    ///     Validator.TryValidateObject uses reflection internally.
    /// </summary>
    public static readonly DiagnosticDescriptor ValidationUsesReflection = new(
        "EOE039",
        "DataAnnotations validation uses reflection",
        "Parameter '{0}' in endpoint '{1}' has validation attributes. " +
        "Validator.TryValidateObject uses reflection and may cause trim warnings. " +
        "Consider FluentValidation with source generators or manual validation.",
        Category,
        DiagnosticSeverity.Info,
        true,
        helpLinkUri: TrimWarningsUrl);

    // ============================================================================
    // EOE040-041: JSON context completeness
    // ============================================================================

    /// <summary>
    ///     JsonSerializerContext is missing CamelCase property naming policy.
    ///     Web APIs typically use camelCase for JSON properties.
    /// </summary>
    /// <remarks>
    ///     This is an alias for EOE025 (MissingCamelCasePolicy) with different ID
    ///     for clarity in documentation. EOE025 remains the primary diagnostic.
    /// </remarks>
    public static readonly DiagnosticDescriptor JsonContextMissingCamelCase = new(
        "EOE040",
        "JsonSerializerContext missing CamelCase",
        "JsonSerializerContext '{0}' should use PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase for ASP.NET Core compatibility",
        Category,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     JsonSerializerContext is missing ProblemDetails and HttpValidationProblemDetails.
    ///     These types are required for error responses in ASP.NET Core.
    /// </summary>
    public static readonly DiagnosticDescriptor JsonContextMissingProblemDetails = new(
        "EOE041",
        "JsonSerializerContext missing error types",
        "JsonSerializerContext '{0}' should include [JsonSerializable(typeof(ProblemDetails))] and " +
        "[JsonSerializable(typeof(HttpValidationProblemDetails))] for error responses",
        Category,
        DiagnosticSeverity.Warning,
        true);
}
