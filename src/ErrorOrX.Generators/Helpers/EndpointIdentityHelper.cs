namespace ErrorOr.Generators;

/// <summary>
///     Centralized logic for computing endpoint identity (operationId, tagName).
///     CRITICAL: Both ErrorOrEndpointGenerator.Emitter and OpenApiTransformerGenerator
///     must use this helper to ensure OpenAPI spec matches actual endpoints.
/// </summary>
internal static class EndpointIdentityHelper
{
    private const string EndpointsSuffix = "Endpoints";

    /// <summary>
    ///     Computes the tag name from a containing type name.
    ///     Strips "Endpoints" suffix if present (e.g., "TodoEndpoints" -> "Todo").
    /// </summary>
    public static string GetTagName(string className)
    {
        return className.EndsWith(EndpointsSuffix)
            ? className[..^EndpointsSuffix.Length]
            : className;
    }

    /// <summary>
    ///     Computes both tag name and operation ID for an endpoint.
    /// </summary>
    public static (string TagName, string OperationId) GetEndpointIdentity(string className, string methodName)
    {
        var tagName = GetTagName(className);
        var operationId = $"{tagName}_{methodName}";
        return (tagName, operationId);
    }
}