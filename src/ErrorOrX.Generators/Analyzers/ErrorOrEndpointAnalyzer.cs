using ANcpLua.Roslyn.Utilities;
using ErrorOr.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ErrorOr.Analyzers;

/// <summary>
///     Real-time Roslyn analyzer for ErrorOr.Endpoints endpoints.
///     Provides immediate IDE feedback for common issues.
/// </summary>
/// <remarks>
///     <para>
///         This analyzer handles single-method diagnostics that can run fast.
///         Cross-file diagnostics (EOE004, EOE007) remain in the generator.
///         Route classification (Stream/PipeReader/IFormFile/etc.) is delegated to
///         <see cref="ErrorOrContext" /> so analyzer and generator stay in lockstep.
///     </para>
///     <para>
///         Split across:
///         <list type="bullet">
///             <item><c>ErrorOrEndpointAnalyzer.cs</c> — Entry, Initialize, top-level analysis loop, return-type / attribute extraction.</item>
///             <item><c>ErrorOrEndpointAnalyzer.RouteValidation.cs</c> — Pattern parsing + per-constraint validation.</item>
///             <item><c>ErrorOrEndpointAnalyzer.BodyAndValidation.cs</c> — Body-source counting, DataAnnotations reflection check.</item>
///         </list>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class ErrorOrEndpointAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        Descriptors.InvalidReturnType,
        Descriptors.NonStaticHandler,
        Descriptors.RouteParameterNotBound,
        Descriptors.InvalidRoutePattern,
        Descriptors.MultipleBodySources,
        Descriptors.BodyOnReadOnlyMethod,
        Descriptors.AcceptedOnReadOnlyMethod,
        Descriptors.RouteConstraintTypeMismatch,
        Descriptors.ValidationUsesReflection
    ];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(static ctx => AnalyzeMethod(in ctx), SymbolKind.Method);
    }

    private static void AnalyzeMethod(in SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol method) return;

        // Find ErrorOr endpoint attributes
        var endpointAttributes = GetEndpointAttributes(method);
        if (endpointAttributes.Count is 0) return;

        // EOE002: Handler must be static
        if (!method.IsStatic)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.NonStaticHandler,
                method.Locations.FirstOrDefault(),
                method.Name));
            return; // Don't report more diagnostics for invalid handler
        }

        // EOE001: Invalid return type
        if (!IsValidReturnType(method.ReturnType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.InvalidReturnType,
                method.Locations.FirstOrDefault(),
                method.Name));
            return; // Don't report more diagnostics for invalid handler
        }

        // Analyze each endpoint attribute
        foreach (var (httpMethod, pattern, attributeLocation) in endpointAttributes)
            AnalyzeEndpoint(in context, method, httpMethod, pattern, attributeLocation);
    }

    private static void AnalyzeEndpoint(
        in SymbolAnalysisContext context,
        IMethodSymbol method,
        string httpMethod,
        string pattern,
        Location attributeLocation)
    {
        // EOE005: Invalid route pattern
        var patternDiagnostics = ValidateRoutePattern(pattern);
        foreach (var message in patternDiagnostics)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.InvalidRoutePattern,
                attributeLocation,
                pattern,
                message));
        }

        // If pattern is invalid, skip further route analysis
        if (patternDiagnostics.Count > 0) return;

        // Parse verb once at method scope — null for custom/unrecognized HTTP methods
        var verb = HttpVerbExtensions.ParseMethodString(httpMethod);

        // Extract route parameters with constraints — route binding requires a recognized verb
        var routeParams = ExtractRouteParametersWithConstraints(pattern);
        if (!routeParams.IsDefaultOrEmpty && verb is not null)
        {
            var routeParamNames = routeParams
                .Select(static r => r.Name)
                .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

            var errorOrContext = new ErrorOrContext(context.Compilation);

            var bindingFlow =
                RouteBindingHelper.BindRouteParameters(method, routeParamNames, errorOrContext, verb.Value);
            if (bindingFlow.IsSuccess)
            {
                var bindingAnalysis = bindingFlow.ValueOrDefault();
                var methodParamsByRouteName = RouteValidator.BuildRouteParameterLookup(
                    bindingAnalysis.RouteParameters.AsImmutableArray());

                // EOE003: Route parameter not bound
                foreach (var routeParam in routeParams)
                {
                    if (!methodParamsByRouteName.ContainsKey(routeParam.Name))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Descriptors.RouteParameterNotBound,
                            attributeLocation,
                            pattern,
                            routeParam.Name));
                    }
                }

                // EOE020: Route constraint type mismatch
                ValidateConstraintTypes(in context, routeParams, methodParamsByRouteName, attributeLocation);
            }
        }

        // EOE006: Multiple body sources
        var bodyCount = CountBodySources(method);
        if (bodyCount > 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.MultipleBodySources,
                method.Locations.FirstOrDefault(),
                method.Name));
        }

        // EOE008: Body on read-only HTTP method
        var isBodyless = verb?.IsBodyless() ?? WellKnownTypes.HttpMethod.IsBodyless(httpMethod);
        var hasBody = bodyCount > 0;
        if (hasBody && isBodyless)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.BodyOnReadOnlyMethod,
                attributeLocation,
                method.Name,
                httpMethod.ToUpperInvariant()));
        }

        // EOE009: [AcceptedResponse] on read-only method
        if (HasAcceptedResponseAttribute(method) && isBodyless)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.AcceptedOnReadOnlyMethod,
                attributeLocation,
                method.Name,
                httpMethod.ToUpperInvariant()));
        }

        // EOE039: DataAnnotations validation uses reflection
        CheckForValidationAttributes(in context, method);
    }

    private static bool IsErrorOr(ITypeSymbol type)
    {
        return type is INamedTypeSymbol { Name: "ErrorOr", IsGenericType: true } named &&
               named.ContainingNamespace?.ToDisplayString() is "ErrorOr" or "ErrorOr.Core.ErrorOr";
    }

    private static List<(string HttpMethod, string Pattern, Location Location)> GetEndpointAttributes(
        ISymbol method)
    {
        var results = new List<(string, string, Location)>();

        foreach (var attr in method.GetAttributes())
        {
            if (attr.AttributeClass?.Name is not { } attrName) continue;

            string? httpMethod = null;
            var pattern = "/";

            switch (attrName)
            {
                // Check for specific HTTP method attributes
                case "GetAttribute" or "Get":
                    httpMethod = WellKnownTypes.HttpMethod.Get;
                    break;
                case "PostAttribute" or "Post":
                    httpMethod = WellKnownTypes.HttpMethod.Post;
                    break;
                case "PutAttribute" or "Put":
                    httpMethod = WellKnownTypes.HttpMethod.Put;
                    break;
                case "DeleteAttribute" or "Delete":
                    httpMethod = WellKnownTypes.HttpMethod.Delete;
                    break;
                case "PatchAttribute" or "Patch":
                    httpMethod = WellKnownTypes.HttpMethod.Patch;
                    break;
                // Generic endpoint attribute - extract HTTP method from first constructor arg
                case "ErrorOrEndpointAttribute" or "ErrorOrEndpoint":
                {
                    if (attr.ConstructorArguments is [{ Value: string m }, ..]) httpMethod = m.ToUpperInvariant();

                    break;
                }
            }

            if (httpMethod is null) continue;

            // Extract pattern from constructor arguments
            var patternIndex = attrName.Contains("ErrorOrEndpoint") ? 1 : 0;
            if (attr.GetConstructorArgument<string>(patternIndex) is { } p)
            {
                pattern = p;
            }

            var location = attr.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                           ?? method.Locations.FirstOrDefault()
                           ?? Location.None;

            results.Add((httpMethod, pattern, location));
        }

        return results;
    }

    private static bool IsValidReturnType(ITypeSymbol returnType)
    {
        if (returnType is not INamedTypeSymbol named) return false;

        // Direct ErrorOr<T>
        if (IsErrorOr(named)) return true;

        // Task<ErrorOr<T>> or ValueTask<ErrorOr<T>>
        if (named.Name is "Task" or "ValueTask" && named is { IsGenericType: true, TypeArguments.Length: > 0 })
            return IsErrorOr(named.TypeArguments[0]);

        return false;
    }

    /// <summary>
    ///     Extracts route parameters with their constraints from a route pattern.
    ///     Delegates to RouteValidator which is the single source of truth for route parsing.
    /// </summary>
    private static ImmutableArray<RouteParameterInfo> ExtractRouteParametersWithConstraints(string pattern)
    {
        return RouteValidator.ExtractRouteParameters(pattern);
    }
}
