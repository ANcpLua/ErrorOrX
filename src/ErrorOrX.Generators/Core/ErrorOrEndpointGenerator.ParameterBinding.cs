using System.Collections.Immutable;
using System.Text.RegularExpressions;
using ANcpLua.Roslyn.Utilities;
using ANcpLua.Roslyn.Utilities.Models;
using ErrorOr.Analyzers;
using Microsoft.CodeAnalysis;

namespace ErrorOr.Generators;

/// <summary>
///     Partial class containing parameter binding logic.
///     Now with proper diagnostic wiring for EOE011-EOE016, EOE024.
/// </summary>
public sealed partial class ErrorOrEndpointGenerator
{
    internal static ParameterBindingResult BindParameters(
        IMethodSymbol method,
        ImmutableHashSet<string> routeParameters,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        ErrorOrContext context)
    {
        if (method.Parameters.Length is 0)
            return ParameterBindingResult.Empty;

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

        return BuildEndpointParameters(metas, routeParameters, method, diagnostics, context);
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

    // ReSharper disable once UnusedParameter.Local - reserved for future diagnostics
    private static ParameterMeta CreateParameterMeta(
        IParameterSymbol parameter,
        ErrorOrContext context,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        var type = parameter.Type;
        var typeFqn = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var hasFromRoute = HasParameterAttribute(parameter, context.FromRoute, WellKnownTypes.FromRouteAttribute);
        var hasFromQuery = HasParameterAttribute(parameter, context.FromQuery, WellKnownTypes.FromQueryAttribute);
        var hasFromHeader =
            HasParameterAttribute(parameter, context.FromHeader, WellKnownTypes.FromHeaderAttribute);
        var hasFromKeyedServices = HasParameterAttribute(parameter, context.FromKeyedServices,
            WellKnownTypes.FromKeyedServicesAttribute);
        var hasAsParameters =
            HasParameterAttribute(parameter, context.AsParameters, WellKnownTypes.AsParametersAttribute);
        var hasFromForm = HasParameterAttribute(parameter, context.FromForm, WellKnownTypes.FromFormAttribute);

        var routeName = hasFromRoute
            ? TryGetAttributeName(parameter, context.FromRoute, WellKnownTypes.FromRouteAttribute) ??
              parameter.Name
            : parameter.Name;
        var queryName = hasFromQuery
            ? TryGetAttributeName(parameter, context.FromQuery, WellKnownTypes.FromQueryAttribute) ??
              parameter.Name
            : parameter.Name;
        var headerName = hasFromHeader
            ? TryGetAttributeName(parameter, context.FromHeader, WellKnownTypes.FromHeaderAttribute) ??
              parameter.Name
            : parameter.Name;
        var formName = hasFromForm
            ? TryGetAttributeName(parameter, context.FromForm, WellKnownTypes.FromFormAttribute) ?? parameter.Name
            : parameter.Name;
        var keyedServiceKey =
            hasFromKeyedServices ? ExtractKeyFromKeyedServiceAttribute(parameter, context) : null;

        var (isNullable, isNonNullableValueType) = GetParameterNullability(type, parameter.NullableAnnotation);
        var (isCollection, itemType, itemPrimitiveKind) = AnalyzeCollectionType(type, context);

        var isFormFile = context.IsFormFile(type);
        var isFormFileCollection = context.IsFormFileCollection(type);
        var isFormCollection = context.IsFormCollection(type);
        var isStream = context.IsStream(type);
        var isPipeReader = context.IsPipeReader(type);
        var isCancellationToken = context.IsCancellationToken(type);
        var isHttpContext = context.IsHttpContext(type);

        // Check if type requires BCL validation (has ValidationAttribute descendants or implements IValidatableObject)
        var requiresValidation = context.RequiresValidation(type);

        return new ParameterMeta(
            parameter, parameter.Name, typeFqn, TryGetRoutePrimitiveKind(type, context),
            HasParameterAttribute(parameter, context.FromServices, WellKnownTypes.FromServicesAttribute),
            hasFromKeyedServices, keyedServiceKey,
            HasParameterAttribute(parameter, context.FromBody, WellKnownTypes.FromBodyAttribute),
            hasFromRoute, hasFromQuery, hasFromHeader, hasAsParameters,
            routeName, queryName, headerName,
            isCancellationToken,
            isHttpContext,
            isNullable, isNonNullableValueType,
            isCollection, itemType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), itemPrimitiveKind,
            hasFromForm, formName, isFormFile, isFormFileCollection, isFormCollection,
            isStream, isPipeReader,
            DetectCustomBinding(type, context),
            requiresValidation);
    }

