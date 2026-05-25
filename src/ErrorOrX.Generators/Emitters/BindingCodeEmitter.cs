namespace ErrorOr.Generators.Emitters;

/// <summary>
///     Emits the per-parameter C# binding code consumed by the generated <c>Invoke_Ep{N}_Core</c>
///     methods. Partial across:
///     <list type="bullet">
///         <item><c>BindingCodeEmitter.cs</c> — Dispatcher, Route, and special / service / form-file bindings.</item>
///         <item><c>BindingCodeEmitter.Query.cs</c> — Query and Header bindings (scalar + collection).</item>
///         <item><c>BindingCodeEmitter.Body.cs</c> — Body, Form (DTO + scalar), and AsParameters expansion.</item>
///         <item>
///             <c>BindingCodeEmitter.Parsing.cs</c> — Shared <c>BuildArgumentExpression</c>,
///             <c>GetTryParseExpression</c>, validation dict builder.
///         </item>
///     </list>
/// </summary>
internal static partial class BindingCodeEmitter
{
    /// <summary>
    ///     Emits parameter binding code and returns whether BindFail helper is used.
    /// </summary>
    internal static bool EmitParameterBinding(StringBuilder code, in EndpointParameter param, string paramName,
        string bindFailFn)
    {
        var source = param.Source;

        if (source == ParameterSource.Route) return EmitRouteBinding(code, in param, paramName, bindFailFn);
        if (source == ParameterSource.Query) return EmitQueryBinding(code, in param, paramName, bindFailFn);
        if (source == ParameterSource.Header) return EmitHeaderBinding(code, in param, paramName, bindFailFn);
        if (source == ParameterSource.Body) return EmitBodyBinding(code, in param, paramName, bindFailFn);
        if (source == ParameterSource.Service) return EmitServiceBinding(code, in param, paramName);
        if (source == ParameterSource.KeyedService) return EmitKeyedServiceBinding(code, in param, paramName);
        if (source == ParameterSource.HttpContext) return EmitHttpContextBinding(code, paramName);
        if (source == ParameterSource.CancellationToken) return EmitCancellationTokenBinding(code, paramName);
        if (source == ParameterSource.Stream) return EmitStreamBinding(code, paramName);
        if (source == ParameterSource.PipeReader) return EmitPipeReaderBinding(code, paramName);
        if (source == ParameterSource.FormFile) return EmitFormFileBinding(code, in param, paramName, bindFailFn);
        if (source == ParameterSource.FormFiles) return EmitFormFilesBinding(code, paramName);
        if (source == ParameterSource.FormCollection) return EmitFormCollectionBinding(code, paramName);
        if (source == ParameterSource.Form) return EmitFormBinding(code, in param, paramName, bindFailFn);
        if (source == ParameterSource.AsParameters)
            return EmitAsParametersBinding(code, in param, paramName, bindFailFn);

        throw new ArgumentOutOfRangeException(nameof(param), $"Unknown parameter source: {source}");
    }

    internal static bool EmitServiceBinding(StringBuilder code, in EndpointParameter param, string paramName)
    {
        code.AppendLine($"            var {paramName} = ctx.RequestServices.GetRequiredService<{param.TypeFqn}>();");
        return false;
    }

    internal static bool EmitKeyedServiceBinding(StringBuilder code, in EndpointParameter param, string paramName)
    {
        code.AppendLine(
            $"            var {paramName} = ctx.RequestServices.GetRequiredKeyedService<{param.TypeFqn}>({param.KeyName});");
        return false;
    }

    internal static bool EmitHttpContextBinding(StringBuilder code, string paramName)
    {
        code.AppendLine($"            var {paramName} = ctx;");
        return false;
    }

    internal static bool EmitCancellationTokenBinding(StringBuilder code, string paramName)
    {
        code.AppendLine($"            var {paramName} = ctx.RequestAborted;");
        return false;
    }

    internal static bool EmitStreamBinding(StringBuilder code, string paramName)
    {
        code.AppendLine($"            var {paramName} = ctx.Request.Body;");
        return false;
    }

    internal static bool EmitPipeReaderBinding(StringBuilder code, string paramName)
    {
        code.AppendLine($"            var {paramName} = ctx.Request.BodyReader;");
        return false;
    }

    internal static bool EmitFormFilesBinding(StringBuilder code, string paramName)
    {
        code.AppendLine($"            var {paramName} = form.Files;");
        return false;
    }

    internal static bool EmitFormCollectionBinding(StringBuilder code, string paramName)
    {
        code.AppendLine($"            var {paramName} = form;");
        return false;
    }

    internal static bool EmitFormFileBinding(StringBuilder code, in EndpointParameter param, string paramName,
        string bindFailFn)
    {
        code.AppendLine($"            var {paramName} = form.Files.GetFile(\"{param.KeyName ?? param.Name}\");");
        if (param.IsNullable) return false;

        code.AppendLine(
            $"            if ({paramName} is null) return {bindFailFn}(\"{param.Name}\", \"is required\");");
        return true;
    }

    internal static bool EmitRouteBinding(StringBuilder code, in EndpointParameter param, string paramName,
        string bindFailFn)
    {
        var routeName = param.KeyName ?? param.Name;
        code.AppendLine(
            param.TypeFqn.IsStringType()
                ? $"            if (!TryGetRouteValue(ctx, \"{routeName}\", out var {paramName})) return {bindFailFn}(\"{param.Name}\", \"is missing from route\");"
                : $"            if (!TryGetRouteValue(ctx, \"{routeName}\", out var {paramName}Raw) || !{GetTryParseExpression(param.TypeFqn, paramName + "Raw", paramName, param.CustomBinding)}) return {bindFailFn}(\"{param.Name}\", \"has invalid format\");");
        return true;
    }
}
