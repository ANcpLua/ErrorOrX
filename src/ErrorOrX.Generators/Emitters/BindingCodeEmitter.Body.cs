namespace ErrorOr.Generators.Emitters;

/// <summary>
///     Request-body binding emission: JSON body (with empty-body Allow/Disallow split),
///     <c>multipart/form-data</c> field binding, and <c>[AsParameters]</c> constructor expansion.
///     All three consume something from <c>ctx.Request</c> and produce either a parsed DTO or a
///     <c>BindFail</c> short-circuit.
/// </summary>
internal static partial class BindingCodeEmitter
{
    internal static bool EmitBodyBinding(StringBuilder code, in EndpointParameter param, string paramName,
        string bindFailFn)
    {
        // Determine effective behavior: explicit > nullability-based default
        var effectiveBehavior = param.EmptyBodyBehavior;
        if (effectiveBehavior == EmptyBodyBehavior.Default)
            effectiveBehavior = param.IsNullable ? EmptyBodyBehavior.Allow : EmptyBodyBehavior.Disallow;

        return effectiveBehavior switch
        {
            EmptyBodyBehavior.Allow => EmitBodyBindingAllow(code, in param, paramName, bindFailFn),
            _ => EmitBodyBindingDisallow(code, in param, paramName, bindFailFn)
        };
    }

    internal static bool EmitBodyBindingAllow(StringBuilder code, in EndpointParameter param, string paramName,
        string bindFailFn)
    {
        // Allow empty bodies - check ContentLength before reading
        code.AppendLine($"            {param.TypeFqn}? {paramName};");
        code.AppendLine("            if (ctx.Request.ContentLength is null or 0)");
        code.AppendLine("            {");
        code.AppendLine($"                {paramName} = default;");
        code.AppendLine("            }");
        code.AppendLine("            else");
        code.AppendLine("            {");
        code.AppendLine("                if (!ctx.Request.HasJsonContentType()) return BindFail415();");
        code.AppendLine("                try");
        code.AppendLine("                {");
        code.AppendLine(
            $"                    {paramName} = await ctx.Request.ReadFromJsonAsync<{param.TypeFqn}>(cancellationToken: ctx.RequestAborted);");
        code.AppendLine("                }");
        code.AppendLine($"                catch ({WellKnownTypes.Fqn.JsonException})");
        code.AppendLine("                {");
        code.AppendLine($"                    return {bindFailFn}(\"{param.Name}\", \"has invalid JSON format\");");
        code.AppendLine("                }");
        code.AppendLine("            }");
        return true;
    }

    internal static bool EmitBodyBindingDisallow(StringBuilder code, in EndpointParameter param, string paramName,
        string bindFailFn)
    {
        // Disallow empty bodies - reject with 400 if empty
        code.AppendLine("            if (ctx.Request.ContentLength is null or 0)");
        code.AppendLine($"                return {bindFailFn}(\"{param.Name}\", \"is required\");");
        code.AppendLine("            if (!ctx.Request.HasJsonContentType()) return BindFail415();");
        code.AppendLine($"            {param.TypeFqn}? {paramName};");
        code.AppendLine("            try");
        code.AppendLine("            {");
        code.AppendLine(
            $"                {paramName} = await ctx.Request.ReadFromJsonAsync<{param.TypeFqn}>(cancellationToken: ctx.RequestAborted);");
        code.AppendLine("            }");
        code.AppendLine($"            catch ({WellKnownTypes.Fqn.JsonException})");
        code.AppendLine("            {");
        code.AppendLine($"                return {bindFailFn}(\"{param.Name}\", \"has invalid JSON format\");");
        code.AppendLine("            }");
        code.AppendLine(
            $"            if ({paramName} is null) return {bindFailFn}(\"{param.Name}\", \"is required\");");
        return true;
    }

    internal static bool EmitFormBinding(StringBuilder code, in EndpointParameter param, string paramName,
        string bindFailFn)
    {
        if (!param.Children.IsDefaultOrEmpty)
        {
            var usesBindFail = false;
            for (var i = 0; i < param.Children.Length; i++)
            {
                var child = param.Children[i];
                usesBindFail |= EmitParameterBinding(code, in child, $"{paramName}_f{i}", bindFailFn);
            }

            var args = string.Join(", ", param.Children.AsImmutableArray().Select((_, i) => $"{paramName}_f{i}"));
            code.AppendLine($"            var {paramName} = new {param.TypeFqn}({args});");
            return usesBindFail;
        }

        var usesBindFailScalar = false;
        var fieldName = param.KeyName ?? param.Name;
        var declType = param.IsNullable && !param.TypeFqn.EndsWithOrdinal("?") ? param.TypeFqn + "?" : param.TypeFqn;
        code.AppendLine($"            {declType} {paramName};");
        code.AppendLine(
            $"            if (!form.TryGetValue(\"{fieldName}\", out var {paramName}Raw) || {paramName}Raw.Count is 0)");
        code.AppendLine("            {");
        if (param.IsNullable)
        {
            code.AppendLine($"                {paramName} = default;");
        }
        else
        {
            usesBindFailScalar = true;
            code.AppendLine($"                return {bindFailFn}(\"{param.Name}\", \"is required\");");
        }

        code.AppendLine("            }");
        code.AppendLine("            else");
        code.AppendLine("            {");
        if (param.TypeFqn.IsStringType())
        {
            code.AppendLine($"                {paramName} = {paramName}Raw.ToString();");
        }
        else
        {
            usesBindFailScalar = true;
            code.AppendLine(
                $"                if (!{GetTryParseExpression(param.TypeFqn, paramName + "Raw.ToString()", paramName + "Temp")}) return {bindFailFn}(\"{param.Name}\", \"has invalid format\");");
            code.AppendLine($"                {paramName} = {paramName}Temp;");
        }

        code.AppendLine("            }");
        return usesBindFailScalar;
    }

    internal static bool EmitAsParametersBinding(StringBuilder code, in EndpointParameter param, string paramName,
        string bindFailFn)
    {
        var usesBindFail = false;
        var childVars = new List<string>();
        for (var i = 0; i < param.Children.Length; i++)
        {
            var child = param.Children[i];
            var childVarName = $"{paramName}_c{i}";
            usesBindFail |= EmitParameterBinding(code, in child, childVarName, bindFailFn);
            childVars.Add(BuildArgumentExpression(in child, childVarName));
        }

        // Properties (non-constructor) bind into an object initializer: new T(ctorArgs) { Prop = value, ... }.
        var initAssignments = new List<string>();
        if (!param.InitProperties.IsDefaultOrEmpty)
        {
            for (var i = 0; i < param.InitProperties.Length; i++)
            {
                var prop = param.InitProperties[i];
                var propVarName = $"{paramName}_p{i}";
                usesBindFail |= EmitParameterBinding(code, in prop, propVarName, bindFailFn);
                initAssignments.Add($"{prop.Name} = {BuildArgumentExpression(in prop, propVarName)}");
            }
        }

        var initializer = initAssignments.Count > 0
            ? " { " + string.Join(", ", initAssignments) + " }"
            : "";
        code.AppendLine(
            $"            var {paramName} = new {param.TypeFqn}({string.Join(", ", childVars)}){initializer};");
        return usesBindFail;
    }
}