    private static ParameterBindingResult BuildEndpointParameters(
        IReadOnlyCollection<ParameterMeta> metas,
        ImmutableHashSet<string> routeParameters,
        IMethodSymbol method,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        ErrorOrContext context)
    {
        var builder = ImmutableArray.CreateBuilder<EndpointParameter>(metas.Count);
        var isValid = true;

        foreach (var meta in metas)
        {
            var result = ClassifyParameter(in meta, routeParameters, method, diagnostics, context);
            if (result.IsError)
            {
                isValid = false;
                continue;
            }

            builder.Add(result.Parameter);
        }

        return isValid
            ? new ParameterBindingResult(true, builder.ToImmutable())
            : ParameterBindingResult.Invalid;
    }

    private static ParameterClassificationResult ClassifyParameter(
        in ParameterMeta meta,
        ImmutableHashSet<string> routeParameters,
        IMethodSymbol method,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        ErrorOrContext context)
    {
        // Explicit attribute bindings first
        if (meta.HasAsParameters)
            return ClassifyAsParameters(in meta, routeParameters, method, diagnostics, context);
        if (meta.HasFromBody)
            return ParameterSuccess(in meta, EndpointParameterSource.Body);
        if (meta.HasFromForm)
            return ClassifyFromFormParameter(in meta, method, diagnostics, context);
        if (meta.HasFromServices)
            return ParameterSuccess(in meta, EndpointParameterSource.Service);
        if (meta.HasFromKeyedServices)
            return ParameterSuccess(in meta, EndpointParameterSource.KeyedService,
                keyedServiceKey: meta.KeyedServiceKey);

        if (meta.HasFromHeader)
            return ClassifyFromHeaderParameter(in meta, method, diagnostics);
        if (meta.HasFromRoute)
            return ClassifyFromRouteParameter(in meta, routeParameters, method, diagnostics);
        if (meta.HasFromQuery)
            return ClassifyFromQueryParameter(in meta, method, diagnostics);

        // Special types
        if (meta.IsHttpContext)
            return ParameterSuccess(in meta, EndpointParameterSource.HttpContext);
        if (meta.IsCancellationToken)
            return ParameterSuccess(in meta, EndpointParameterSource.CancellationToken);

        // Form file types (implicit binding)
        if (meta.IsFormFile)
            return ParameterSuccess(in meta, EndpointParameterSource.FormFile, formName: meta.Name);
        if (meta.IsFormFileCollection)
            return ParameterSuccess(in meta, EndpointParameterSource.FormFiles, formName: meta.Name);
        if (meta.IsFormCollection)
            return ParameterSuccess(in meta, EndpointParameterSource.FormCollection, formName: meta.Name);

        // Stream types
        if (meta.IsStream)
            return ParameterSuccess(in meta, EndpointParameterSource.Stream);
        if (meta.IsPipeReader)
            return ParameterSuccess(in meta, EndpointParameterSource.PipeReader);

        // Implicit route binding (parameter name matches route parameter)
        if (routeParameters.Contains(meta.Name))
            return ClassifyImplicitRouteParameter(in meta, method, diagnostics);

        // Implicit query binding (primitives and collections of primitives)
        if (meta.RouteKind is not null || meta is { IsCollection: true, CollectionItemPrimitiveKind: not null })
            return ParameterSuccess(in meta, EndpointParameterSource.Query, queryName: meta.Name);

        // Custom binding (TryParse, BindAsync)
        if (meta.CustomBinding != CustomBindingMethod.None)
        {
            if (meta.CustomBinding is CustomBindingMethod.TryParse or CustomBindingMethod.TryParseWithFormat)
            {
                var source = routeParameters.Contains(meta.Name)
                    ? EndpointParameterSource.Route
                    : EndpointParameterSource.Query;
                return ParameterSuccess(in meta, source,
                    source == EndpointParameterSource.Route ? meta.Name : null,
                    queryName: source == EndpointParameterSource.Query ? meta.Name : null,
                    customBinding: meta.CustomBinding);
            }

            return ParameterSuccess(in meta, EndpointParameterSource.Query,
                queryName: meta.Name, customBinding: meta.CustomBinding);
        }

        // Fallback: treat as service injection (BCL handles resolution at runtime)
        return ParameterSuccess(in meta, EndpointParameterSource.Service);
    }

