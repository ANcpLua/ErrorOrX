using System.Collections.Immutable;
using ANcpLua.Roslyn.Utilities;
using ANcpLua.Roslyn.Utilities.Models;
using ErrorOr.Endpoints.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ErrorOr.Endpoints.Generators;

/// <summary>
///     Generator entry point for ErrorOr endpoint mappings.
///     Generates MapErrorOrEndpoints() and AddErrorOrEndpointJson() extension methods.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed partial class ErrorOrEndpointGenerator : IIncrementalGenerator
{
    // EPS06: Defensive copies are unavoidable with Roslyn's incremental generator API.
#pragma warning disable EPS06

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Build ErrorOrContext once per compilation
        var errorOrContextProvider = context.CompilationProvider
            .Select(static (c, _) => new ErrorOrContext(c));

        // Create providers for each HTTP method attribute
        var getProvider = CreateEndpointProvider(context, errorOrContextProvider, WellKnownTypes.GetAttribute);
        var postProvider = CreateEndpointProvider(context, errorOrContextProvider, WellKnownTypes.PostAttribute);
        var putProvider = CreateEndpointProvider(context, errorOrContextProvider, WellKnownTypes.PutAttribute);
        var deleteProvider = CreateEndpointProvider(context, errorOrContextProvider, WellKnownTypes.DeleteAttribute);
        var patchProvider = CreateEndpointProvider(context, errorOrContextProvider, WellKnownTypes.PatchAttribute);
        var baseProvider =
            CreateEndpointProvider(context, errorOrContextProvider, WellKnownTypes.ErrorOrEndpointAttribute);

        // Combine all endpoint providers
        var endpoints = CombineEndpointProviders(
            getProvider, postProvider, putProvider,
            deleteProvider, patchProvider, baseProvider);

        // Get JSON context info for AOT validation
        var jsonContexts = JsonContextProvider.Create(context).CollectAsEquatableArray();

        // Get max arity from compilation
        var compilationProvider = context.CompilationProvider
            .Select(static (c, _) => ResultsUnionTypeBuilder.DetectMaxArity(c));

        // Combine and emit
        var combined = endpoints.Combine(jsonContexts).Combine(compilationProvider);
        context.RegisterSourceOutput(
            combined,
            static (spc, data) =>
            {
                var ((endpoints, jsonContexts), maxArity) = data;
                var endpointArray = endpoints.IsDefaultOrEmpty
                    ? ImmutableArray<EndpointDescriptor>.Empty
                    : endpoints.AsImmutableArray();
                var jsonContextArray = jsonContexts.IsDefaultOrEmpty
                    ? ImmutableArray<JsonContextInfo>.Empty
                    : jsonContexts.AsImmutableArray();

                // Run duplicate route detection
                var duplicateDiagnostics = DuplicateRouteDetector.Detect(endpointArray);
                foreach (var diag in duplicateDiagnostics)
                    spc.ReportDiagnostic(diag);

                // Emit endpoint mappings
                if (!endpointArray.IsDefaultOrEmpty)
                {
                    EmitEndpoints(spc, endpointArray, maxArity);
                    AnalyzeJsonContextCoverage(spc, endpointArray, jsonContextArray);
                    AnalyzeUnionTypeArity(spc, endpointArray, maxArity);
                }
            });
    }

    private static IncrementalValuesProvider<EndpointDescriptor> CreateEndpointProvider(
        IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<ErrorOrContext> errorOrContextProvider,
        string attributeName)
    {
        return context.SyntaxProvider
            .ForAttributeWithMetadataName(
                attributeName,
                static (node, _) => node is MethodDeclarationSyntax,
                static (ctx, _) => ctx)
            .Combine(errorOrContextProvider)
            .SelectMany(static (pair, ct) => AnalyzeEndpointFlows(pair.Left, pair.Right, ct))
            .ReportAndContinue(context);
    }

    private static IncrementalValueProvider<EquatableArray<EndpointDescriptor>> CombineEndpointProviders(
        IncrementalValuesProvider<EndpointDescriptor> p1,
        IncrementalValuesProvider<EndpointDescriptor> p2,
        IncrementalValuesProvider<EndpointDescriptor> p3,
        IncrementalValuesProvider<EndpointDescriptor> p4,
        IncrementalValuesProvider<EndpointDescriptor> p5,
        IncrementalValuesProvider<EndpointDescriptor> p6)
    {
        var c1 = p1.CollectAsEquatableArray();
        var c2 = p2.CollectAsEquatableArray();
        var c3 = p3.CollectAsEquatableArray();
        var c4 = p4.CollectAsEquatableArray();
        var c5 = p5.CollectAsEquatableArray();
        var c6 = p6.CollectAsEquatableArray();

        var combined1 = c1.Combine(c2);
        var combined2 = combined1.Combine(c3);
        var combined3 = combined2.Combine(c4);
        var combined4 = combined3.Combine(c5);
        var combined5 = combined4.Combine(c6);

        return combined5.Select(static (combined, _) =>
        {
            var (((((e0, e1), e2), e3), e4), e5) = combined;
            var descriptors = ImmutableArray.CreateBuilder<EndpointDescriptor>(
                e0.Length + e1.Length + e2.Length + e3.Length + e4.Length + e5.Length);

            if (!e0.IsDefaultOrEmpty) descriptors.AddRange(e0.AsImmutableArray());
            if (!e1.IsDefaultOrEmpty) descriptors.AddRange(e1.AsImmutableArray());
            if (!e2.IsDefaultOrEmpty) descriptors.AddRange(e2.AsImmutableArray());
            if (!e3.IsDefaultOrEmpty) descriptors.AddRange(e3.AsImmutableArray());
            if (!e4.IsDefaultOrEmpty) descriptors.AddRange(e4.AsImmutableArray());
            if (!e5.IsDefaultOrEmpty) descriptors.AddRange(e5.AsImmutableArray());

            return new EquatableArray<EndpointDescriptor>(descriptors.ToImmutable());
        });
    }

    private static ImmutableArray<DiagnosticFlow<EndpointDescriptor>> AnalyzeEndpointFlows(
        GeneratorAttributeSyntaxContext ctx,
        ErrorOrContext errorOrContext,
        CancellationToken ct)
    {
        if (ctx.TargetSymbol is not IMethodSymbol method || ctx.Attributes.IsDefaultOrEmpty)
            return ImmutableArray<DiagnosticFlow<EndpointDescriptor>>.Empty;

        var location = method.Locations.FirstOrDefault() ?? Location.None;

        // 1. Validate shape using SemanticGuard + DiagnosticFlow (The Railway Pattern)
        var methodAnalysisFlow = SemanticGuard.For(method)
            .MustBeStatic(DiagnosticInfo.Create(Descriptors.NonStaticHandler, location, method.Name))
            .ToFlow()
            .Then(m =>
            {
                var returnInfo = ExtractErrorOrReturnType(m.ReturnType, errorOrContext);
                return returnInfo.SuccessTypeFqn is not null
                    ? DiagnosticFlow.Ok((m, returnInfo))
                    : DiagnosticFlow.Fail<(IMethodSymbol, ErrorOrReturnTypeInfo)>(
                        DiagnosticInfo.Create(Descriptors.InvalidReturnType, location, m.Name));
            })
            .Then(pair =>
            {
                var (m, returnInfo) = pair;
                var builder = ImmutableArray.CreateBuilder<DiagnosticInfo>();

                // Extract method-level attributes first (needed for interface call detection)
                var producesErrors = ExtractProducesErrorAttributes(m, errorOrContext);
                var isAcceptedResponse = HasAcceptedResponseAttribute(m, errorOrContext);
                var hasExplicitProducesError = !producesErrors.IsDefaultOrEmpty;

                // Infer errors once per method (now with interface call detection)
                var (inferredErrors, customErrors) =
                    InferErrorTypesFromMethod(ctx, m, errorOrContext, builder, hasExplicitProducesError);

                var analysis = new MethodAnalysis(
                    m,
                    returnInfo,
                    inferredErrors,
                    customErrors,
                    producesErrors,
                    isAcceptedResponse);

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
            var flow = methodAnalysisFlow.Then(analysis => ProcessAttributeFlow(analysis, attr, errorOrContext, ct));
            flows.Add(flow);
        }

        return flows.ToImmutable();
    }

    private static DiagnosticFlow<EndpointDescriptor> ProcessAttributeFlow(
        MethodAnalysis analysis,
        AttributeData attr,
        ErrorOrContext errorOrContext,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var attrClass = attr.AttributeClass;
        if (attrClass is null) return DiagnosticFlow.Fail<EndpointDescriptor>();
        var attrName = attrClass.Name;

        var (httpMethod, pattern) = ExtractHttpMethodAndPattern(attr, attrName);
        if (httpMethod is null) return DiagnosticFlow.Fail<EndpointDescriptor>();

        // Guard: SuccessTypeFqn validated in upstream .Then() but compiler doesn't know
        if (analysis.ReturnInfo.SuccessTypeFqn is not { } successTypeFqn)
            return DiagnosticFlow.Fail<EndpointDescriptor>();

        var builder = ImmutableArray.CreateBuilder<DiagnosticInfo>();

        // Extract route parameters as HashSet for binding
        var routeParamInfos = RouteValidator.ExtractRouteParameters(pattern);
        var routeParamNames = routeParamInfos
            .Select(static r => r.Name)
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        // Bind parameters
        var bindingDiagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();
        var bindingResult = BindParameters(analysis.Method, routeParamNames, bindingDiagnostics, errorOrContext);
        if (!bindingResult.IsValid)
            return DiagnosticFlow.Fail<EndpointDescriptor>(bindingDiagnostics.ToImmutable().AsEquatableArray());

        builder.AddRange(bindingDiagnostics);

        // Validate route pattern
        builder.AddRange(RouteValidator.ValidatePattern(pattern, analysis.Method, attrName));

        // Extract method parameter info for route binding validation
        var methodParams = bindingResult.Parameters
            .Where(static p => p.Source == EndpointParameterSource.Route)
            .Select(static p => new MethodParameterInfo(p.Name, p.KeyName ?? p.Name, p.TypeFqn, p.IsNullable))
            .ToImmutableArray();

        // Validate route parameters are bound
        builder.AddRange(RouteValidator.ValidateParameterBindings(
            pattern, routeParamInfos, methodParams, analysis.Method));

        // Validate route constraint types
        builder.AddRange(RouteValidator.ValidateConstraintTypes(
            routeParamInfos, methodParams, analysis.Method));

        var descriptor = new EndpointDescriptor(
            httpMethod,
            pattern,
            successTypeFqn,
            analysis.ReturnInfo.Kind,
            analysis.ReturnInfo.IsAsync,
            analysis.Method.ContainingType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "Unknown",
            analysis.Method.Name,
            new EquatableArray<EndpointParameter>(bindingResult.Parameters),
            analysis.InferredErrors,
            analysis.InferredCustomErrors,
            analysis.ProducesErrors,
            analysis.ReturnInfo.IsSse,
            analysis.ReturnInfo.SseItemTypeFqn,
            analysis.IsAcceptedResponse);

        var flow = DiagnosticFlow.Ok(descriptor);
        foreach (var diag in builder)
            flow = flow.Warn(diag);

        return flow;
    }

    private static (string? HttpMethod, string Pattern) ExtractHttpMethodAndPattern(
        AttributeData attr,
        string attrName)
    {
        var httpMethod = attrName switch
        {
            "GetAttribute" or "Get" => WellKnownTypes.HttpMethod.Get,
            "PostAttribute" or "Post" => WellKnownTypes.HttpMethod.Post,
            "PutAttribute" or "Put" => WellKnownTypes.HttpMethod.Put,
            "DeleteAttribute" or "Delete" => WellKnownTypes.HttpMethod.Delete,
            "PatchAttribute" or "Patch" => WellKnownTypes.HttpMethod.Patch,
            "ErrorOrEndpointAttribute" or "ErrorOrEndpoint" when
                attr.ConstructorArguments is [{ Value: string m }, ..]
                => m.ToUpperInvariant(),
            _ => null
        };

        if (httpMethod is null)
            return (null, "/");

        // Extract pattern
        var patternIndex = attrName.Contains("ErrorOrEndpoint") ? 1 : 0;
        var pattern = "/";
        if (attr.ConstructorArguments.Length > patternIndex &&
            attr.ConstructorArguments[patternIndex].Value is string p)
            pattern = p;

        return (httpMethod, pattern);
    }

#pragma warning restore EPS06
}