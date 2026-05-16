using ANcpLua.Roslyn.Utilities;
using ANcpLua.Roslyn.Utilities.Models;
using ErrorOr.Analyzers;
using Microsoft.CodeAnalysis;

namespace ErrorOr.Generators;

/// <summary>
///     Partial class containing parameter binding logic.
///     Includes diagnostic wiring for invalid body, route, query, header, form, and AsParameters bindings.
/// </summary>
public sealed partial class ErrorOrEndpointGenerator
{
    internal static ParameterBindingResult BindParameters(
        IMethodSymbol method,
        ImmutableHashSet<string> routeParameters,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        ErrorOrContext context,
        HttpVerb httpVerb)
    {
        if (method.Parameters.Length is 0) return ParameterBindingResult.Empty;

        var metas = BuildParameterMetas(method.Parameters, context);

        // EOE006: Multiple body sources (FromBody, FromForm, Stream, PipeReader)
        var bodyCount = metas.Count(static m => m.HasFromBody);
        var formCount = metas.Count(static m =>
            m.HasFromForm || m.IsFormFile || m.IsFormFileCollection || m.IsFormCollection);
        var streamCount = metas.Count(static m => m.IsStream || m.IsPipeReader);

        // Multiple [FromBody] or multiple body source types
        if (bodyCount > 1 || (bodyCount > 0 ? 1 : 0) + (formCount > 0 ? 1 : 0) + (streamCount > 0 ? 1 : 0) > 1)
        {
            diagnostics.Add(DiagnosticInfo.Create(
                Descriptors.MultipleBodySources, method.Locations.FirstOrDefault() ?? Location.None, method.Name));
            return ParameterBindingResult.Invalid;
        }

        return BuildEndpointParameters(metas, method.Parameters, routeParameters, method, diagnostics, context, httpVerb);
    }

