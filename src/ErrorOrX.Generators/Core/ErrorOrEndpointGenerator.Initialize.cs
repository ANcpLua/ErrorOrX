using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ErrorOr.Generators;

/// <summary>
///     Generator entry point for ErrorOr endpoint mappings.
///     Generates MapErrorOrEndpoints() and AddErrorOrEndpoints() fluent configuration extension methods.
///     <para>
///         Pipeline wiring lives here. Sibling partials:
///         <list type="bullet">
///             <item><c>Initialize.Attributes.cs</c> — Marker attribute emission via PostInitializationOutput.</item>
///             <item><c>Initialize.EndpointFlow.cs</c> — Per-attribute endpoint discovery, validation, and descriptor build.</item>
///         </list>
///     </para>
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed partial class ErrorOrEndpointGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(EmitAttributes);

        // Create ErrorOrContext once per compilation (fixes N+1 symbol lookup performance issue)
        var errorOrContextProvider = ErrorOrContext.CreateProvider(context)
            .WithTrackingName(TrackingNames.ErrorOrContext);

        var endpoints = CombineHttpMethodProviders(context, errorOrContextProvider);
        var jsonContexts = JsonContextProvider.Create(context).CollectAsEquatableArray();
        var generateJsonContextOption = context.AnalyzerConfigOptionsProvider
            .IsToggleEnabled("ErrorOrGenerateJsonContext", defaultValue: true);
        var publishAotOption = context.AnalyzerConfigOptionsProvider
            .IsToggleEnabled("PublishAot", defaultValue: false);
        var referenceArities = context.MetadataReferencesProvider
            .Select(static (reference, _) => ResultsUnionTypeBuilder.GetResultsUnionArity(reference))
            .CollectAsEquatableArray();
        var maxResultsUnionArity = referenceArities
            .Select(static (arities, _) => ResultsUnionTypeBuilder.DetectMaxArity(arities.AsImmutableArray()))
            .WithTrackingName(TrackingNames.ResultsUnionMaxArity);

        var hasValidationResolverSupport = errorOrContextProvider
            .Select(static (ctx, _) => ctx.HasValidationResolverSupport);

        var emitInput = endpoints.Combine(jsonContexts).Combine(generateJsonContextOption).Combine(publishAotOption)
            .Combine(maxResultsUnionArity).Combine(hasValidationResolverSupport)
            .Select(static (data, _) =>
            {
                var (((((ep, jc), gjc), pa), mra), hvrs) = data;
                return new EmitContext(ep, jc, gjc, pa, mra, hvrs);
            });

        context.RegisterSourceOutput(emitInput, static (spc, ctx) => EmitMappingsAndRunAnalysis(spc, in ctx));
    }

    private static void EmitMappingsAndRunAnalysis(
        SourceProductionContext spc,
        in EmitContext ctx)
    {
        var endpointArray = Helpers.AsArrayOrEmpty(ctx.Endpoints);
        var jsonContextArray = Helpers.AsArrayOrEmpty(ctx.JsonContexts);

        ReportDuplicateRoutes(spc, endpointArray);
        ReportVersioningInconsistencies(spc, endpointArray);

        if (!endpointArray.IsDefaultOrEmpty)
        {
            EmitEndpoints(spc, endpointArray, jsonContextArray, ctx.MaxResultsUnionArity, ctx.GenerateJsonContext,
                ctx.HasValidationResolverSupport);
            AnalyzeJsonContextCoverage(spc, endpointArray, jsonContextArray, ctx.PublishAot);
            AnalyzeUnionTypeArity(spc, endpointArray, ctx.MaxResultsUnionArity);
        }
    }

    private static void ReportDuplicateRoutes(SourceProductionContext spc, ImmutableArray<EndpointDescriptor> endpoints)
    {
        foreach (var diagnostic in RouteValidator.DetectDuplicateRoutes(endpoints))
            spc.ReportDiagnostic(diagnostic);
    }

    private static void ReportVersioningInconsistencies(SourceProductionContext spc,
        ImmutableArray<EndpointDescriptor> endpoints)
    {
        foreach (var diagnostic in ApiVersioningValidator.DetectMissingVersioning(endpoints))
            spc.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    ///     Flattened context for the combined Roslyn pipeline inputs to <see cref="EmitMappingsAndRunAnalysis" />.
    /// </summary>
    private readonly record struct EmitContext(
        EquatableArray<EndpointDescriptor> Endpoints,
        EquatableArray<JsonContextInfo> JsonContexts,
        bool GenerateJsonContext,
        bool PublishAot,
        int MaxResultsUnionArity,
        bool HasValidationResolverSupport);

    /// <summary>
    ///     Small focused helpers for common pipeline operations.
    /// </summary>
    private static class Helpers
    {
        /// <summary>
        ///     Converts EquatableArray to ImmutableArray, returning Empty if default/empty.
        /// </summary>
        public static ImmutableArray<T> AsArrayOrEmpty<T>(EquatableArray<T> array) where T : IEquatable<T>
        {
            return array.IsDefaultOrEmpty ? ImmutableArray<T>.Empty : array.AsImmutableArray();
        }

        /// <summary>
        ///     Creates an empty endpoint descriptor flow for early-exit scenarios.
        /// </summary>
        public static DiagnosticFlow<EquatableArray<EndpointDescriptor>> EmptyEndpointFlow()
        {
            return DiagnosticFlow.Ok(new EquatableArray<EndpointDescriptor>(ImmutableArray<EndpointDescriptor>.Empty));
        }
    }
}
