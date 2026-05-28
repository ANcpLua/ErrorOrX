using ANcpLua.Roslyn.Utilities.Models;
using ErrorOr.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ErrorOr.Generators;

/// <summary>
///     Per-attribute endpoint discovery and descriptor construction. The pipeline:
///     <list type="number">
///         <item><see cref="CombineHttpMethodProviders" /> fans out one <c>SyntaxProvider</c> per HTTP-method attribute.</item>
///         <item><see cref="AnalyzeEndpointFlow" /> shape-validates the method via <c>DiagnosticFlow</c> railway.</item>
///         <item>
///             <see cref="ProcessAttributeFlow" /> binds parameters, validates routes/versions, and builds the
///             descriptor.
///         </item>
///     </list>
/// </summary>
public sealed partial class ErrorOrEndpointGenerator
{
    private static IncrementalValueProvider<EquatableArray<EndpointDescriptor>> CombineHttpMethodProviders(
        IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<ErrorOrContext> errorOrContextProvider)
    {
        var getProvider = CreateEndpointProvider(context, WellKnownTypes.GetAttribute, errorOrContextProvider);
        var postProvider = CreateEndpointProvider(context, WellKnownTypes.PostAttribute, errorOrContextProvider);
        var putProvider = CreateEndpointProvider(context, WellKnownTypes.PutAttribute, errorOrContextProvider);
        var deleteProvider = CreateEndpointProvider(context, WellKnownTypes.DeleteAttribute, errorOrContextProvider);
        var patchProvider = CreateEndpointProvider(context, WellKnownTypes.PatchAttribute, errorOrContextProvider);
        var headProvider = CreateEndpointProvider(context, WellKnownTypes.HeadAttribute, errorOrContextProvider);
        var optionsProvider =
            CreateEndpointProvider(context, WellKnownTypes.OptionsAttribute, errorOrContextProvider);
        var traceProvider = CreateEndpointProvider(context, WellKnownTypes.TraceAttribute, errorOrContextProvider);
        var baseProvider =
            CreateEndpointProvider(context, WellKnownTypes.ErrorOrEndpointAttribute, errorOrContextProvider);

        return IncrementalProviderExtensions.CombineAll(
            getProvider, postProvider, putProvider,
            deleteProvider, patchProvider,
            headProvider, optionsProvider, traceProvider,
            baseProvider);
    }

    private static IncrementalValuesProvider<EndpointDescriptor> CreateEndpointProvider(
        IncrementalGeneratorInitializationContext context,
        string attributeName,
        IncrementalValueProvider<ErrorOrContext> errorOrContextProvider)
    {
        return context.SyntaxProvider
            .ForAttributeWithMetadataName(
                attributeName,
                static (node, _) => node is MethodDeclarationSyntax,
                static (ctx, _) => ctx)
            .Combine(errorOrContextProvider)
            .SelectFlow(static (pair, ct) =>
            {
                var (ctx, errorOrContext) = pair;
                return AnalyzeEndpointFlow(ctx, errorOrContext, ct);
            })
            .WithTrackingName("EndpointBindingFlow." + attributeName)
            .ReportAndContinue(context)
            .SelectMany(static (endpoints, _) => endpoints.AsImmutableArray());
    }