    /// <summary>
    ///     Classifies [FromRoute] parameter with proper EOE011 diagnostic.
    /// </summary>
    private static ParameterClassificationResult ClassifyFromRouteParameter(
        in ParameterMeta meta,
        ImmutableHashSet<string> routeParameters,
        ISymbol method,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        _ = routeParameters; // Reserved for future validation of route parameter existence
        var hasTryParse = meta.CustomBinding is CustomBindingMethod.TryParse or CustomBindingMethod.TryParseWithFormat;

        // EOE011: [FromRoute] requires primitive or TryParse
        if (meta.RouteKind is null && !hasTryParse)
        {
            diagnostics.Add(DiagnosticInfo.Create(
                Descriptors.InvalidFromRouteType,
                method.Locations.FirstOrDefault() ?? Location.None,
                meta.Name,
                meta.TypeFqn));
            return ParameterClassificationResult.Error;
        }

        return ParameterSuccess(in meta, EndpointParameterSource.Route, meta.RouteName,
            customBinding: meta.CustomBinding);
    }

    /// <summary>
    ///     Classifies implicit route parameter with proper EOE011 diagnostic.
    /// </summary>
    private static ParameterClassificationResult ClassifyImplicitRouteParameter(
        in ParameterMeta meta,
        ISymbol method,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        var hasTryParse = meta.CustomBinding is CustomBindingMethod.TryParse or CustomBindingMethod.TryParseWithFormat;

        // EOE011: Route parameters must use supported primitive types or TryParse
        if (meta.RouteKind is null && !hasTryParse)
        {
            diagnostics.Add(DiagnosticInfo.Create(
                Descriptors.InvalidFromRouteType,
                method.Locations.FirstOrDefault() ?? Location.None,
                meta.Name,
                meta.TypeFqn));
            return ParameterClassificationResult.Error;
        }

        return ParameterSuccess(in meta, EndpointParameterSource.Route, meta.Name, customBinding: meta.CustomBinding);
    }

    /// <summary>
    ///     Classifies [FromQuery] parameter with proper EOE012 diagnostic.
    /// </summary>
    private static ParameterClassificationResult ClassifyFromQueryParameter(
        in ParameterMeta meta,
        ISymbol method,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        // Valid: primitive type
        if (meta.RouteKind is not null)
            return ParameterSuccess(in meta, EndpointParameterSource.Query, queryName: meta.QueryName);

        // Valid: collection of primitives
        if (meta is { IsCollection: true, CollectionItemPrimitiveKind: not null })
            return ParameterSuccess(in meta, EndpointParameterSource.Query, queryName: meta.QueryName);

        // Valid: has TryParse
        if (meta.CustomBinding is CustomBindingMethod.TryParse or CustomBindingMethod.TryParseWithFormat)
            return ParameterSuccess(in meta, EndpointParameterSource.Query, queryName: meta.QueryName,
                customBinding: meta.CustomBinding);

        // EOE012: [FromQuery] only supports primitives or collections of primitives
        diagnostics.Add(DiagnosticInfo.Create(
            Descriptors.InvalidFromQueryType,
            method.Locations.FirstOrDefault() ?? Location.None,
            meta.Name,
            meta.TypeFqn));
        return ParameterClassificationResult.Error;
    }

    /// <summary>
    ///     Classifies [FromHeader] parameter with proper EOE016 diagnostic.
    /// </summary>
    private static ParameterClassificationResult ClassifyFromHeaderParameter(
        in ParameterMeta meta,
        ISymbol method,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        // Valid: string type
        if (meta.RouteKind == RoutePrimitiveKind.String)
            return ParameterSuccess(in meta, EndpointParameterSource.Header, headerName: meta.HeaderName);

        // Valid: primitive type (has implicit TryParse)
        if (meta.RouteKind is not null)
            return ParameterSuccess(in meta, EndpointParameterSource.Header, headerName: meta.HeaderName);

        // Valid: collection of strings or primitives
        if (meta is { IsCollection: true, CollectionItemPrimitiveKind: not null })
            return ParameterSuccess(in meta, EndpointParameterSource.Header, headerName: meta.HeaderName);

        // Valid: has TryParse
        if (meta.CustomBinding is CustomBindingMethod.TryParse or CustomBindingMethod.TryParseWithFormat)
            return ParameterSuccess(in meta, EndpointParameterSource.Header, headerName: meta.HeaderName,
                customBinding: meta.CustomBinding);

        // EOE016: [FromHeader] requires string, primitive with TryParse, or collection thereof
        diagnostics.Add(DiagnosticInfo.Create(
            Descriptors.InvalidFromHeaderType,
            method.Locations.FirstOrDefault() ?? Location.None,
            meta.Name,
            meta.TypeFqn));
        return ParameterClassificationResult.Error;
    }

