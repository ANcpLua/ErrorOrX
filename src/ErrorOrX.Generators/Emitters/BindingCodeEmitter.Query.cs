namespace ErrorOr.Generators.Emitters;

/// <summary>
///     Query and Header binding emission. Both sources share the same scalar/collection branching
///     and the same nullable-vs-required emission shape; only the source-extraction call differs
///     (<c>TryGetQueryValue</c> / <c>ctx.Request.Query["..."]</c> vs <c>ctx.Request.Headers.TryGetValue</c>).
///     Also hosts <see cref="EmitBindAsyncBinding" /> since it's a query-bound custom hook.
/// </summary>
internal static partial class BindingCodeEmitter
{
    internal static bool EmitQueryBinding(StringBuilder code, in EndpointParameter param, string paramName,
        string bindFailFn)
    {
        if (param.CustomBinding is CustomBindingMethod.BindAsync or CustomBindingMethod.BindAsyncWithParam)
            return EmitBindAsyncBinding(code, in param, paramName, bindFailFn);

        var queryKey = param.KeyName ?? param.Name;
        return param is { IsCollection: true, CollectionItemTypeFqn: { } itemType }
            ? EmitCollectionQueryBinding(code, in param, paramName, queryKey, itemType, bindFailFn)
            : EmitScalarQueryBinding(code, in param, paramName, queryKey, bindFailFn);
    }

    internal static bool EmitBindAsyncBinding(StringBuilder code, in EndpointParameter param, string paramName,
        string bindFailFn)
    {
        var baseType = param.TypeFqn.TrimEnd('?');
        code.AppendLine($"            var {paramName} = await {baseType}.BindAsync(ctx);");
        if (param.IsNullable) return false;

        code.AppendLine(
            $"            if ({paramName} is null) return {bindFailFn}(\"{param.Name}\", \"binding failed\");");
        return true;
    }

    internal static bool EmitCollectionQueryBinding(StringBuilder code, in EndpointParameter param, string paramName,
        string queryKey, string itemType, string bindFailFn)
    {
        code.AppendLine($"            var {paramName}Raw = ctx.Request.Query[\"{queryKey}\"];");
        code.AppendLine($"            var {paramName}List = new {WellKnownTypes.Fqn.List}<{itemType}>();");
        code.AppendLine($"            foreach (var item in {paramName}Raw)");
        code.AppendLine("            {");

        var usesBindFail = false;
        if (itemType.IsStringType())
        {
            code.AppendLine(
                $"                if (item is {{ Length: > 0 }} validItem) {paramName}List.Add(validItem);");
        }
        else
        {
            usesBindFail = true;
            code.AppendLine(
                $"                if ({GetTryParseExpression(itemType, "item", "parsedItem")}) {paramName}List.Add(parsedItem);");
            code.AppendLine(
                $"                else if (!string.IsNullOrEmpty(item)) return {bindFailFn}(\"{param.Name}\", \"has invalid item format\");");
        }

        code.AppendLine("            }");
        var isArray = param.TypeFqn.EndsWithOrdinal("[]");
        var assignment = isArray ? $"{paramName}List.ToArray()" : $"{paramName}List";
        code.AppendLine($"            var {paramName} = {assignment};");
        return usesBindFail;
    }

    internal static bool EmitScalarQueryBinding(StringBuilder code, in EndpointParameter param, string paramName,
        string queryKey, string bindFailFn)
    {
        var usesBindFail = false;
        var declType = param.TypeFqn.EndsWithOrdinal("?") ? param.TypeFqn : param.TypeFqn + "?";
        code.AppendLine($"            {declType} {paramName};");
        code.AppendLine($"            if (!TryGetQueryValue(ctx, \"{queryKey}\", out var {paramName}Raw))");
        code.AppendLine("            {");
        if (param.IsNullable)
        {
            code.AppendLine($"                {paramName} = default;");
        }
        else
        {
            usesBindFail = true;
            code.AppendLine($"                return {bindFailFn}(\"{param.Name}\", \"is required\");");
        }

        code.AppendLine("            }");
        code.AppendLine("            else");
        code.AppendLine("            {");
        if (param.TypeFqn.IsStringType())
        {
            code.AppendLine($"                {paramName} = {paramName}Raw;");
        }
        else
        {
            usesBindFail = true;
            code.AppendLine(
                $"                if (!{GetTryParseExpression(param.TypeFqn, paramName + "Raw", paramName + "Temp", param.CustomBinding)}) return {bindFailFn}(\"{param.Name}\", \"has invalid format\");");
            code.AppendLine($"                {paramName} = {paramName}Temp;");
        }

        code.AppendLine("            }");
        return usesBindFail;
    }

    internal static bool EmitHeaderBinding(StringBuilder code, in EndpointParameter param, string paramName,
        string bindFailFn)
    {
        var key = param.KeyName ?? param.Name;
        var usesBindFail = false;

        if (param is { IsCollection: true, CollectionItemTypeFqn: { } itemType })
        {
            code.AppendLine($"            {param.TypeFqn} {paramName};");
            code.AppendLine(
                $"            if (!ctx.Request.Headers.TryGetValue(\"{key}\", out var {paramName}Raw) || {paramName}Raw.Count is 0)");
            code.AppendLine("            {");
            if (param.IsNullable)
            {
                code.AppendLine($"                {paramName} = default!;");
            }
            else
            {
                usesBindFail = true;
                code.AppendLine($"                return {bindFailFn}(\"{param.Name}\", \"is required\");");
            }

            code.AppendLine("            }");
            code.AppendLine("            else");
            code.AppendLine("            {");
            code.AppendLine($"                var {paramName}List = new {WellKnownTypes.Fqn.List}<{itemType}>();");
            code.AppendLine($"                foreach (var item in {paramName}Raw)");
            code.AppendLine("                {");
            code.AppendLine(
                itemType.IsStringType()
                    ? $"                    if (item is {{ Length: > 0 }} validItem) {paramName}List.Add(validItem);"
                    : $"                    if ({GetTryParseExpression(itemType, "item", "parsedItem")}) {paramName}List.Add(parsedItem);");
            code.AppendLine("                }");
            var isArray = param.TypeFqn.EndsWithOrdinal("[]");
            var assignment = isArray ? $"{paramName}List.ToArray()" : $"{paramName}List";
            code.AppendLine($"                {paramName} = {assignment};");
        }
        else
        {
            var declType = param.TypeFqn.EndsWithOrdinal("?") ? param.TypeFqn : param.TypeFqn + "?";
            code.AppendLine($"            {declType} {paramName};");
            code.AppendLine(
                $"            if (!ctx.Request.Headers.TryGetValue(\"{key}\", out var {paramName}Raw) || {paramName}Raw.Count is 0)");
            code.AppendLine("            {");
            if (param.IsNullable)
            {
                code.AppendLine($"                {paramName} = default;");
            }
            else
            {
                usesBindFail = true;
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
                usesBindFail = true;
                code.AppendLine(
                    $"                if (!{GetTryParseExpression(param.TypeFqn, paramName + "Raw.ToString()", paramName + "Temp")}) return {bindFailFn}(\"{param.Name}\", \"has invalid format\"); {paramName} = {paramName}Temp;");
            }
        }

        code.AppendLine("            }");
        return usesBindFail;
    }
}
