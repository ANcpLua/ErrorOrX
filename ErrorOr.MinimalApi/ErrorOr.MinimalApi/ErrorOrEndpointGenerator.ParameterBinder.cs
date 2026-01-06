using System.Collections.Immutable;
using System.Text.RegularExpressions;
using ANcpLua.Roslyn.Utilities;
using ANcpLua.Roslyn.Utilities.Models;
using Microsoft.CodeAnalysis;

namespace ErrorOr.MinimalApi;

/// <summary>
///     Partial class containing parameter binding logic.
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
                DiagnosticDescriptors.MultipleBodySources, method, method.Name));
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
            metas[i] = CreateParameterMeta(i, parameters[i], context, diagnostics);
        return metas;
    }

    // ReSharper disable once UnusedParameter.Local - reserved for future diagnostics
    private static ParameterMeta CreateParameterMeta(
        int index,
        IParameterSymbol parameter,
        ErrorOrContext context,
        ImmutableArray<DiagnosticInfo>.Builder _)
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
        var (isCollection, itemType, itemPrimitiveKind) = AnalyzeCollectionType(type);

        var isFormFile = IsFormFileType(type, typeFqn, context);
        var isFormFileCollection = IsFormFileCollectionType(type, typeFqn, context);
        var isFormCollection = IsFormCollectionType(type, typeFqn, context);
        var isStream = IsStreamType(typeFqn);
        var isPipeReader = IsPipeReaderType(typeFqn);

        return new ParameterMeta(
            index, parameter, parameter.Name, typeFqn, TryGetRoutePrimitiveKind(type),
            HasParameterAttribute(parameter, context.FromServices, WellKnownTypes.FromServicesAttribute),
            hasFromKeyedServices, keyedServiceKey,
            HasParameterAttribute(parameter, context.FromBody, WellKnownTypes.FromBodyAttribute),
            hasFromRoute, hasFromQuery, hasFromHeader, hasAsParameters,
            routeName, queryName, headerName,
            typeFqn == WellKnownTypes.Fqn.CancellationToken,
            typeFqn == WellKnownTypes.Fqn.HttpContext,
            isNullable, isNonNullableValueType,
            isCollection, itemType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), itemPrimitiveKind,
            hasFromForm, formName, isFormFile, isFormFileCollection, isFormCollection,
            isStream, isPipeReader,
            DetectCustomBinding(type, context));
    }

    private static ParameterBindingResult BuildEndpointParameters(
        ParameterMeta[] metas,
        ImmutableHashSet<string> routeParameters,
        IMethodSymbol method,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        ErrorOrContext context)
    {
        var builder = ImmutableArray.CreateBuilder<EndpointParameter>(metas.Length);
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
        {
            return ParameterSuccess(in meta, EndpointParameterSource.KeyedService,
                keyedServiceKey: meta.KeyedServiceKey);
        }

        if (meta.HasFromHeader)
            return ParameterSuccess(in meta, EndpointParameterSource.Header, headerName: meta.HeaderName);
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
        if (meta.RouteKind is not null || (meta.IsCollection && meta.CollectionItemPrimitiveKind is not null))
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

    private static ParameterClassificationResult ClassifyFromRouteParameter(
        in ParameterMeta meta,
        ImmutableHashSet<string> routeParameters,
        IMethodSymbol method,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        var hasTryParse = meta.CustomBinding is CustomBindingMethod.TryParse or CustomBindingMethod.TryParseWithFormat;

        if (meta.RouteKind is null && !hasTryParse)
        {
            return EmitParameterError(in meta, method, diagnostics,
                "[FromRoute] requires a supported primitive type or a type with TryParse");
        }

        // Note: We don't validate route parameter existence here anymore
        // That's done by RouteValidator.ValidateParameterBindings in the main generator
        return ParameterSuccess(in meta, EndpointParameterSource.Route, meta.RouteName,
            customBinding: meta.CustomBinding);
    }

    private static ParameterClassificationResult ClassifyImplicitRouteParameter(
        in ParameterMeta meta,
        IMethodSymbol method,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        var hasTryParse = meta.CustomBinding is CustomBindingMethod.TryParse or CustomBindingMethod.TryParseWithFormat;

        if (meta.RouteKind is null && !hasTryParse)
        {
            return EmitParameterError(in meta, method, diagnostics,
                "Route parameters must use supported primitive types or a type with TryParse");
        }

        return ParameterSuccess(in meta, EndpointParameterSource.Route, meta.Name, customBinding: meta.CustomBinding);
    }

    private static ParameterClassificationResult ClassifyFromQueryParameter(
        in ParameterMeta meta,
        IMethodSymbol method,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        if (meta.RouteKind is not null || (meta.IsCollection && meta.CollectionItemPrimitiveKind is not null))
            return ParameterSuccess(in meta, EndpointParameterSource.Query, queryName: meta.QueryName);

        return EmitParameterError(in meta, method, diagnostics,
            "[FromQuery] only supports primitives or collections of primitives");
    }

    // ReSharper disable UnusedParameter.Local - matches common classifier delegate signature
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
        if (meta.RouteKind is not null)
            return ParameterSuccess(in meta, EndpointParameterSource.Form, formName: meta.FormName);
        if (meta.IsCollection && meta.CollectionItemPrimitiveKind is not null)
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
        {
            // Non-named types get simple form binding - BCL will handle/fail at runtime
            return ParameterSuccess(in meta, EndpointParameterSource.Form, formName: meta.FormName);
        }

        var constructor = typeSymbol.Constructors
            .Where(static c => c.DeclaredAccessibility == Accessibility.Public && !c.IsStatic)
            .OrderByDescending(static c => c.Parameters.Length)
            .FirstOrDefault();

        if (constructor is null || constructor.Parameters.Length == 0)
        {
            // No suitable constructor - simple form binding
            return ParameterSuccess(in meta, EndpointParameterSource.Form, formName: meta.FormName);
        }

        // Build child parameters for DTO constructor
        var children = ImmutableArray.CreateBuilder<EndpointParameter>(constructor.Parameters.Length);
        var dummyDiagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

        foreach (var paramSymbol in constructor.Parameters)
        {
            var childMeta = CreateParameterMeta(children.Count, paramSymbol, context, dummyDiagnostics);

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
            new EquatableArray<EndpointParameter>(children.ToImmutable())));
    }

    private static ParameterClassificationResult ClassifyAsParameters(
        in ParameterMeta meta,
        ImmutableHashSet<string> routeParameters,
        IMethodSymbol method,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        ErrorOrContext context)
    {
        if (meta.Symbol.Type is not INamedTypeSymbol typeSymbol)
        {
            return EmitParameterError(in meta, method, diagnostics,
                "[AsParameters] can only be used on class or struct types.");
        }

        var constructor = typeSymbol.Constructors.OrderByDescending(static c => c.Parameters.Length).FirstOrDefault();

        if (constructor is null)
            return EmitParameterError(in meta, method, diagnostics, "[AsParameters] type must have a constructor.");

        var children = ImmutableArray.CreateBuilder<EndpointParameter>();
        for (var i = 0; i < constructor.Parameters.Length; i++)
        {
            var paramSymbol = constructor.Parameters[i];
            var childMeta = CreateParameterMeta(i, paramSymbol, context, diagnostics);
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
            new EquatableArray<EndpointParameter>(children.ToImmutable())));
    }

    /// <summary>
    ///     Fallback for parameters that can't be classified - let BCL handle via service injection.
    /// </summary>
    // ReSharper disable UnusedParameter.Local - matches common classifier delegate signature
    private static ParameterClassificationResult EmitParameterError(
            in ParameterMeta meta,
            IMethodSymbol _,
            ImmutableArray<DiagnosticInfo>.Builder __,
            string ___)
        // ReSharper restore UnusedParameter.Local
    {
        // BCL will resolve via DI or fail at runtime with a clear error
        return ParameterSuccess(in meta, EndpointParameterSource.Service);
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
            customBinding));
    }

    private readonly record struct ParameterClassificationResult(bool IsError, EndpointParameter Parameter)
    {
        public static readonly ParameterClassificationResult Error = new(true, default);
    }

    #region Type Detection Helpers

    private static RoutePrimitiveKind? TryGetRoutePrimitiveKind(ITypeSymbol type)
    {
        return type.SpecialType switch
        {
            SpecialType.System_String => RoutePrimitiveKind.String,
            SpecialType.System_Int32 => RoutePrimitiveKind.Int32,
            SpecialType.System_Int64 => RoutePrimitiveKind.Int64,
            SpecialType.System_Int16 => RoutePrimitiveKind.Int16,
            SpecialType.System_UInt32 => RoutePrimitiveKind.UInt32,
            SpecialType.System_UInt64 => RoutePrimitiveKind.UInt64,
            SpecialType.System_UInt16 => RoutePrimitiveKind.UInt16,
            SpecialType.System_Byte => RoutePrimitiveKind.Byte,
            SpecialType.System_SByte => RoutePrimitiveKind.SByte,
            SpecialType.System_Boolean => RoutePrimitiveKind.Boolean,
            SpecialType.System_Decimal => RoutePrimitiveKind.Decimal,
            SpecialType.System_Double => RoutePrimitiveKind.Double,
            SpecialType.System_Single => RoutePrimitiveKind.Single,
            _ => TryGetRoutePrimitiveKindByFqn(type)
        };
    }

    private static RoutePrimitiveKind? TryGetRoutePrimitiveKindByFqn(ITypeSymbol type)
    {
        var fqn = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return fqn switch
        {
            WellKnownTypes.Fqn.Guid => RoutePrimitiveKind.Guid,
            WellKnownTypes.Fqn.DateTime => RoutePrimitiveKind.DateTime,
            WellKnownTypes.Fqn.DateTimeOffset => RoutePrimitiveKind.DateTimeOffset,
            WellKnownTypes.Fqn.DateOnly => RoutePrimitiveKind.DateOnly,
            WellKnownTypes.Fqn.TimeOnly => RoutePrimitiveKind.TimeOnly,
            WellKnownTypes.Fqn.TimeSpan => RoutePrimitiveKind.TimeSpan,
            _ => null
        };
    }

    private static CustomBindingMethod DetectCustomBinding(ITypeSymbol type, ErrorOrContext context)
    {
        if (type is not INamedTypeSymbol namedType)
            return CustomBindingMethod.None;

        if (IsPrimitiveOrWellKnownType(namedType))
            return CustomBindingMethod.None;

        if (ImplementsBindableInterface(namedType, context))
            return CustomBindingMethod.Bindable;

        var bindAsyncMethod = DetectBindAsyncMethod(namedType, context);
        if (bindAsyncMethod != CustomBindingMethod.None)
            return bindAsyncMethod;

        return DetectTryParseMethod(namedType);
    }

    private static bool IsPrimitiveOrWellKnownType(INamedTypeSymbol type)
    {
        if (type.SpecialType is not SpecialType.None)
            return true;

        var fqn = type.ToDisplayString();
        return fqn is "System.Guid" or "global::System.Guid" or
            "System.DateTime" or "global::System.DateTime" or
            "System.DateTimeOffset" or "global::System.DateTimeOffset" or
            "System.DateOnly" or "global::System.DateOnly" or
            "System.TimeOnly" or "global::System.TimeOnly" or
            "System.TimeSpan" or "global::System.TimeSpan";
    }

    private static bool ImplementsBindableInterface(INamedTypeSymbol type, ErrorOrContext context)
    {
        if (context.IBindableFromHttpContext is not null)
        {
            foreach (var iface in type.AllInterfaces)
            {
                if (iface.IsGenericType &&
                    SymbolEqualityComparer.Default.Equals(iface.ConstructedFrom, context.IBindableFromHttpContext))
                    return true;
            }
        }

        foreach (var iface in type.AllInterfaces)
        {
            if (iface.IsGenericType)
            {
                var fqn = iface.ConstructedFrom.ToDisplayString();
                if (fqn == "Microsoft.AspNetCore.Http.IBindableFromHttpContext<TSelf>")
                    return true;
            }
        }

        return false;
    }

    private static CustomBindingMethod DetectBindAsyncMethod(INamedTypeSymbol type, ErrorOrContext context)
    {
        foreach (var member in type.GetMembers("BindAsync"))
        {
            if (member is not IMethodSymbol { IsStatic: true, ReturnsVoid: false } method)
                continue;

            var returnFqn = method.ReturnType.ToDisplayString();
            if (!returnFqn.StartsWith("System.Threading.Tasks.ValueTask<") &&
                !returnFqn.StartsWith("System.Threading.Tasks.Task<"))
                continue;

            if (method.Parameters.Length < 1)
                continue;

            var firstParam = method.Parameters[0].Type;
            var firstParamFqn = firstParam.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (firstParamFqn != WellKnownTypes.Fqn.HttpContext &&
                !IsHttpContextType(firstParam, context))
                continue;

            if (method.Parameters.Length >= 2)
            {
                var secondParam = method.Parameters[1].Type;
                var secondParamFqn = secondParam.ToDisplayString();
                if (secondParamFqn.EndsWith("ParameterInfo") ||
                    (context.ParameterInfo is not null &&
                     SymbolEqualityComparer.Default.Equals(secondParam, context.ParameterInfo)))
                    return CustomBindingMethod.BindAsyncWithParam;
            }

            return CustomBindingMethod.BindAsync;
        }

        return CustomBindingMethod.None;
    }

    private static bool IsHttpContextType(ITypeSymbol type, ErrorOrContext context)
    {
        if (context.HttpContextSymbol is not null &&
            SymbolEqualityComparer.Default.Equals(type, context.HttpContextSymbol))
            return true;

        var fqn = type.ToDisplayString();
        return fqn is "Microsoft.AspNetCore.Http.HttpContext" or "HttpContext";
    }

    private static CustomBindingMethod DetectTryParseMethod(INamedTypeSymbol type)
    {
        foreach (var member in type.GetMembers("TryParse"))
        {
            if (member is not IMethodSymbol { IsStatic: true } method)
                continue;

            if (method.ReturnType.SpecialType != SpecialType.System_Boolean)
                continue;

            if (method.Parameters.Length < 2)
                continue;

            if (method.Parameters[0].Type.SpecialType != SpecialType.System_String)
                continue;

            var lastParam = method.Parameters[^1];
            if (lastParam.RefKind != RefKind.Out)
                continue;

            if (method.Parameters.Length == 3)
            {
                var middleParam = method.Parameters[1].Type;
                var middleFqn = middleParam.ToDisplayString();
                if (middleFqn.Contains("IFormatProvider"))
                    return CustomBindingMethod.TryParseWithFormat;
            }

            if (method.Parameters.Length == 2)
                return CustomBindingMethod.TryParse;
        }

        return CustomBindingMethod.None;
    }

    private static bool IsFormFileType(ITypeSymbol type, string typeFqn, ErrorOrContext context)
    {
        if (context.IFormFile is not null &&
            SymbolEqualityComparer.Default.Equals(type, context.IFormFile))
            return true;

        return typeFqn is WellKnownTypes.Fqn.IFormFile or WellKnownTypes.IFormFile or "IFormFile";
    }

    private static bool IsFormFileCollectionType(ITypeSymbol type, string typeFqn, ErrorOrContext context)
    {
        if (context.IFormFileCollection is not null &&
            SymbolEqualityComparer.Default.Equals(type, context.IFormFileCollection))
            return true;

        if (typeFqn is WellKnownTypes.Fqn.IFormFileCollection or WellKnownTypes.IFormFileCollection
            or "IFormFileCollection")
            return true;

        return IsFormFileReadOnlyList(type, context);
    }

    private static bool IsFormFileReadOnlyList(ITypeSymbol type, ErrorOrContext context)
    {
        if (type is not INamedTypeSymbol { IsGenericType: true } named)
            return false;

        var origin = named.ConstructedFrom.ToDisplayString();
        if (origin != "System.Collections.Generic.IReadOnlyList<T>")
            return false;

        var itemType = named.TypeArguments[0];
        var itemFqn = itemType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return IsFormFileType(itemType, itemFqn, context);
    }

    private static bool IsFormCollectionType(ISymbol type, string typeFqn, ErrorOrContext context)
    {
        if (context.IFormCollection is not null &&
            SymbolEqualityComparer.Default.Equals(type, context.IFormCollection))
            return true;

        return typeFqn is WellKnownTypes.Fqn.IFormCollection or "Microsoft.AspNetCore.Http.IFormCollection"
            or "IFormCollection";
    }

    private static bool IsStreamType(string typeFqn)
    {
        return typeFqn is WellKnownTypes.Fqn.Stream or WellKnownTypes.Stream or "Stream" or "System.IO.Stream";
    }

    private static bool IsPipeReaderType(string typeFqn)
    {
        return typeFqn is WellKnownTypes.Fqn.PipeReader or WellKnownTypes.PipeReader or "PipeReader"
            or "System.IO.Pipelines.PipeReader";
    }

    private static (bool IsCollection, ITypeSymbol? ItemType, RoutePrimitiveKind? Kind) AnalyzeCollectionType(
        ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String)
            return (false, null, null);

        ITypeSymbol? itemType = null;
        if (type is IArrayTypeSymbol arrayType)
            itemType = arrayType.ElementType;
        else if (type is INamedTypeSymbol { IsGenericType: true } named)
        {
            var origin = named.ConstructedFrom.ToDisplayString();
            if (origin.StartsWith("System.Collections.Generic.List") ||
                origin.StartsWith("System.Collections.Generic.IList") ||
                origin.StartsWith("System.Collections.Generic.IEnumerable") ||
                origin.StartsWith("System.Collections.Generic.IReadOnlyList"))
                itemType = named.TypeArguments[0];
        }

        if (itemType is not null)
            return (true, itemType, TryGetRoutePrimitiveKind(itemType));

        return (false, null, null);
    }

    private static (bool IsNullable, bool IsNonNullableValueType) GetParameterNullability(
        ITypeSymbol type,
        NullableAnnotation annotation)
    {
        if (type.IsReferenceType)
            return (annotation == NullableAnnotation.Annotated, false);

        if (type is INamedTypeSymbol { IsGenericType: true } named &&
            named.ConstructedFrom.ToDisplayString() == "System.Nullable<T>")
            return (true, false);

        return (false, true);
    }

    // ReSharper disable once SuggestBaseTypeForParameter - IParameterSymbol is semantically correct
    private static string? ExtractKeyFromKeyedServiceAttribute(IParameterSymbol parameter, ErrorOrContext context)
    {
        AttributeData? attr = null;
        foreach (var a in parameter.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(a.AttributeClass, context.FromKeyedServices))
            {
                attr = a;
                break;
            }
        }

        if (attr is null || attr.ConstructorArguments.Length == 0)
            return null;

        var val = attr.ConstructorArguments[0].Value;
        return val switch { string s => $"\"{s}\"", _ => val?.ToString() };
    }

    // ReSharper disable once SuggestBaseTypeForParameter - IParameterSymbol is semantically correct
    private static bool HasParameterAttribute(IParameterSymbol parameter, INamedTypeSymbol? attributeSymbol,
        string attributeName)
    {
        var attributes = parameter.GetAttributes();

        if (attributeSymbol is not null)
        {
            foreach (var attr in attributes)
            {
                if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeSymbol))
                    return true;
            }
        }

        var shortName = attributeName[(attributeName.LastIndexOf('.') + 1)..];
        var shortNameWithoutAttr = shortName.EndsWith("Attribute") ? shortName[..^"Attribute".Length] : shortName;

        foreach (var attr in attributes)
        {
            var display = attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (display is null)
                continue;

            if (display.StartsWith("global::"))
                display = display[8..];

            if (display == attributeName ||
                display.EndsWith($".{shortName}") ||
                display == shortName ||
                display == shortNameWithoutAttr)
                return true;
        }

        return false;
    }

    // ReSharper disable once SuggestBaseTypeForParameter - IParameterSymbol is semantically correct
    private static string? TryGetAttributeName(IParameterSymbol parameter, INamedTypeSymbol? attributeSymbol,
        string attributeName)
    {
        var attributes = parameter.GetAttributes();

        if (attributeSymbol is not null)
        {
            foreach (var attr in attributes)
            {
                if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeSymbol))
                    return ExtractNameFromAttribute(attr);
            }
        }

        var shortName = attributeName[(attributeName.LastIndexOf('.') + 1)..];
        var shortNameWithoutAttr = shortName.EndsWith("Attribute") ? shortName[..^"Attribute".Length] : shortName;

        foreach (var attr in attributes)
        {
            var display = attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (display is null)
                continue;

            if (display.StartsWith("global::"))
                display = display[8..];

            if (display == attributeName ||
                display.EndsWith($".{shortName}") ||
                display == shortName ||
                display == shortNameWithoutAttr)
                return ExtractNameFromAttribute(attr);
        }

        return null;
    }

    private static string? ExtractNameFromAttribute(AttributeData? attr)
    {
        if (attr is null)
            return null;

        foreach (var namedArg in attr.NamedArguments)
        {
            if (string.Equals(namedArg.Key, "Name", StringComparison.OrdinalIgnoreCase) &&
                namedArg.Value.Value is string name && !string.IsNullOrWhiteSpace(name))
                return name;
        }

        if (attr.ConstructorArguments.Length > 0 &&
            attr.ConstructorArguments[0].Value is string ctorArg &&
            !string.IsNullOrWhiteSpace(ctorArg))
            return ctorArg;

        if (attr.ApplicationSyntaxReference?.GetSyntax() is { } syntax)
        {
            var syntaxText = syntax.ToString();
            var nameMatch = Regex.Match(syntaxText, @"Name\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase);
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