    // ReSharper disable once UnusedParameter.Local - matches common classifier delegate signature
    private static ParameterClassificationResult ClassifyFromFormParameter(
            in ParameterMeta meta,
            IMethodSymbol _,
            ImmutableArray<DiagnosticInfo>.Builder __,
            ErrorOrContext context)
        // ReSharper restore UnusedParameter.Local
    {
        if (meta.IsFormFile)
            return ParameterSuccess(in meta, EndpointParameterSource.FormFile, formName: meta.FormName);
        if (meta.IsFormFileCollection)
            return ParameterSuccess(in meta, EndpointParameterSource.FormFiles, formName: meta.FormName);
        if (meta.IsFormCollection)
            return ParameterSuccess(in meta, EndpointParameterSource.FormCollection, formName: meta.FormName);
        if (meta.RouteKind is not null || meta is { IsCollection: true, CollectionItemPrimitiveKind: not null })
            return ParameterSuccess(in meta, EndpointParameterSource.Form, formName: meta.FormName);

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
            return ParameterSuccess(in meta, EndpointParameterSource.Form, formName: meta.FormName);

        var constructor = typeSymbol.Constructors
            .Where(static c => c.DeclaredAccessibility == Accessibility.Public && !c.IsStatic)
            .OrderByDescending(static c => c.Parameters.Length)
            .FirstOrDefault();

        if (constructor is null || constructor.Parameters.Length is 0)
            // No suitable constructor - simple form binding
            return ParameterSuccess(in meta, EndpointParameterSource.Form, formName: meta.FormName);

        // Build child parameters for DTO constructor
        var children = ImmutableArray.CreateBuilder<EndpointParameter>(constructor.Parameters.Length);
        var dummyDiagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

        foreach (var paramSymbol in constructor.Parameters)
        {
            var childMeta = CreateParameterMeta(paramSymbol, context, dummyDiagnostics);

            EndpointParameterSource childSource;
            if (childMeta.IsFormFile)
                childSource = EndpointParameterSource.FormFile;
            else if (childMeta.IsFormFileCollection)
                childSource = EndpointParameterSource.FormFiles;
            else
                childSource = EndpointParameterSource.Form;

            children.Add(new EndpointParameter(
                childMeta.Name,
                childMeta.TypeFqn,
                childSource,
                childMeta.FormName,
                childMeta.IsNullable,
                childMeta.IsNonNullableValueType,
                childMeta.IsCollection,
                childMeta.CollectionItemTypeFqn,
                default));
        }

        return new ParameterClassificationResult(false, new EndpointParameter(
            meta.Name,
            meta.TypeFqn,
            EndpointParameterSource.Form,
            meta.FormName,
            meta.IsNullable,
            meta.IsNonNullableValueType,
            false,
            null,
            new EquatableArray<EndpointParameter>(children.ToImmutable()),
            CustomBindingMethod.None,
            meta.RequiresValidation));
    }