    private static DiagnosticFlow<EquatableArray<EndpointDescriptor>> AnalyzeEndpointFlow(
        GeneratorAttributeSyntaxContext ctx,
        ErrorOrContext errorOrContext,
        CancellationToken ct)
    {
        if (ctx.TargetSymbol is not IMethodSymbol method || ctx.Attributes.IsDefaultOrEmpty)
            return Helpers.EmptyEndpointFlow();

        var location = method.Locations.FirstOrDefault() ?? Location.None;

        // 1. Validate shape using SemanticGuard + DiagnosticFlow (The Railway Pattern)
        var methodAnalysisFlow = SemanticGuard.For(method)
            .MustBeStatic(DiagnosticInfo.Create(Descriptors.NonStaticHandler, location, method.Name))
            .ToFlow()
            .Then(m =>
            {
                var returnInfo = ExtractErrorOrReturnType(m.ReturnType);

                // EOE015 is now Warning severity (non-failing) — handled in the next .Then via the
                // builder pattern below, alongside EOE033. Fail-fast here would short-circuit the
                // railway on a warning, which would skip generation that the user can still ship.

                // EOE018: Inaccessible return type
                if (returnInfo.IsInaccessibleType)
                {
                    return DiagnosticFlow.Fail<(IMethodSymbol, ErrorOrReturnTypeInfo)>(
                        DiagnosticInfo.Create(Descriptors.InaccessibleTypeNotSupported, location,
                            returnInfo.InaccessibleTypeName ?? "unknown",
                            m.Name,
                            returnInfo.InaccessibleTypeAccessibility ?? "private"));
                }

                // EOE019: Type parameter in return type
                if (returnInfo.IsTypeParameter)
                {
                    return DiagnosticFlow.Fail<(IMethodSymbol, ErrorOrReturnTypeInfo)>(
                        DiagnosticInfo.Create(Descriptors.TypeParameterNotSupported, location,
                            m.Name,
                            returnInfo.TypeParameterName ?? "T"));
                }

                return returnInfo.SuccessTypeFqn is not null
                    ? DiagnosticFlow.Ok((m, returnInfo))
                    : DiagnosticFlow.Fail<(IMethodSymbol, ErrorOrReturnTypeInfo)>(
                        DiagnosticInfo.Create(Descriptors.InvalidReturnType, location, m.Name));
            })
            .Then(pair =>
            {
                var (m, returnInfo) = pair;
                var builder = ImmutableArray.CreateBuilder<DiagnosticInfo>();

                // EOE033: Validate PascalCase naming convention
                if (NamingValidator.ValidatePascalCase(m.Name, location) is { } namingDiagnostic)
                    builder.Add(namingDiagnostic);

                // EOE015: ErrorOr<object> / ErrorOr<dynamic> — warn but continue. The generator
                // still emits valid code (JsonSerializable(typeof(object))); the warning surfaces
                // the AOT-safety risk so the user can switch to a concrete payload type or
                // explicitly register object in their JsonSerializerContext.
                if (returnInfo.IsObjectReturn)
                    builder.Add(DiagnosticInfo.Create(Descriptors.ObjectReturnTypeNotSupported, location, m.Name));

                // EOE034: DataAnnotations validation uses reflection (Info severity).
                // Dual-reports with the analyzer so the diagnostic is visible in build output and
                // snapshot tests, not just the IDE. ErrorOrContext.HasValidationNeeds is the shared
                // predicate — covers both [Required] on the parameter and [Required] on a property
                // of the parameter's type (records, regular DTOs, IValidatableObject).
                foreach (var param in m.Parameters)
                {
                    if (!ErrorOrContext.HasValidationNeeds(param)) continue;

                    builder.Add(DiagnosticInfo.Create(
                        Descriptors.ValidationUsesReflection,
                        param.Locations.FirstOrDefault() ?? location,
                        param.Name,
                        m.Name));
                }

                // Extract method-level attributes first (needed for interface call detection)
                var producesErrors = ExtractProducesErrorAttributes(m);
                var isAcceptedResponse = HasAcceptedResponseAttribute(m);
                var hasExplicitProducesError = !producesErrors.IsDefaultOrEmpty;

                // Extract middleware attributes (BCL: Authorize, RateLimiting, OutputCache, CORS)
                var middleware = ExtractMiddlewareAttributes(m);

                // Infer errors once per method (now with interface call detection)
                var (inferredErrors, customErrors) =
                    InferErrorTypesFromMethod(ctx, m, errorOrContext, builder, hasExplicitProducesError);

                var analysis = new MethodAnalysis(
                    returnInfo,
                    inferredErrors,
                    customErrors,
                    producesErrors,
                    isAcceptedResponse,
                    middleware);

                var flow = DiagnosticFlow.Ok(analysis);
                foreach (var diag in builder)
                    flow = flow.Warn(diag);

                return flow;
            });

        // 2. Map method analysis to individual attribute descriptors
        var flows = ImmutableArray.CreateBuilder<DiagnosticFlow<EndpointDescriptor>>(ctx.Attributes.Length);
        foreach (var attr in ctx.Attributes)
        {
            if (attr is null) continue;

            var flow = methodAnalysisFlow.Then(analysis =>
                ProcessAttributeFlow(method, in analysis, attr, errorOrContext, ct));
            flows.Add(flow);
        }

        if (flows.Count is 0) return Helpers.EmptyEndpointFlow();

        return DiagnosticFlow.Collect(flows.ToImmutable())
            .Select(static endpoints => new EquatableArray<EndpointDescriptor>(endpoints));
    }

