namespace ErrorOr.Generators;

/// <summary>
///     Information about a route parameter extracted from the route template.
/// </summary>
internal readonly record struct RouteParameterInfo(
    string Name,
    string? Constraint,
    bool IsOptional,
    bool IsCatchAll);

/// <summary>
///     Information about a method parameter relevant to route binding validation.
/// </summary>
internal readonly record struct RouteMethodParameterInfo(
    string Name,
    string? BoundRouteName,
    string? TypeFqn,
    bool IsNullable);

/// <summary>
///     Result of route binding analysis containing bound parameters and route-specific extraction.
/// </summary>
internal readonly record struct RouteBindingAnalysis(
    EquatableArray<EndpointParameter> Parameters,
    EquatableArray<RouteMethodParameterInfo> RouteParameters);

/// <summary>
///     Route group configuration extracted from [RouteGroup] attribute on containing type.
/// </summary>
internal readonly record struct RouteGroupInfo(
    string? GroupPath,
    string? ApiName)
{
    /// <summary>
    ///     Returns true if route grouping is enabled for this endpoint.
    /// </summary>
    public bool HasRouteGroup => GroupPath is not null;
}