    /// <summary>
    ///     Classifies [AsParameters] with proper EOE013/EOE014 diagnostics.
    /// </summary>
    private static ParameterClassificationResult ClassifyAsParameters(
        in ParameterMeta meta,
        ImmutableHashSet<string> routeParameters,
        IMethodSymbol method,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        ErrorOrContext context)
    {
        // EOE013: [AsParameters] can only be used on class or struct types
        if (meta.Symbol.Type is not INamedTypeSymbol typeSymbol)
        {
            diagnostics.Add(DiagnosticInfo.Create(Descriptors.InvalidAsParametersType, method, meta.Name, meta.TypeFqn));
            return ParameterClassificationResult.Error;
        }

        var constructor = typeSymbol.Constructors
            .Where(static c => c.DeclaredAccessibility == Accessibility.Public && !c.IsStatic)
            .OrderByDescending(static c => c.Parameters.Length)
            .FirstOrDefault();

        // EOE014: [AsParameters] type must have an accessible constructor
        if (constructor is null)
        {
            diagnostics.Add(DiagnosticInfo.Create(Descriptors.AsParametersNoConstructor, method, typeSymbol.ToDisplayString()));
            return ParameterClassificationResult.Error;
        }

        var children = ImmutableArray.CreateBuilder<EndpointParameter>();
        foreach (var paramSymbol in constructor.Parameters)
        {
            var childMeta = CreateParameterMeta(paramSymbol, context, diagnostics);
            var result = ClassifyParameter(in childMeta, routeParameters, method, diagnostics, context);

            if (result.IsError)
                return ParameterClassificationResult.Error;

            children.Add(result.Parameter);
        }

        return new ParameterClassificationResult(false, new EndpointParameter(
            meta.Name,
            meta.TypeFqn,
            EndpointParameterSource.AsParameters,
            null,
            meta.IsNullable,
            meta.IsNonNullableValueType,
            false,
            null,
            new EquatableArray<EndpointParameter>(children.ToImmutable()),
            CustomBindingMethod.None,
            meta.RequiresValidation));
    }

