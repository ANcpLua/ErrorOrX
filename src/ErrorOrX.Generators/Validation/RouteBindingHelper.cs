using ANcpLua.Roslyn.Utilities.Models;
using Microsoft.CodeAnalysis;

namespace ErrorOr.Generators;

internal static class RouteBindingHelper
{
    public static DiagnosticFlow<RouteBindingAnalysis> BindRouteParameters(
        IMethodSymbol method,
        ImmutableHashSet<string> routeParameters,
        ErrorOrContext context,
        HttpVerb httpVerb)
    {
        var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();
        var bindingResult = ErrorOrEndpointGenerator.BindParameters(
            method,
            routeParameters,
            diagnostics,
            context,
            httpVerb);

        if (!bindingResult.IsValid)
            return DiagnosticFlow.Fail<RouteBindingAnalysis>(diagnostics.ToImmutable().AsEquatableArray());

        var routeParametersInfo = ExtractRouteMethodParameters(bindingResult.Parameters.AsImmutableArray());
        var analysis = new RouteBindingAnalysis(
            bindingResult.Parameters,
            routeParametersInfo);

        var flow = DiagnosticFlow.Ok(analysis);
        foreach (var diagnostic in diagnostics)
            flow = flow.Warn(diagnostic);

        return flow;
    }

    private static EquatableArray<RouteMethodParameterInfo> ExtractRouteMethodParameters(
        ImmutableArray<EndpointParameter> parameters)
    {
        if (parameters.IsDefaultOrEmpty)
            return new EquatableArray<RouteMethodParameterInfo>(ImmutableArray<RouteMethodParameterInfo>.Empty);

        var builder = ImmutableArray.CreateBuilder<RouteMethodParameterInfo>();
        foreach (var parameter in parameters)
            CollectRouteMethodParameters(in parameter, builder);

        return new EquatableArray<RouteMethodParameterInfo>(builder.ToImmutable());
    }

    private static void CollectRouteMethodParameters(
        in EndpointParameter parameter,
        ImmutableArray<RouteMethodParameterInfo>.Builder builder)
    {
        if (parameter.Source == ParameterSource.Route)
            builder.Add(new RouteMethodParameterInfo(
                parameter.Name,
                parameter.KeyName ?? parameter.Name,
                parameter.TypeFqn,
                parameter.IsNullable));

        if (parameter.Children.IsDefaultOrEmpty) return;

        foreach (var child in parameter.Children.AsImmutableArray())
            CollectRouteMethodParameters(in child, builder);
    }
}