    private static DiagnosticFlow<EndpointDescriptor> ProcessAttributeFlow(
        IMethodSymbol method,
        in MethodAnalysis analysis,
        AttributeData attr,
        ErrorOrContext errorOrContext,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (attr.AttributeClass is not { } attrClass) return DiagnosticFlow.Fail<EndpointDescriptor>();

        var attrName = attrClass.Name;

        var (verb, pattern, customMethod) = ExtractHttpMethodAndPattern(attr, attrName);
        if (verb is null) return DiagnosticFlow.Fail<EndpointDescriptor>();

        // Guard: SuccessTypeFqn validated in upstream .Then() but compiler doesn't know
        if (analysis.ReturnInfo.SuccessTypeFqn is not { } successTypeFqn)
            return DiagnosticFlow.Fail<EndpointDescriptor>();

        var builder = ImmutableArray.CreateBuilder<DiagnosticInfo>();

        // Extract route parameters as HashSet for binding
        var routeParamInfos = RouteValidator.ExtractRouteParameters(pattern);
        var routeParamNames = routeParamInfos
            .Select(static r => r.Name)
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        var bindingFlow = RouteBindingHelper.BindRouteParameters(
            method,
            routeParamNames,
            errorOrContext,
            verb.Value);
        if (!bindingFlow.IsSuccess) return DiagnosticFlow.Fail<EndpointDescriptor>(bindingFlow.Diagnostics);

        builder.AddRange(bindingFlow.Diagnostics.AsImmutableArray());
        var bindingAnalysis = bindingFlow.ValueOrDefault();

        // Validate route pattern
        builder.AddRange(RouteValidator.ValidatePattern(pattern, method));

        // Extract method parameter info for route binding validation
        var methodParams = bindingAnalysis.RouteParameters.AsImmutableArray();

        // Validate route parameters are bound
        builder.AddRange(RouteValidator.ValidateParameterBindings(
            pattern, routeParamInfos, methodParams, method));

        // Validate route constraint types
        builder.AddRange(RouteValidator.ValidateConstraintTypes(
            routeParamInfos, methodParams, method));

        // Extract API versioning attributes
        var versioning = ExtractVersioningAttributes(method, errorOrContext);

        // Validate API versioning configuration (EOE027-EOE031)
        var rawClassVersions = ExtractRawClassVersionStrings(method, errorOrContext);
        var rawMethodVersions = ExtractRawMethodVersionStrings(method, errorOrContext);
        var location = method.Locations.FirstOrDefault() ?? Location.None;
        builder.AddRange(ApiVersioningValidator.Validate(
            method.Name,
            in versioning,
            rawClassVersions,
            rawMethodVersions,
            location,
            errorOrContext.HasApiVersioningSupport,
            method));

        // Extract route group configuration for eShop-style grouping
        var routeGroup = ExtractRouteGroupInfo(method);

        // Extract custom endpoint metadata
        var metadata = ExtractMetadata(method);

        var descriptor = new EndpointDescriptor(
            verb.Value,
            pattern,
            successTypeFqn,
            analysis.ReturnInfo.Kind,
            analysis.ReturnInfo.IsAsync,
            method.ContainingType?.GetFullyQualifiedName() ?? "Unknown",
            method.Name,
            bindingAnalysis.Parameters,
            new ErrorInferenceInfo(
                analysis.InferredErrorTypeNames,
                analysis.InferredCustomErrors,
                analysis.ProducesErrors),
            new SseInfo(
                analysis.ReturnInfo.IsSse,
                analysis.ReturnInfo.SseItemTypeFqn),
            analysis.IsAcceptedResponse,
            analysis.ReturnInfo.IdPropertyName,
            analysis.Middleware,
            versioning,
            routeGroup,
            metadata,
            customMethod);

        var flow = DiagnosticFlow.Ok(descriptor);
        foreach (var diag in builder)
            flow = flow.Warn(diag);

        return flow;
    }

    private static (HttpVerb? Verb, string Pattern, string? CustomMethod) ExtractHttpMethodAndPattern(
        AttributeData attr,
        string attrName)
    {
        var verb = HttpVerbExtensions.TryParseFromAttribute(attrName, attr.ConstructorArguments);

        // For ErrorOrEndpointAttribute with unrecognized methods (e.g., "CONNECT", "PROPFIND"),
        // store the raw method string so we can emit MapMethods with it
        string? customMethod = null;
        var isErrorOrEndpoint = attrName.Contains("ErrorOrEndpoint");
        if (verb is null && isErrorOrEndpoint &&
            attr.ConstructorArguments is [{ Value: string rawMethod }, ..])
        {
            customMethod = rawMethod.ToUpperInvariant();
            verb = HttpVerb.Get; // placeholder — MapMethods is used when CustomHttpMethod is set
        }

        if (verb is null) return (null, "/", null);

        // Extract pattern - index differs for ErrorOrEndpoint (has httpMethod arg first)
        var patternIndex = isErrorOrEndpoint ? 1 : 0;
        var pattern = attr.GetConstructorArgument<string>(patternIndex) is { } p
            ? p
            : "/";

        return (verb, pattern, customMethod);
    }

    /// <summary>
    ///     Incremental pipeline tracking names for caching diagnostics. Cache step labels — they
    ///     just need to be stable per provider and unique across providers. The per-attribute
    ///     <c>EndpointBindingFlow</c> label is built inline as <c>"EndpointBindingFlow." + attributeName</c>
    ///     directly at the <c>WithTrackingName</c> call site, so no helper or mapping table is needed:
    ///     the attribute FQN itself satisfies stability + uniqueness, and the literal prefix preserves
    ///     the contract that <c>GeneratorCachingTests.IsCached("EndpointBindingFlow")</c> depends on.
    /// </summary>
    private static class TrackingNames
    {
        public const string ResultsUnionMaxArity = "ResultsUnionMaxArity";
        public const string ErrorOrContext = "ErrorOrContext";
    }
}
