using ANcpLua.Roslyn.Utilities.Models;
using ErrorOr.Analyzers;
using Microsoft.CodeAnalysis;

namespace ErrorOr.Generators;

/// <summary>
///     Partial class containing parameter binding entry point and classification dispatcher.
///     Per-source classifiers live in <c>ErrorOrEndpointGenerator.ParameterBinding.Classifiers.cs</c>.
///     Symbol-to-meta extraction lives in <c>ErrorOrEndpointGenerator.ParameterBinding.Meta.cs</c>.
///     Type-shape helpers live in <c>ErrorOrEndpointGenerator.ParameterBinding.TypeAnalysis.cs</c>.
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

        return BuildEndpointParameters(metas, method.Parameters, routeParameters, method, diagnostics, context,
            httpVerb);
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
            ? new ParameterBindingResult(true, builder.ToImmutable().AsEquatableArray())
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
            return ClassifyAsParameters(in meta, parameter.Type, routeParameters, method, diagnostics, context,
                httpVerb);

        if (meta.HasFromBody)
            return ParameterSuccess(in meta, ParameterSource.Body, emptyBodyBehavior: meta.EmptyBodyBehavior,
                validatableProperties: meta.ValidatableProperties);

        if (meta.HasFromForm) return ClassifyFromFormParameter(in meta, parameter.Type, context);

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
                return ParameterSuccess(in meta, ParameterSource.Body, emptyBodyBehavior: meta.EmptyBodyBehavior,
                    validatableProperties: meta.ValidatableProperties);

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

    private readonly record struct ParameterClassificationResult(bool IsError, EndpointParameter Parameter)
    {
        public static readonly ParameterClassificationResult Error = new(true, default);
    }
}
