using System.Text.RegularExpressions;
using ANcpLua.Roslyn.Utilities.Matching;
using ANcpLua.Roslyn.Utilities.Models;
using ErrorOr.Analyzers;
using Microsoft.CodeAnalysis;
using SymbolMatch = ANcpLua.Roslyn.Utilities.Matching.Match;

namespace ErrorOr.Generators;

/// <summary>
///     Partial class containing parameter binding logic.
///     Now with proper diagnostic wiring for EOE010-EOE014, EOE024.
/// </summary>
public sealed partial class ErrorOrEndpointGenerator
{
    /// <summary>
    ///     Pattern matchers for DI service detection using ANcpLua.Roslyn.Utilities.
    ///     These patterns identify types that should be resolved from DI container.
    /// </summary>
    private static readonly TypeMatcher ServiceNameMatcher = SymbolMatch.Type()
        .Where(static t => t.Name.EndsWithOrdinal("Service") ||
                           t.Name.EndsWithOrdinal("Repository") ||
                           t.Name.EndsWithOrdinal("Handler") ||
                           t.Name.EndsWithOrdinal("Manager") ||
                           t.Name.EndsWithOrdinal("Provider") ||
                           t.Name.EndsWithOrdinal("Factory") ||
                           t.Name.EndsWithOrdinal("Client"));

    private static readonly TypeMatcher DbContextMatcher = SymbolMatch.Type()
        .Where(static t => t.Name.EndsWithOrdinal("Context") &&
                           (t.Name.Contains("Db") || t.Name.StartsWithOrdinal("Db")));

    internal static ParameterBindingResult BindParameters(
        IMethodSymbol method,
        ImmutableHashSet<string> routeParameters,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        ErrorOrContext context,
        HttpVerb httpVerb)
    {
        if (method.Parameters.Length is 0) return ParameterBindingResult.Empty;

        var metas = BuildParameterMetas(method.Parameters, context, diagnostics);

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

        return BuildEndpointParameters(metas, routeParameters, method, diagnostics, context, httpVerb);
    }