    private static ParameterMeta[] BuildParameterMetas(
        ImmutableArray<IParameterSymbol> parameters,
        ErrorOrContext context)
    {
        var metas = new ParameterMeta[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
            metas[i] = CreateParameterMeta(parameters[i], context);
        return metas;
    }

    private static ParameterMeta CreateParameterMeta(
        IParameterSymbol parameter,
        ErrorOrContext context)
    {
        var type = parameter.Type;
        var typeFqn = type.GetFullyQualifiedName();

        var flags = BuildFlags(parameter, type, context);
        var specialKind = DetectSpecialKind(type, context);

        var (isCollection, itemType, itemPrimitiveKind) = AnalyzeCollectionType(type, context);
        if (isCollection) flags |= ParameterFlags.Collection;

        // Determine bound name based on explicit attribute or default to parameter name
        var boundName = DetermineBoundName(parameter, flags, context);

        var serviceKey = flags.HasFlag(ParameterFlags.FromKeyedServices)
            ? ExtractKeyFromKeyedServiceAttribute(parameter)
            : null;

        var validatableProperties = flags.HasFlag(ParameterFlags.RequiresValidation)
            ? ErrorOrContext.CollectValidatableProperties(type)
            : default;

        return new ParameterMeta(
            parameter.Name,
            typeFqn,
            TryGetRoutePrimitiveKind(type, context),
            flags,
            specialKind,
            serviceKey,
            boundName,
            itemType?.GetFullyQualifiedName(),
            itemPrimitiveKind,
            DetectCustomBinding(type, context),
            DetectEmptyBodyBehavior(parameter),
            validatableProperties);
    }

    private static ParameterFlags BuildFlags(IParameterSymbol parameter, ITypeSymbol type, ErrorOrContext context)
    {
        var flags = ParameterFlags.None;

        if (HasParameterAttribute(parameter, WellKnownTypes.FromBodyAttribute))
            flags |= ParameterFlags.FromBody;

        if (HasParameterAttribute(parameter, WellKnownTypes.FromRouteAttribute))
            flags |= ParameterFlags.FromRoute;

        if (HasParameterAttribute(parameter, WellKnownTypes.FromQueryAttribute))
            flags |= ParameterFlags.FromQuery;

        if (HasParameterAttribute(parameter, WellKnownTypes.FromHeaderAttribute))
            flags |= ParameterFlags.FromHeader;

        if (HasParameterAttribute(parameter, WellKnownTypes.FromFormAttribute))
            flags |= ParameterFlags.FromForm;

        if (HasParameterAttribute(parameter, WellKnownTypes.FromServicesAttribute))
            flags |= ParameterFlags.FromServices;

        if (HasParameterAttribute(parameter, WellKnownTypes.FromKeyedServicesAttribute))
            flags |= ParameterFlags.FromKeyedServices;

        if (HasParameterAttribute(parameter, WellKnownTypes.AsParametersAttribute))
            flags |= ParameterFlags.AsParameters;

        var (isNullable, isNonNullableValueType) = GetParameterNullability(type, parameter.NullableAnnotation);
        if (isNullable) flags |= ParameterFlags.Nullable;

        if (isNonNullableValueType) flags |= ParameterFlags.NonNullableValueType;

        if (ErrorOrContext.RequiresValidation(type)) flags |= ParameterFlags.RequiresValidation;

        return flags;
    }

    private static SpecialParameterKind DetectSpecialKind(ITypeSymbol type, ErrorOrContext context)
    {
        if (ErrorOrContext.IsHttpContext(type)) return SpecialParameterKind.HttpContext;

        if (ErrorOrContext.IsCancellationToken(type)) return SpecialParameterKind.CancellationToken;

        if (ErrorOrContext.IsFormFile(type)) return SpecialParameterKind.FormFile;

        if (ErrorOrContext.IsFormFileCollection(type)) return SpecialParameterKind.FormFileCollection;

        if (ErrorOrContext.IsFormCollection(type)) return SpecialParameterKind.FormCollection;

        if (ErrorOrContext.IsStream(type)) return SpecialParameterKind.Stream;

        return ErrorOrContext.IsPipeReader(type) ? SpecialParameterKind.PipeReader : SpecialParameterKind.None;
    }

    private static string DetermineBoundName(ISymbol parameter, ParameterFlags flags, ErrorOrContext context)
    {
        // Try to get explicit name from binding attribute
        if (flags.HasFlag(ParameterFlags.FromRoute))
        {
            return TryGetAttributeName(parameter, context, WellKnownTypes.FromRouteAttribute) ??
                   parameter.Name;
        }

        if (flags.HasFlag(ParameterFlags.FromQuery))
        {
            return TryGetAttributeName(parameter, context, WellKnownTypes.FromQueryAttribute) ??
                   parameter.Name;
        }

        if (flags.HasFlag(ParameterFlags.FromHeader))
        {
            return TryGetAttributeName(parameter, context, WellKnownTypes.FromHeaderAttribute) ??
                   parameter.Name;
        }

        if (flags.HasFlag(ParameterFlags.FromForm))
            return TryGetAttributeName(parameter, context, WellKnownTypes.FromFormAttribute) ?? parameter.Name;

        return parameter.Name;
    }

    private static ParameterBindingResult BuildEndpointParameters(
        ParameterMeta[] metas,
        ImmutableArray<IParameterSymbol> parameters,
        ImmutableHashSet<string> routeParameters,
        ISymbol method,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        ErrorOrContext context,
        HttpVerb httpVerb)
    {
        var builder = ImmutableArray.CreateBuilder<EndpointParameter>(metas.Length);
        var isValid = true;

        for (var i = 0; i < metas.Length; i++)
        {
            var result = ClassifyParameter(in metas[i], parameters[i], routeParameters, method, diagnostics, context,
                httpVerb);
            if (result.IsError)
            {
                isValid = false;
                continue;
            }

            builder.Add(result.Parameter);
        }

        return isValid
            ? new ParameterBindingResult(IsValid: true, builder.ToImmutable().AsEquatableArray())
            : ParameterBindingResult.Invalid;
    }

    private static ParameterClassificationResult ClassifyParameter(
        in ParameterMeta meta,
        IParameterSymbol parameter,
        ImmutableHashSet<string> routeParameters,
        ISymbol method,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        ErrorOrContext context,
        HttpVerb httpVerb)
    {
        // Explicit attribute bindings first
        if (meta.HasAsParameters)
            return ClassifyAsParameters(in meta, parameter.Type, routeParameters, method, diagnostics, context, httpVerb);

        if (meta.HasFromBody)
        {
            return ParameterSuccess(in meta, ParameterSource.Body, emptyBodyBehavior: meta.EmptyBodyBehavior,
                validatableProperties: meta.ValidatableProperties);
        }

        if (meta.HasFromForm) return ClassifyFromFormParameter(in meta, parameter.Type, context);

        if (meta.HasFromServices) return ParameterSuccess(in meta, ParameterSource.Service);

        if (meta.HasFromKeyedServices)
        {
            return ParameterSuccess(in meta, ParameterSource.KeyedService,
                keyedServiceKey: meta.ServiceKey);
        }

        if (meta.HasFromHeader) return ClassifyFromHeaderParameter(in meta, method, diagnostics);

        if (meta.HasFromRoute) return ClassifyFromRouteParameter(in meta, routeParameters, method, diagnostics);

        if (meta.HasFromQuery) return ClassifyFromQueryParameter(in meta, method, diagnostics);

        // Special types
        if (meta.IsHttpContext) return ParameterSuccess(in meta, ParameterSource.HttpContext);

        if (meta.IsCancellationToken) return ParameterSuccess(in meta, ParameterSource.CancellationToken);

        // Form file types (implicit binding)
        if (meta.IsFormFile) return ParameterSuccess(in meta, ParameterSource.FormFile, formName: meta.Name);

        if (meta.IsFormFileCollection) return ParameterSuccess(in meta, ParameterSource.FormFiles, formName: meta.Name);

        if (meta.IsFormCollection)
            return ParameterSuccess(in meta, ParameterSource.FormCollection, formName: meta.Name);

        // Stream types
        if (meta.IsStream) return ParameterSuccess(in meta, ParameterSource.Stream);

        if (meta.IsPipeReader) return ParameterSuccess(in meta, ParameterSource.PipeReader);

        // Implicit route binding (parameter name matches route parameter)
        if (routeParameters.Contains(meta.Name)) return ClassifyImplicitRouteParameter(in meta, method, diagnostics);

        // Implicit query binding (primitives and collections of primitives)
        if (meta.RouteKind is not null || meta is { IsCollection: true, CollectionItemPrimitiveKind: not null })
            return ParameterSuccess(in meta, ParameterSource.Query, queryName: meta.Name);

        // Custom binding (TryParse, BindAsync)
        if (meta.CustomBinding != CustomBindingMethod.None)
        {
            if (meta.CustomBinding is CustomBindingMethod.TryParse or CustomBindingMethod.TryParseWithFormat)
            {
                var source = routeParameters.Contains(meta.Name)
                    ? ParameterSource.Route
                    : ParameterSource.Query;
                return ParameterSuccess(in meta, source,
                    source == ParameterSource.Route ? meta.Name : null,
                    queryName: source == ParameterSource.Query ? meta.Name : null,
                    customBinding: meta.CustomBinding);
            }

            return ParameterSuccess(in meta, ParameterSource.Query,
                queryName: meta.Name, customBinding: meta.CustomBinding);
        }

        // Smart inference based on HTTP method and type analysis
        return InferParameterSource(in meta, parameter.Type, httpVerb, method, diagnostics, context);
    }

    /// <summary>
    ///     Infers the parameter source based on HTTP method and type analysis.
    ///     POST/PUT/PATCH with complex types → Body
    ///     Other methods with complex types → Service + EOE021 warning (explicit binding recommended)
    ///     Service types (interfaces, abstract, DI patterns) → Service
    ///     Fallback → Service
    /// </summary>
    private static ParameterClassificationResult InferParameterSource(
        in ParameterMeta meta,
        ITypeSymbol type,
        HttpVerb httpVerb,
        ISymbol method,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        ErrorOrContext context)
    {
        var isLikelyService = IsLikelyServiceType(type);

        // Service types: interfaces, abstract classes, and DI naming patterns
        if (isLikelyService) return ParameterSuccess(in meta, ParameterSource.Service);

        // Check if type is complex (DTO)
        if (IsComplexType(type, context))
        {
            // POST, PUT, PATCH with complex type → Body
            if (!httpVerb.IsBodyless())
            {
                return ParameterSuccess(in meta, ParameterSource.Body, emptyBodyBehavior: meta.EmptyBodyBehavior,
                    validatableProperties: meta.ValidatableProperties);
            }

            // Bodyless/custom methods: do not infer body; warn for DTO-like types.
            diagnostics.Add(DiagnosticInfo.Create(
                Descriptors.AmbiguousParameterBinding,
                method.Locations.FirstOrDefault() ?? Location.None,
                meta.Name,
                meta.TypeFqn,
                httpVerb.ToHttpString()));

            return ParameterSuccess(in meta, ParameterSource.Service);
        }

        // Fallback: treat as service injection (BCL handles resolution at runtime)
        return ParameterSuccess(in meta, ParameterSource.Service);
    }

    /// <summary>
    ///     Classifies [FromRoute] parameter with proper EOE010 diagnostic.
    /// </summary>
    private static ParameterClassificationResult ClassifyFromRouteParameter(
        in ParameterMeta meta,
        ImmutableHashSet<string> routeParameters,
        ISymbol method,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        var hasTryParse = meta.CustomBinding is CustomBindingMethod.TryParse or CustomBindingMethod.TryParseWithFormat;

        // EOE010: [FromRoute] requires primitive or TryParse
        if (meta.RouteKind is null && !hasTryParse)
        {
            diagnostics.Add(DiagnosticInfo.Create(
                Descriptors.InvalidFromRouteType,
                method.Locations.FirstOrDefault() ?? Location.None,
                meta.Name,
                meta.TypeFqn));
            return ParameterClassificationResult.Error;
        }

        return ParameterSuccess(in meta, ParameterSource.Route, meta.BoundName,
            customBinding: meta.CustomBinding);
    }

    /// <summary>
    ///     Classifies implicit route parameter with proper EOE010 diagnostic.
    /// </summary>
    private static ParameterClassificationResult ClassifyImplicitRouteParameter(
        in ParameterMeta meta,
        ISymbol method,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        var hasTryParse = meta.CustomBinding is CustomBindingMethod.TryParse or CustomBindingMethod.TryParseWithFormat;

        // EOE010: Route parameters must use supported primitive types or TryParse
        if (meta.RouteKind is null && !hasTryParse)
        {
            diagnostics.Add(DiagnosticInfo.Create(
                Descriptors.InvalidFromRouteType,
                method.Locations.FirstOrDefault() ?? Location.None,
                meta.Name,
                meta.TypeFqn));
            return ParameterClassificationResult.Error;
        }

        return ParameterSuccess(in meta, ParameterSource.Route, meta.Name, customBinding: meta.CustomBinding);
    }

    /// <summary>
    ///     Classifies [FromQuery] parameter with proper EOE011 diagnostic.
    /// </summary>
    private static ParameterClassificationResult ClassifyFromQueryParameter(
        in ParameterMeta meta,
        ISymbol method,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        // Valid: primitive type
        if (meta.RouteKind is not null)
            return ParameterSuccess(in meta, ParameterSource.Query, queryName: meta.BoundName);

        // Valid: collection of primitives
        if (meta is { IsCollection: true, CollectionItemPrimitiveKind: not null })
            return ParameterSuccess(in meta, ParameterSource.Query, queryName: meta.BoundName);

        // Valid: has TryParse
        if (meta.CustomBinding is CustomBindingMethod.TryParse or CustomBindingMethod.TryParseWithFormat)
        {
            return ParameterSuccess(in meta, ParameterSource.Query, queryName: meta.BoundName,
                customBinding: meta.CustomBinding);
        }

        // EOE011: [FromQuery] only supports primitives or collections of primitives
        diagnostics.Add(DiagnosticInfo.Create(
            Descriptors.InvalidFromQueryType,
            method.Locations.FirstOrDefault() ?? Location.None,
            meta.Name,
            meta.TypeFqn));
        return ParameterClassificationResult.Error;
    }

    /// <summary>
    ///     Classifies [FromHeader] parameter with proper EOE014 diagnostic.
    /// </summary>
    private static ParameterClassificationResult ClassifyFromHeaderParameter(
        in ParameterMeta meta,
        ISymbol method,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        // Valid: primitive type (has implicit TryParse)
        if (meta.RouteKind is not null)
            return ParameterSuccess(in meta, ParameterSource.Header, headerName: meta.BoundName);

        // Valid: collection of strings or primitives
        if (meta is { IsCollection: true, CollectionItemPrimitiveKind: not null })
            return ParameterSuccess(in meta, ParameterSource.Header, headerName: meta.BoundName);

        // Valid: has TryParse
        if (meta.CustomBinding is CustomBindingMethod.TryParse or CustomBindingMethod.TryParseWithFormat)
        {
            return ParameterSuccess(in meta, ParameterSource.Header, headerName: meta.BoundName,
                customBinding: meta.CustomBinding);
        }

        // EOE014: [FromHeader] requires string, primitive with TryParse, or collection thereof
        diagnostics.Add(DiagnosticInfo.Create(
            Descriptors.InvalidFromHeaderType,
            method.Locations.FirstOrDefault() ?? Location.None,
            meta.Name,
            meta.TypeFqn));
        return ParameterClassificationResult.Error;
    }

    private static ParameterClassificationResult ClassifyFromFormParameter(
        in ParameterMeta meta,
        ITypeSymbol type,
        ErrorOrContext context)
    {
        if (meta.IsFormFile) return ParameterSuccess(in meta, ParameterSource.FormFile, formName: meta.BoundName);

        if (meta.IsFormFileCollection)
            return ParameterSuccess(in meta, ParameterSource.FormFiles, formName: meta.BoundName);

        if (meta.IsFormCollection)
            return ParameterSuccess(in meta, ParameterSource.FormCollection, formName: meta.BoundName);

        if (meta.RouteKind is not null || meta is { IsCollection: true, CollectionItemPrimitiveKind: not null })
            return ParameterSuccess(in meta, ParameterSource.Form, formName: meta.BoundName);

        // Complex DTO - let BCL handle form binding
        return ClassifyFormDtoParameter(in meta, type, context);
    }

    private static ParameterClassificationResult ClassifyFormDtoParameter(
        in ParameterMeta meta,
        ITypeSymbol type,
        ErrorOrContext context)
    {
        // For complex form DTOs, analyze the constructor to build child parameter info
        // BCL handles actual binding - we just need structure for code generation
        if (type is not INamedTypeSymbol typeSymbol)
        {
            // Non-named types get simple form binding - BCL will handle/fail at runtime
            return ParameterSuccess(in meta, ParameterSource.Form, formName: meta.BoundName);
        }

        var constructor = typeSymbol.Constructors
            .Where(static c => c.DeclaredAccessibility == Accessibility.Public && !c.IsStatic)
            .OrderByDescending(static c => c.Parameters.Length)
            .FirstOrDefault();

        if (constructor is null || constructor.Parameters.Length is 0)
        {
            // No suitable constructor - simple form binding
            return ParameterSuccess(in meta, ParameterSource.Form, formName: meta.BoundName);
        }

        // Build child parameters for DTO constructor
        var children = ImmutableArray.CreateBuilder<EndpointParameter>(constructor.Parameters.Length);

        foreach (var paramSymbol in constructor.Parameters)
        {
            var childMeta = CreateParameterMeta(paramSymbol, context);

            ParameterSource childSource;
            if (childMeta.IsFormFile)
                childSource = ParameterSource.FormFile;
            else if (childMeta.IsFormFileCollection)
                childSource = ParameterSource.FormFiles;
            else
                childSource = ParameterSource.Form;

            children.Add(new EndpointParameter(
                childMeta.Name,
                childMeta.TypeFqn,
                childSource,
                childMeta.BoundName,
                childMeta.IsNullable,
                childMeta.IsNonNullableValueType,
                childMeta.IsCollection,
                childMeta.CollectionItemTypeFqn,
                default));
        }

        return new ParameterClassificationResult(IsError: false, new EndpointParameter(
            meta.Name,
            meta.TypeFqn,
            ParameterSource.Form,
            meta.BoundName,
            meta.IsNullable,
            meta.IsNonNullableValueType,
IsCollection: false,
CollectionItemTypeFqn: null,
            new EquatableArray<EndpointParameter>(children.ToImmutable()),
            CustomBindingMethod.None,
            meta.RequiresValidation,
            ValidatableProperties: meta.ValidatableProperties));
    }

    /// <summary>
    ///     Classifies [AsParameters] with proper EOE012/EOE013/EOE016/EOE017 diagnostics.
    /// </summary>
    private static ParameterClassificationResult ClassifyAsParameters(
        in ParameterMeta meta,
        ITypeSymbol type,
        ImmutableHashSet<string> routeParameters,
        ISymbol method,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        ErrorOrContext context,
        HttpVerb httpVerb)
    {
        // EOE017: [AsParameters] cannot be nullable
        if (meta.IsNullable)
        {
            diagnostics.Add(DiagnosticInfo.Create(Descriptors.NullableAsParametersNotSupported,
                method.Locations.FirstOrDefault() ?? Location.None, meta.Name));
            return ParameterClassificationResult.Error;
        }

        // EOE012: [AsParameters] can only be used on class or struct types
        if (type is not INamedTypeSymbol typeSymbol)
        {
            diagnostics.Add(DiagnosticInfo.Create(Descriptors.InvalidAsParametersType, method, meta.Name,
                meta.TypeFqn));
            return ParameterClassificationResult.Error;
        }

        var constructor = typeSymbol.Constructors
            .Where(static c => c.DeclaredAccessibility == Accessibility.Public && !c.IsStatic)
            .OrderByDescending(static c => c.Parameters.Length)
            .FirstOrDefault();

        // EOE013: [AsParameters] type must have an accessible constructor
        if (constructor is null)
        {
            diagnostics.Add(DiagnosticInfo.Create(Descriptors.AsParametersNoConstructor, method,
                typeSymbol.ToDisplayString()));
            return ParameterClassificationResult.Error;
        }

        var children = ImmutableArray.CreateBuilder<EndpointParameter>();
        foreach (var paramSymbol in constructor.Parameters)
        {
            var childMeta = CreateParameterMeta(paramSymbol, context);

            // EOE016: Nested [AsParameters] not supported
            if (childMeta.HasAsParameters)
            {
                diagnostics.Add(DiagnosticInfo.Create(Descriptors.NestedAsParametersNotSupported,
                    method.Locations.FirstOrDefault() ?? Location.None,
                    typeSymbol.ToDisplayString(),
                    paramSymbol.Name));
                return ParameterClassificationResult.Error;
            }

            var result = ClassifyParameter(in childMeta, paramSymbol, routeParameters, method, diagnostics, context,
                httpVerb);

            if (result.IsError) return ParameterClassificationResult.Error;

            children.Add(result.Parameter);
        }

        return new ParameterClassificationResult(IsError: false, new EndpointParameter(
            meta.Name,
            meta.TypeFqn,
            ParameterSource.AsParameters,
KeyName: null,
            meta.IsNullable,
            meta.IsNonNullableValueType,
IsCollection: false,
CollectionItemTypeFqn: null,
            new EquatableArray<EndpointParameter>(children.ToImmutable()),
            CustomBindingMethod.None,
            meta.RequiresValidation,
            ValidatableProperties: meta.ValidatableProperties));
    }

    private static ParameterClassificationResult ParameterSuccess(
        in ParameterMeta meta,
        ParameterSource source,
        string? routeName = null,
        string? headerName = null,
        string? queryName = null,
        string? keyedServiceKey = null,
        string? formName = null,
        CustomBindingMethod customBinding = CustomBindingMethod.None,
        EmptyBodyBehavior emptyBodyBehavior = EmptyBodyBehavior.Default,
        EquatableArray<ValidatablePropertyDescriptor> validatableProperties = default)
    {
        return new ParameterClassificationResult(IsError: false, new EndpointParameter(
            meta.Name,
            meta.TypeFqn,
            source,
            routeName ?? queryName ?? headerName ?? keyedServiceKey ?? formName,
            meta.IsNullable,
            meta.IsNonNullableValueType,
            meta.IsCollection,
            meta.CollectionItemTypeFqn,
            default,
            customBinding,
            meta.RequiresValidation,
            emptyBodyBehavior,
            validatableProperties));
    }

    private readonly record struct ParameterClassificationResult(bool IsError, EndpointParameter Parameter)
    {
        public static readonly ParameterClassificationResult Error = new(IsError: true, default);
    }

    private readonly struct AttributeNameMatcher
    {
        private readonly string _fullName;
        private readonly string _shortName;
        private readonly string _shortNameWithoutAttr;

        public AttributeNameMatcher(string fullName)
        {
            _fullName = fullName;
            var lastDot = fullName.LastIndexOf('.');
            _shortName = lastDot >= 0 ? fullName[(lastDot + 1)..] : fullName;
            _shortNameWithoutAttr =
                _shortName.EndsWithOrdinal("Attribute") ? _shortName[..^"Attribute".Length] : _shortName;
        }

        public bool IsMatch(ISymbol? attributeClass)
        {
            if (attributeClass is not ITypeSymbol typeSymbol) return false;

            var display = typeSymbol.GetFullyQualifiedName();

            if (display.StartsWithOrdinal("global::")) display = display[8..];

            // Strict match: Must match FQN or ShortName (if FQN not available/provided)
            // We drop loose EndsWith matching to avoid collisions
            return display == _fullName ||
                   display == _shortName ||
                   display == _shortNameWithoutAttr;
        }
    }
}