    private static ParameterClassificationResult ParameterSuccess(
        in ParameterMeta meta,
        EndpointParameterSource source,
        string? routeName = null,
        string? headerName = null,
        string? queryName = null,
        string? keyedServiceKey = null,
        string? formName = null,
        CustomBindingMethod customBinding = CustomBindingMethod.None)
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
            meta.RequiresValidation));
    }

    private readonly record struct ParameterClassificationResult(bool IsError, EndpointParameter Parameter)
    {
        public static readonly ParameterClassificationResult Error = new(true, default);
    }

    #region Type Detection Helpers

    private static RoutePrimitiveKind? TryGetRoutePrimitiveKind(ITypeSymbol type, ErrorOrContext context)
    {
        type = ErrorOrContext.UnwrapNullable(type);

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
        if (context.Guid is not null && SymbolEqualityComparer.Default.Equals(type, context.Guid))
            return RoutePrimitiveKind.Guid;
        if (context.DateTime is not null && SymbolEqualityComparer.Default.Equals(type, context.DateTime))
            return RoutePrimitiveKind.DateTime;
        if (context.DateTimeOffset is not null && SymbolEqualityComparer.Default.Equals(type, context.DateTimeOffset))
            return RoutePrimitiveKind.DateTimeOffset;
        if (context.DateOnly is not null && SymbolEqualityComparer.Default.Equals(type, context.DateOnly))
            return RoutePrimitiveKind.DateOnly;
        if (context.TimeOnly is not null && SymbolEqualityComparer.Default.Equals(type, context.TimeOnly))
            return RoutePrimitiveKind.TimeOnly;
        if (context.TimeSpan is not null && SymbolEqualityComparer.Default.Equals(type, context.TimeSpan))
            return RoutePrimitiveKind.TimeSpan;

        return null;
    }

    private static CustomBindingMethod DetectCustomBinding(ITypeSymbol type, ErrorOrContext context)
    {
        if (type is not INamedTypeSymbol namedType || IsPrimitiveOrWellKnownType(namedType, context))
            return CustomBindingMethod.None;

        if (ImplementsBindableInterface(namedType, context))
            return CustomBindingMethod.Bindable;

        var bindAsyncMethod = DetectBindAsyncMethod(namedType, context);
        return bindAsyncMethod != CustomBindingMethod.None ? bindAsyncMethod : DetectTryParseMethod(namedType, context);
    }

    private static bool IsPrimitiveOrWellKnownType(ITypeSymbol type, ErrorOrContext context)
    {
        type = ErrorOrContext.UnwrapNullable(type);

        // Check for primitive types (int, string, bool, etc.)
        if (type.SpecialType is not SpecialType.None)
            return true;

        // Check for well-known non-primitive types that have built-in conversions
        return TryGetRoutePrimitiveKindBySymbol(type, context) is not null;
    }

    private static bool ImplementsBindableInterface(ITypeSymbol type, ErrorOrContext context)
    {
        foreach (var iface in type.AllInterfaces)
        {
            if (iface is not { IsGenericType: true }) continue;

            var constructed = iface.ConstructedFrom;

            if (context.BindableFromHttpContext is not null)
            {
                if (SymbolEqualityComparer.Default.Equals(constructed, context.BindableFromHttpContext))
                    return true;
            }
            else if (constructed.MetadataName == "IBindableFromHttpContext`1" &&
                     constructed.ContainingNamespace.ToDisplayString() == "Microsoft.AspNetCore.Http")
            {
                return true;
            }
        }

        return false;
    }

    private static CustomBindingMethod DetectBindAsyncMethod(INamespaceOrTypeSymbol type, ErrorOrContext context)
    {
        foreach (var member in type.GetMembers("BindAsync"))
        {
            var result = ClassifyBindAsyncMember(member, context);
            if (result != CustomBindingMethod.None)
                return result;
        }

        return CustomBindingMethod.None;
    }

    private static CustomBindingMethod ClassifyBindAsyncMember(ISymbol member, ErrorOrContext context)
    {
        if (member is not IMethodSymbol { IsStatic: true, ReturnsVoid: false } method)
            return CustomBindingMethod.None;

        if (!IsTaskLike(method.ReturnType, context))
            return CustomBindingMethod.None;

        if (method.Parameters.Length < 1 || !context.IsHttpContext(method.Parameters[0].Type))
            return CustomBindingMethod.None;

        if (method.Parameters.Length >= 2 && context.IsParameterInfo(method.Parameters[1].Type))
            return CustomBindingMethod.BindAsyncWithParam;

        return CustomBindingMethod.BindAsync;
    }

    private static bool IsTaskLike(ITypeSymbol type, ErrorOrContext context)
    {
        if (type is not INamedTypeSymbol named) return false;
        var constructed = named.ConstructedFrom;

        return (context.TaskOfT is not null && SymbolEqualityComparer.Default.Equals(constructed, context.TaskOfT)) ||
               (context.ValueTaskOfT is not null &&
                SymbolEqualityComparer.Default.Equals(constructed, context.ValueTaskOfT));
    }

    private static CustomBindingMethod DetectTryParseMethod(INamespaceOrTypeSymbol type, ErrorOrContext context)
    {
        foreach (var member in type.GetMembers("TryParse"))
        {
            var result = ClassifyTryParseMember(member, context);
            if (result != CustomBindingMethod.None)
                return result;
        }

        return CustomBindingMethod.None;
    }

    private static CustomBindingMethod ClassifyTryParseMember(ISymbol member, ErrorOrContext context)
    {
        if (member is not IMethodSymbol { IsStatic: true, ReturnType.SpecialType: SpecialType.System_Boolean } method)
            return CustomBindingMethod.None;

        if (method.Parameters.Length < 2)
            return CustomBindingMethod.None;

        if (!IsStringOrCharSpan(method.Parameters[0].Type, context))
            return CustomBindingMethod.None;

        if (method.Parameters[^1].RefKind != RefKind.Out)
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
            return SymbolEqualityComparer.Default.Equals(named.ConstructedFrom, context.ReadOnlySpanOfT) &&
                   named.TypeArguments is [{ SpecialType: SpecialType.System_Char }];

        return false;
    }

    private static bool IsFormatProvider(ITypeSymbol type, ErrorOrContext context)
    {
        type = ErrorOrContext.UnwrapNullable(type);
        if (context.IFormatProvider is not null)
            return SymbolEqualityComparer.Default.Equals(type, context.IFormatProvider);

        return type.Name == "IFormatProvider" &&
               type.ContainingNamespace.ToDisplayString() == "System";
    }

    private static (bool IsCollection, ITypeSymbol? ItemType, RoutePrimitiveKind? Kind) AnalyzeCollectionType(
        ITypeSymbol type, ErrorOrContext context)
    {
        type = ErrorOrContext.UnwrapNullable(type);
        if (type.SpecialType == SpecialType.System_String)
            return (false, null, null);

        ITypeSymbol? itemType = null;
        if (type is IArrayTypeSymbol arrayType)
        {
            itemType = arrayType.ElementType;
        }
        else if (type is INamedTypeSymbol { IsGenericType: true } named)
        {
            var origin = named.ConstructedFrom;
            if (IsWellKnownCollection(origin, context))
                itemType = named.TypeArguments[0];
        }

        return itemType is not null
            ? (true, itemType, TryGetRoutePrimitiveKind(itemType, context))
            : (false, null, null);
    }

    private static bool IsWellKnownCollection(ISymbol origin, ErrorOrContext context)
    {
        return (context.ListOfT is not null && SymbolEqualityComparer.Default.Equals(origin, context.ListOfT)) ||
               (context.IListOfT is not null && SymbolEqualityComparer.Default.Equals(origin, context.IListOfT)) ||
               (context.IEnumerableOfT is not null &&
                SymbolEqualityComparer.Default.Equals(origin, context.IEnumerableOfT)) ||
               (context.IReadOnlyListOfT is not null &&
                SymbolEqualityComparer.Default.Equals(origin, context.IReadOnlyListOfT)) ||
               (context.ICollectionOfT is not null &&
                SymbolEqualityComparer.Default.Equals(origin, context.ICollectionOfT)) ||
               (context.HashSetOfT is not null && SymbolEqualityComparer.Default.Equals(origin, context.HashSetOfT));
    }

    private static (bool IsNullable, bool IsNonNullableValueType) GetParameterNullability(
        ITypeSymbol type,
        NullableAnnotation annotation)
    {
        if (type.IsReferenceType)
            return (annotation == NullableAnnotation.Annotated, false);

        return type is INamedTypeSymbol
        {
            IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Nullable_T
        }
            ? (true, false)
            : (false, true);
    }

    // ReSharper disable once SuggestBaseTypeForParameter - IParameterSymbol is semantically correct
    private static string? ExtractKeyFromKeyedServiceAttribute(IParameterSymbol parameter, ErrorOrContext context)
    {
        AttributeData? attr = null;
        foreach (var a in parameter.GetAttributes())
            if (SymbolEqualityComparer.Default.Equals(a.AttributeClass, context.FromKeyedServices))
            {
                attr = a;
                break;
            }

        if (attr is null || attr.ConstructorArguments.Length is 0)
            return null;

        var val = attr.ConstructorArguments[0].Value;
        return val switch { string s => $"\"{s}\"", _ => val?.ToString() };
    }

    // ReSharper disable once SuggestBaseTypeForParameter - IParameterSymbol is semantically correct
    private static bool HasParameterAttribute(IParameterSymbol parameter, INamedTypeSymbol? attributeSymbol,
        string attributeName)
    {
        var attributes = parameter.GetAttributes();

        if (attributeSymbol is not null && attributes.Any(attr =>
                SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeSymbol)))
            return true;

        var matcher = new AttributeNameMatcher(attributeName);
        return attributes.Any(attr => matcher.IsMatch(attr.AttributeClass));
    }

    // ReSharper disable once SuggestBaseTypeForParameter - IParameterSymbol is semantically correct
    private static string? TryGetAttributeName(ISymbol parameter, INamedTypeSymbol? attributeSymbol,
        string attributeName)
    {
        var attributes = parameter.GetAttributes();

        if (attributeSymbol is not null)
        {
            var attr = attributes.FirstOrDefault(a =>
                SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeSymbol));
            if (attr is not null) return ExtractNameFromAttribute(attr);
        }

        var matcher = new AttributeNameMatcher(attributeName);
        var matchingAttr = attributes.FirstOrDefault(attr => matcher.IsMatch(attr.AttributeClass));
        return matchingAttr is not null ? ExtractNameFromAttribute(matchingAttr) : null;
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
            _shortNameWithoutAttr = _shortName.EndsWith("Attribute") ? _shortName[..^"Attribute".Length] : _shortName;
        }

        public bool IsMatch(ISymbol? attributeClass)
        {
            var display = attributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (display is null) return false;

            if (display.StartsWith("global::"))
                display = display[8..];

            return display == _fullName ||
                   display.EndsWith($".{_shortName}") ||
                   display == _shortName ||
                   display == _shortNameWithoutAttr;
        }
    }

    private static string? ExtractNameFromAttribute(AttributeData? attr)
    {
        if (attr is null)
            return null;

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
                                                    Name\s*=\s*"([^"]+)"
                                                    """, RegexOptions.IgnoreCase);
            if (nameMatch.Success)
                return nameMatch.Groups[1].Value;
        }

        return null;
    }

    #endregion
}

internal readonly record struct ParameterBindingResult(bool IsValid, ImmutableArray<EndpointParameter> Parameters)
{
    public static readonly ParameterBindingResult Empty = new(true, ImmutableArray<EndpointParameter>.Empty);
    public static readonly ParameterBindingResult Invalid = new(false, ImmutableArray<EndpointParameter>.Empty);
}