    private static ParameterMeta[] BuildParameterMetas(
        ImmutableArray<IParameterSymbol> parameters,
        ErrorOrContext context,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        var metas = new ParameterMeta[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
            metas[i] = CreateParameterMeta(parameters[i], context, diagnostics);
        return metas;
    }

    private static ParameterMeta CreateParameterMeta(
        IParameterSymbol parameter,
        ErrorOrContext context,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
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
            ? context.CollectValidatableProperties(type)
            : default;

        return new ParameterMeta(
            parameter,
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
            validatableProperties);
    }

    private static ParameterFlags BuildFlags(IParameterSymbol parameter, ITypeSymbol type, ErrorOrContext context)
    {
        var flags = ParameterFlags.None;

        if (HasParameterAttribute(parameter, context.FromBody, WellKnownTypes.FromBodyAttribute))
            flags |= ParameterFlags.FromBody;

        if (HasParameterAttribute(parameter, context.FromRoute, WellKnownTypes.FromRouteAttribute))
            flags |= ParameterFlags.FromRoute;

        if (HasParameterAttribute(parameter, context.FromQuery, WellKnownTypes.FromQueryAttribute))
            flags |= ParameterFlags.FromQuery;

        if (HasParameterAttribute(parameter, context.FromHeader, WellKnownTypes.FromHeaderAttribute))
            flags |= ParameterFlags.FromHeader;

        if (HasParameterAttribute(parameter, context.FromForm, WellKnownTypes.FromFormAttribute))
            flags |= ParameterFlags.FromForm;

        if (HasParameterAttribute(parameter, context.FromServices, WellKnownTypes.FromServicesAttribute))
            flags |= ParameterFlags.FromServices;

        if (HasParameterAttribute(parameter, context.FromKeyedServices, WellKnownTypes.FromKeyedServicesAttribute))
            flags |= ParameterFlags.FromKeyedServices;

        if (HasParameterAttribute(parameter, context.AsParameters, WellKnownTypes.AsParametersAttribute))
            flags |= ParameterFlags.AsParameters;

        var (isNullable, isNonNullableValueType) = GetParameterNullability(type, parameter.NullableAnnotation);
        if (isNullable) flags |= ParameterFlags.Nullable;

        if (isNonNullableValueType) flags |= ParameterFlags.NonNullableValueType;

        if (context.RequiresValidation(type)) flags |= ParameterFlags.RequiresValidation;

        return flags;
    }

    private static SpecialParameterKind DetectSpecialKind(ITypeSymbol type, ErrorOrContext context)
    {
        if (context.IsHttpContext(type)) return SpecialParameterKind.HttpContext;

        if (context.IsCancellationToken(type)) return SpecialParameterKind.CancellationToken;

        if (context.IsFormFile(type)) return SpecialParameterKind.FormFile;

        if (context.IsFormFileCollection(type)) return SpecialParameterKind.FormFileCollection;

        if (context.IsFormCollection(type)) return SpecialParameterKind.FormCollection;

        if (context.IsStream(type)) return SpecialParameterKind.Stream;

        return context.IsPipeReader(type) ? SpecialParameterKind.PipeReader : SpecialParameterKind.None;
    }

    private static string DetermineBoundName(ISymbol parameter, ParameterFlags flags, ErrorOrContext context)
    {
        // Try to get explicit name from binding attribute
        if (flags.HasFlag(ParameterFlags.FromRoute))
            return TryGetAttributeName(parameter, context.FromRoute, WellKnownTypes.FromRouteAttribute) ??
                   parameter.Name;

        if (flags.HasFlag(ParameterFlags.FromQuery))
            return TryGetAttributeName(parameter, context.FromQuery, WellKnownTypes.FromQueryAttribute) ??
                   parameter.Name;

        if (flags.HasFlag(ParameterFlags.FromHeader))
            return TryGetAttributeName(parameter, context.FromHeader, WellKnownTypes.FromHeaderAttribute) ??
                   parameter.Name;

        if (flags.HasFlag(ParameterFlags.FromForm))
            return TryGetAttributeName(parameter, context.FromForm, WellKnownTypes.FromFormAttribute) ?? parameter.Name;

        return parameter.Name;
    }

    private static ParameterBindingResult BuildEndpointParameters(
        ParameterMeta[] metas,
        ImmutableHashSet<string> routeParameters,
        ISymbol method,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        ErrorOrContext context,
        HttpVerb httpVerb)
    {
        var builder = ImmutableArray.CreateBuilder<EndpointParameter>(metas.Length);
        var isValid = true;

        foreach (var meta in metas)
        {
            var result = ClassifyParameter(in meta, routeParameters, method, diagnostics, context, httpVerb);
            if (result.IsError)
            {
                isValid = false;
                continue;
            }

            builder.Add(result.Parameter);
        }

        return isValid
            ? new ParameterBindingResult(true, builder.ToImmutable().AsEquatableArray())
            : ParameterBindingResult.Invalid;
    }

    private static ParameterClassificationResult ClassifyParameter(
        in ParameterMeta meta,
        ImmutableHashSet<string> routeParameters,
        ISymbol method,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        ErrorOrContext context,
        HttpVerb httpVerb)
    {
        // Explicit attribute bindings first
        if (meta.HasAsParameters)
            return ClassifyAsParameters(in meta, routeParameters, method, diagnostics, context, httpVerb);

        if (meta.HasFromBody)
        {
            var behavior = DetectEmptyBodyBehavior(meta.Symbol);
            return ParameterSuccess(in meta, ParameterSource.Body, emptyBodyBehavior: behavior,
                validatableProperties: meta.ValidatableProperties);
        }

        if (meta.HasFromForm) return ClassifyFromFormParameter(in meta, context);

        if (meta.HasFromServices) return ParameterSuccess(in meta, ParameterSource.Service);

        if (meta.HasFromKeyedServices)
            return ParameterSuccess(in meta, ParameterSource.KeyedService,
                keyedServiceKey: meta.ServiceKey);

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
        return InferParameterSource(in meta, httpVerb, method, diagnostics, context);
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
        HttpVerb httpVerb,
        ISymbol method,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        ErrorOrContext context)
    {
        var type = meta.Symbol.Type;

        var isLikelyService = IsLikelyServiceType(type);

        // Service types: interfaces, abstract classes, and DI naming patterns
        if (isLikelyService) return ParameterSuccess(in meta, ParameterSource.Service);

        // Check if type is complex (DTO)
        if (IsComplexType(type, context))
        {
            // POST, PUT, PATCH with complex type → Body
            if (!httpVerb.IsBodyless())
            {
                var behavior = DetectEmptyBodyBehavior(meta.Symbol);
                return ParameterSuccess(in meta, ParameterSource.Body, emptyBodyBehavior: behavior,
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
            return ParameterSuccess(in meta, ParameterSource.Query, queryName: meta.BoundName,
                customBinding: meta.CustomBinding);

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
        // Valid: string type
        if (meta.RouteKind == RoutePrimitiveKind.String)
            return ParameterSuccess(in meta, ParameterSource.Header, headerName: meta.BoundName);

        // Valid: primitive type (has implicit TryParse)
        if (meta.RouteKind is not null)
            return ParameterSuccess(in meta, ParameterSource.Header, headerName: meta.BoundName);

        // Valid: collection of strings or primitives
        if (meta is { IsCollection: true, CollectionItemPrimitiveKind: not null })
            return ParameterSuccess(in meta, ParameterSource.Header, headerName: meta.BoundName);

        // Valid: has TryParse
        if (meta.CustomBinding is CustomBindingMethod.TryParse or CustomBindingMethod.TryParseWithFormat)
            return ParameterSuccess(in meta, ParameterSource.Header, headerName: meta.BoundName,
                customBinding: meta.CustomBinding);

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
        return ClassifyFormDtoParameter(in meta, context);
    }

    private static ParameterClassificationResult ClassifyFormDtoParameter(
        in ParameterMeta meta,
        ErrorOrContext context)
    {
        // For complex form DTOs, analyze the constructor to build child parameter info
        // BCL handles actual binding - we just need structure for code generation
        if (meta.Symbol.Type is not INamedTypeSymbol typeSymbol)
            // Non-named types get simple form binding - BCL will handle/fail at runtime
            return ParameterSuccess(in meta, ParameterSource.Form, formName: meta.BoundName);

        var constructor = typeSymbol.Constructors
            .Where(static c => c.DeclaredAccessibility == Accessibility.Public && !c.IsStatic)
            .OrderByDescending(static c => c.Parameters.Length)
            .FirstOrDefault();

        if (constructor is null || constructor.Parameters.Length is 0)
            // No suitable constructor - simple form binding
            return ParameterSuccess(in meta, ParameterSource.Form, formName: meta.BoundName);

        // Build child parameters for DTO constructor
        var children = ImmutableArray.CreateBuilder<EndpointParameter>(constructor.Parameters.Length);
        var dummyDiagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

        foreach (var paramSymbol in constructor.Parameters)
        {
            var childMeta = CreateParameterMeta(paramSymbol, context, dummyDiagnostics);

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

        return new ParameterClassificationResult(false, new EndpointParameter(
            meta.Name,
            meta.TypeFqn,
            ParameterSource.Form,
            meta.BoundName,
            meta.IsNullable,
            meta.IsNonNullableValueType,
            false,
            null,
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
        if (meta.Symbol.Type is not INamedTypeSymbol typeSymbol)
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
            var childMeta = CreateParameterMeta(paramSymbol, context, diagnostics);

            // EOE016: Nested [AsParameters] not supported
            if (childMeta.HasAsParameters)
            {
                diagnostics.Add(DiagnosticInfo.Create(Descriptors.NestedAsParametersNotSupported,
                    method.Locations.FirstOrDefault() ?? Location.None,
                    typeSymbol.ToDisplayString(),
                    paramSymbol.Name));
                return ParameterClassificationResult.Error;
            }

            var result = ClassifyParameter(in childMeta, routeParameters, method, diagnostics, context, httpVerb);

            if (result.IsError) return ParameterClassificationResult.Error;

            children.Add(result.Parameter);
        }

        return new ParameterClassificationResult(false, new EndpointParameter(
            meta.Name,
            meta.TypeFqn,
            ParameterSource.AsParameters,
            null,
            meta.IsNullable,
            meta.IsNonNullableValueType,
            false,
            null,
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
        return new ParameterClassificationResult(false, new EndpointParameter(
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

    private static EmptyBodyBehavior DetectEmptyBodyBehavior(ISymbol parameter)
    {
        return parameter.HasAttributeByShortName("AllowEmptyBody")
            ? EmptyBodyBehavior.Allow
            : EmptyBodyBehavior.Default;
    }

    private static RoutePrimitiveKind? TryGetRoutePrimitiveKind(ITypeSymbol type, ErrorOrContext context)
    {
        type = type.UnwrapNullable();

        return type.SpecialType switch
        {
            SpecialType.System_String => RoutePrimitiveKind.String,
            SpecialType.System_Int32 => RoutePrimitiveKind.Int32,
            SpecialType.System_Int64 => RoutePrimitiveKind.Int64,
            SpecialType.System_Int16 => RoutePrimitiveKind.Int16,
            SpecialType.System_Byte => RoutePrimitiveKind.Byte,
            SpecialType.System_SByte => RoutePrimitiveKind.SByte,
            SpecialType.System_UInt32 => RoutePrimitiveKind.UInt32,
            SpecialType.System_UInt64 => RoutePrimitiveKind.UInt64,
            SpecialType.System_UInt16 => RoutePrimitiveKind.UInt16,
            SpecialType.System_Boolean => RoutePrimitiveKind.Boolean,
            SpecialType.System_Decimal => RoutePrimitiveKind.Decimal,
            SpecialType.System_Double => RoutePrimitiveKind.Double,
            SpecialType.System_Single => RoutePrimitiveKind.Single,
            _ => TryGetRoutePrimitiveKindBySymbol(type, context)
        };
    }

    private static RoutePrimitiveKind? TryGetRoutePrimitiveKindBySymbol(ISymbol type, ErrorOrContext context)
    {
        if (context.Guid is not null && type.IsEqualTo(context.Guid)) return RoutePrimitiveKind.Guid;

        if (context.DateTime is not null && type.IsEqualTo(context.DateTime)) return RoutePrimitiveKind.DateTime;

        if (context.DateTimeOffset is not null && type.IsEqualTo(context.DateTimeOffset))
            return RoutePrimitiveKind.DateTimeOffset;

        if (context.DateOnly is not null && type.IsEqualTo(context.DateOnly)) return RoutePrimitiveKind.DateOnly;

        if (context.TimeOnly is not null && type.IsEqualTo(context.TimeOnly)) return RoutePrimitiveKind.TimeOnly;

        if (context.TimeSpan is not null && type.IsEqualTo(context.TimeSpan)) return RoutePrimitiveKind.TimeSpan;

        return null;
    }

    private static CustomBindingMethod DetectCustomBinding(ITypeSymbol type, ErrorOrContext context)
    {
        if (type is not INamedTypeSymbol namedType || IsPrimitiveOrWellKnownType(namedType, context))
            return CustomBindingMethod.None;

        if (ImplementsBindableInterface(namedType, context)) return CustomBindingMethod.Bindable;

        var bindAsyncMethod = DetectBindAsyncMethod(namedType, context);
        return bindAsyncMethod != CustomBindingMethod.None ? bindAsyncMethod : DetectTryParseMethod(namedType, context);
    }

    private static bool IsPrimitiveOrWellKnownType(ITypeSymbol type, ErrorOrContext context)
    {
        type = type.UnwrapNullable();

        // Check for primitive types (int, string, bool, etc.)
        if (type.SpecialType is not SpecialType.None) return true;

        // Check for well-known non-primitive types that have built-in conversions
        return TryGetRoutePrimitiveKindBySymbol(type, context) is not null;
    }

    private static bool ImplementsBindableInterface(ITypeSymbol type, ErrorOrContext context)
    {
        return context.BindableFromHttpContext is not null && type.IsOrImplements(context.BindableFromHttpContext);
    }

    private static CustomBindingMethod DetectBindAsyncMethod(INamespaceOrTypeSymbol type, ErrorOrContext context)
    {
        foreach (var member in type.GetMembers("BindAsync"))
        {
            var result = ClassifyBindAsyncMember(member, context);
            if (result != CustomBindingMethod.None) return result;
        }

        return CustomBindingMethod.None;
    }

    private static CustomBindingMethod ClassifyBindAsyncMember(ISymbol member, ErrorOrContext context)
    {
        if (member is not IMethodSymbol { IsStatic: true, ReturnsVoid: false } method ||
            !context.Awaitable.IsTaskLike(method.ReturnType) || method.Parameters.Length < 1 ||
            !context.IsHttpContext(method.Parameters[0].Type))
            return CustomBindingMethod.None;

        if (method.Parameters.Length >= 2 && context.IsParameterInfo(method.Parameters[1].Type))
            return CustomBindingMethod.BindAsyncWithParam;

        return CustomBindingMethod.BindAsync;
    }

    private static CustomBindingMethod DetectTryParseMethod(INamespaceOrTypeSymbol type, ErrorOrContext context)
    {
        foreach (var member in type.GetMembers("TryParse"))
        {
            var result = ClassifyTryParseMember(member, context);
            if (result != CustomBindingMethod.None) return result;
        }

        return CustomBindingMethod.None;
    }

    private static CustomBindingMethod ClassifyTryParseMember(ISymbol member, ErrorOrContext context)
    {
        if (member is not IMethodSymbol { IsStatic: true, ReturnType.SpecialType: SpecialType.System_Boolean } method ||
            method.Parameters.Length < 2 || !IsStringOrCharSpan(method.Parameters[0].Type, context) ||
            method.Parameters[^1].RefKind != RefKind.Out)
            return CustomBindingMethod.None;

        if (method.Parameters.Length >= 3)
            for (var i = 1; i < method.Parameters.Length - 1; i++)
                if (IsFormatProvider(method.Parameters[i].Type, context))
                    return CustomBindingMethod.TryParseWithFormat;

        return CustomBindingMethod.TryParse;
    }

    private static bool IsStringOrCharSpan(ITypeSymbol type, ErrorOrContext context)
    {
        if (type.SpecialType == SpecialType.System_String) return true;

        if (type is INamedTypeSymbol { IsGenericType: true } named && context.ReadOnlySpanOfT is not null)
            return named.ConstructedFrom.IsEqualTo(context.ReadOnlySpanOfT) &&
                   named.TypeArguments is [{ SpecialType: SpecialType.System_Char }];

        return false;
    }

    private static bool IsFormatProvider(ITypeSymbol type, ErrorOrContext context)
    {
        type = type.UnwrapNullable();
        if (context.IFormatProvider is not null) return type.IsEqualTo(context.IFormatProvider);

        return type.Name == "IFormatProvider" &&
               type.ContainingNamespace.ToDisplayString() == "System";
    }

    private static (bool IsCollection, ITypeSymbol? ItemType, RoutePrimitiveKind? Kind) AnalyzeCollectionType(
        ITypeSymbol type, ErrorOrContext context)
    {
        type = type.UnwrapNullable();
        if (type.SpecialType == SpecialType.System_String) return (false, null, null);

        ITypeSymbol? itemType = null;
        if (type is IArrayTypeSymbol arrayType)
        {
            itemType = arrayType.ElementType;
        }
        else if (type is INamedTypeSymbol { IsGenericType: true } named)
        {
            var origin = named.ConstructedFrom;
            if (IsWellKnownCollection(origin, context)) itemType = named.TypeArguments[0];
        }

        return itemType is not null
            ? (true, itemType, TryGetRoutePrimitiveKind(itemType, context))
            : (false, null, null);
    }

    private static bool IsWellKnownCollection(ISymbol origin, ErrorOrContext context)
    {
        return (context.ListOfT is not null && origin.IsEqualTo(context.ListOfT)) ||
               (context.IListOfT is not null && origin.IsEqualTo(context.IListOfT)) ||
               (context.IEnumerableOfT is not null &&
                origin.IsEqualTo(context.IEnumerableOfT)) ||
               (context.IReadOnlyListOfT is not null &&
                origin.IsEqualTo(context.IReadOnlyListOfT)) ||
               (context.ICollectionOfT is not null &&
                origin.IsEqualTo(context.ICollectionOfT)) ||
               (context.HashSetOfT is not null && origin.IsEqualTo(context.HashSetOfT));
    }

    private static (bool IsNullable, bool IsNonNullableValueType) GetParameterNullability(
        ITypeSymbol type,
        NullableAnnotation annotation)
    {
        if (type.IsReferenceType) return (annotation == NullableAnnotation.Annotated, false);

        return type is INamedTypeSymbol
        {
            IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Nullable_T
        }
            ? (true, false)
            : (false, true);
    }

    private static string? ExtractKeyFromKeyedServiceAttribute(ISymbol parameter)
    {
        var matcher = new AttributeNameMatcher(WellKnownTypes.FromKeyedServicesAttribute);
        var attr = parameter.GetAttributes().FirstOrDefault(a => matcher.IsMatch(a.AttributeClass));

        if (attr is null || attr.ConstructorArguments.Length is 0) return null;

        var val = attr.ConstructorArguments[0].Value;
        return val switch { string s => $"\"{s}\"", null => null, _ => val.ToString() };
    }

    private static bool HasParameterAttribute(ISymbol parameter, INamedTypeSymbol? attributeSymbol,
        string attributeName)
    {
        var attributes = parameter.GetAttributes();

        // Pattern: is { } attrClass - guards null before IsEqualTo call
        if (attributeSymbol is not null && attributes.Any(attr =>
                attr.AttributeClass is { } attrClass && attrClass.IsEqualTo(attributeSymbol)))
            return true;

        var matcher = new AttributeNameMatcher(attributeName);
        return attributes.Any(attr => matcher.IsMatch(attr.AttributeClass));
    }

    private static string? TryGetAttributeName(ISymbol parameter, INamedTypeSymbol? attributeSymbol,
        string attributeName)
    {
        var attributes = parameter.GetAttributes();

        if (attributeSymbol is not null)
        {
            // Pattern: is { } attrClass - guards null before IsEqualTo call
            var attr = attributes.FirstOrDefault(a =>
                a.AttributeClass is { } attrClass && attrClass.IsEqualTo(attributeSymbol));
            if (attr is not null) return ExtractNameFromAttribute(attr);
        }

        var matcher = new AttributeNameMatcher(attributeName);
        var matchingAttr = attributes.FirstOrDefault(attr => matcher.IsMatch(attr.AttributeClass));
        return matchingAttr is not null ? ExtractNameFromAttribute(matchingAttr) : null;
    }

    private static string? ExtractNameFromAttribute(AttributeData? attr)
    {
        if (attr is null) return null;

        foreach (var namedArg in attr.NamedArguments)
            if (string.Equals(namedArg.Key, "Name", StringComparison.OrdinalIgnoreCase) &&
                namedArg.Value.Value is string name && !string.IsNullOrWhiteSpace(name))
                return name;

        if (attr.ConstructorArguments.Length > 0 &&
            attr.ConstructorArguments[0].Value is string ctorArg &&
            !string.IsNullOrWhiteSpace(ctorArg))
            return ctorArg;

        if (attr.ApplicationSyntaxReference?.GetSyntax() is { } syntax)
        {
            var syntaxText = syntax.ToString();
            var nameMatch = Regex.Match(syntaxText, """
                                                    Name\s*=\s*"(?<val>[^"]+)"
                                                    """, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture,
                TimeSpan.FromSeconds(1));
            if (nameMatch.Success) return nameMatch.Groups["val"].Value;
        }

        return null;
    }

    /// <summary>
    ///     Detects if a type is likely a DI service based on naming conventions.
    ///     Uses composable type matchers from ANcpLua.Roslyn.Utilities.
    /// </summary>
    private static bool IsLikelyServiceType(ITypeSymbol type)
    {
        // Interfaces are typically services
        if (type.TypeKind == TypeKind.Interface) return true;

        // Abstract types are typically services
        if (type.IsAbstract) return true;

        // Check using fluent matchers for common DI naming patterns
        if (type is INamedTypeSymbol namedType)
            if (ServiceNameMatcher.Matches(namedType) || DbContextMatcher.Matches(namedType))
                return true;

        return false;
    }

    /// <summary>
    ///     Determines if a type is a complex type (DTO) that should be bound from body.
    ///     Returns true for types that are NOT: primitives, special types, route-bindable, or collections of primitives.
    /// </summary>
    private static bool IsComplexType(ITypeSymbol type, ErrorOrContext context)
    {
        type = type.UnwrapNullable();

        // Primitives are not complex
        if (type.SpecialType is not SpecialType.None) return false;

        // Well-known types (Guid, DateTime, etc.) are not complex
        if (TryGetRoutePrimitiveKindBySymbol(type, context) is not null) return false;

        // Form file types are not complex (have special binding)
        if (context.IsFormFile(type) || context.IsFormFileCollection(type) || context.IsFormCollection(type))
            return false;

        // Stream types are not complex (have special binding)
        if (context.IsStream(type) || context.IsPipeReader(type)) return false;

        // HttpContext, CancellationToken are not complex
        if (context.IsHttpContext(type) || context.IsCancellationToken(type)) return false;

        // Types with TryParse or BindAsync are route-bindable, not complex
        if (type is INamedTypeSymbol namedType && !IsPrimitiveOrWellKnownType(namedType, context))
        {
            var customBinding = DetectCustomBinding(namedType, context);
            if (customBinding != CustomBindingMethod.None) return false;
        }

        // Collections of primitives are not complex
        var (isCollection, _, itemKind) = AnalyzeCollectionType(type, context);
        if (isCollection && itemKind is not null) return false;

        // Interface or abstract types are services, not complex DTOs
        if (type.TypeKind == TypeKind.Interface || type.IsAbstract) return false;

        // Service types by naming convention are not complex DTOs
        return !IsLikelyServiceType(type);
        // Everything else is complex (DTOs, records, classes)
    }

    private readonly record struct ParameterClassificationResult(bool IsError, EndpointParameter Parameter)
    {
        public static readonly ParameterClassificationResult Error = new(true, default);
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
