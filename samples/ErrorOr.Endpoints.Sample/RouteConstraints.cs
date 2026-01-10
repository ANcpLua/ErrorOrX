namespace ErrorOr.Endpoints.Sample;

/// <summary>
///     Route constraint for DateOnly values (format: yyyy-MM-dd).
/// </summary>
public sealed class DateOnlyRouteConstraint : IRouteConstraint
{
    public bool Match(HttpContext? httpContext, IRouter? route, string routeKey,
        RouteValueDictionary values, RouteDirection routeDirection)
    {
        if (!values.TryGetValue(routeKey, out var value) || value is null)
            return false;

        var valueString = value.ToString();
        return valueString is not null && DateOnly.TryParse(valueString, out _);
    }
}

/// <summary>
///     Route constraint for TimeOnly values (format: HH:mm:ss or HH:mm).
/// </summary>
public sealed class TimeOnlyRouteConstraint : IRouteConstraint
{
    public bool Match(HttpContext? httpContext, IRouter? route, string routeKey,
        RouteValueDictionary values, RouteDirection routeDirection)
    {
        if (!values.TryGetValue(routeKey, out var value) || value is null)
            return false;

        var valueString = value.ToString();
        return valueString is not null && TimeOnly.TryParse(valueString, out _);
    }
}

/// <summary>
///     Route constraint for TimeSpan values.
/// </summary>
public sealed class TimeSpanRouteConstraint : IRouteConstraint
{
    public bool Match(HttpContext? httpContext, IRouter? route, string routeKey,
        RouteValueDictionary values, RouteDirection routeDirection)
    {
        if (!values.TryGetValue(routeKey, out var value) || value is null)
            return false;

        var valueString = value.ToString();
        return valueString is not null && TimeSpan.TryParse(valueString, out _);
    }
}

/// <summary>
///     Route constraint for DateTimeOffset values.
/// </summary>
public sealed class DateTimeOffsetRouteConstraint : IRouteConstraint
{
    public bool Match(HttpContext? httpContext, IRouter? route, string routeKey,
        RouteValueDictionary values, RouteDirection routeDirection)
    {
        if (!values.TryGetValue(routeKey, out var value) || value is null)
            return false;

        var valueString = value.ToString();
        return valueString is not null && DateTimeOffset.TryParse(valueString, out _);
    }
}