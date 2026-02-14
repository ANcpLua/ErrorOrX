namespace ErrorOr.Generators.Emitters;

/// <summary>
///     Shared helpers for emitting endpoint map calls (grouped and ungrouped).
///     Eliminates duplication of the map call line, WithName, and closing pattern.
/// </summary>
internal static class MapCallEmitter
{
    /// <summary>
    ///     Emits the opening map call line and WithName.
    ///     <c>var __ep{index} = {target}.MapGet(@"{pattern}", (Delegate)Invoke_Ep{index})</c>
    ///     <c>    .WithName("{operationId}")</c>
    /// </summary>
    public static void EmitMapCallStart(
        StringBuilder code,
        in EndpointDescriptor ep,
        string target,
        string pattern,
        int index,
        string indent)
    {
        var httpMethodStr = ep.HttpMethod;
        var useMapMethods = ep.CustomHttpMethod is not null || ep.HttpVerb.ToMapMethod() == "MapMethods";

        code.AppendLine(useMapMethods
            ? $"{indent}var __ep{index} = {target}.MapMethods(@\"{pattern}\", new[] {{ \"{httpMethodStr}\" }}, (Delegate)Invoke_Ep{index})"
            : $"{indent}var __ep{index} = {target}.{ep.HttpVerb.ToMapMethod()}(@\"{pattern}\", (Delegate)Invoke_Ep{index})");

        var (_, operationId) =
            EndpointNameHelper.GetEndpointIdentity(ep.HandlerContainingTypeFqn, ep.HandlerMethodName);
        code.AppendLine($"{indent}    .WithName(\"{operationId}\")");
    }

    /// <summary>
    ///     Emits the closing semicolon and endpoint builder registration.
    /// </summary>
    public static void EmitMapCallEnd(
        StringBuilder code,
        int index,
        string indent)
    {
        code.AppendLine($"{indent}    ;");
        code.AppendLine($"{indent}__endpointBuilders.Add(__ep{index});");
    }
}
