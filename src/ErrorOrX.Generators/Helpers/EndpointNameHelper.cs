namespace ErrorOr.Generators;

/// <summary>
///     Domain-specific utilities for endpoint naming conventions.
///     For generic type name manipulation, use <see cref="StringExtensions" />.
/// </summary>
internal static class EndpointNameHelper
{
    private const string EndpointsSuffix = "Endpoints";

    /// <summary>
    ///     Computes the tag name from a containing type name.
    ///     Strips "Endpoints" suffix if present (e.g., "TodoEndpoints" -> "Todo").
    /// </summary>
    private static string GetTagName(string className)
    {
        return className.StripSuffix(EndpointsSuffix);
    }

    /// <summary>
    ///     Computes both tag name and operation ID for an endpoint.
    /// </summary>
    public static (string TagName, string OperationId) GetEndpointIdentity(string containingTypeFqn, string methodName)
    {
        var className = containingTypeFqn.ExtractShortTypeName();
        var tagName = GetTagName(className);
        var normalizedType = containingTypeFqn.StripGlobalPrefix();
        var opPrefix = normalizedType.SanitizeIdentifier();

        return (tagName, $"{opPrefix}_{methodName}");
    }
}
