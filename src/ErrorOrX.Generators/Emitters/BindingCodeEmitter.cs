using System.Collections.Immutable;
using System.Text;

namespace ErrorOr.Generators.Emitters;

internal static class BindingCodeEmitter
{
    internal static bool EmitParameterBinding(StringBuilder code, in EndpointParameter param, string paramName, string bindFailFn) =>
        param.Source switch
        {
            EndpointParameterSource.Route => EmitRouteBinding(code, in param, paramName, bindFailFn),
            EndpointParameterSource.Query => EmitQueryBinding(code, in param, paramName, bindFailFn),
            EndpointParameterSource.Header => EmitHeaderBinding(code, in param, paramName, bindFailFn),
            EndpointParameterSource.Body => EmitBodyBinding(code, in param, paramName, bindFailFn),
            EndpointParameterSource.Service => EmitServiceBinding(code, in param, paramName),
            EndpointParameterSource.KeyedService => EmitKeyedServiceBinding(code, in param, paramName),
            EndpointParameterSource.HttpContext => EmitHttpContextBinding(code, paramName),
            EndpointParameterSource.CancellationToken => EmitCancellationTokenBinding(code, paramName),
            EndpointParameterSource.Stream => EmitStreamBinding(code, paramName),
            EndpointParameterSource.PipeReader => EmitPipeReaderBinding(code, paramName),
            EndpointParameterSource.FormFile => EmitFormFileBinding(code, in param, paramName, bindFailFn),
            EndpointParameterSource.FormFiles => EmitFormFilesBinding(code, paramName),
            EndpointParameterSource.FormCollection => EmitFormCollectionBinding(code, paramName),
            EndpointParameterSource.Form => EmitFormBinding(code, in param, paramName, bindFailFn),
            EndpointParameterSource.AsParameters => EmitAsParametersBinding(code, in param, paramName, bindFailFn),
            _ => false
        };

    private static bool EmitServiceBinding(StringBuilder code, in EndpointParameter param, string paramName)
    {
        code.AppendLine($"            var {paramName} = ctx.RequestServices.GetRequiredService<{param.TypeFqn}>();");
        return false;
    }

    private static bool EmitKeyedServiceBinding(StringBuilder code, in EndpointParameter param, string paramName)
    {
        code.AppendLine($"            var {paramName} = ctx.RequestServices.GetRequiredKeyedService<{param.TypeFqn}>({param.KeyName});");
        return false;
    }

    private static bool EmitHttpContextBinding(StringBuilder code, string paramName)
    {
        code.AppendLine($"            var {paramName} = ctx;");
        return false;
    }

    private static bool EmitCancellationTokenBinding(StringBuilder code, string paramName)
    {
        code.AppendLine($"            var {paramName} = ctx.RequestAborted;");
        return false;
    }

    private static bool EmitStreamBinding(StringBuilder code, string paramName)
    {
        code.AppendLine($"            var {paramName} = ctx.Request.Body;");
        return false;
    }

    private static bool EmitPipeReaderBinding(StringBuilder code, string paramName)
    {
        code.AppendLine($"            var {paramName} = ctx.Request.BodyReader;");
        return false;
    }

    private static bool EmitFormFilesBinding(StringBuilder code, string paramName)
    {
        code.AppendLine($"            var {paramName} = form.Files;");
        return false;
    }

    private static bool EmitFormCollectionBinding(StringBuilder code, string paramName)
    {
        code.AppendLine($"            var {paramName} = form;");
        return false;
    }

    private static bool EmitFormFileBinding(StringBuilder code, in EndpointParameter param, string paramName, string bindFailFn)
    {
        code.AppendLine($"            var {paramName} = form.Files.GetFile(\"{param.KeyName ?? param.Name}\");");
        if (param.IsNullable) return false;
        code.AppendLine($"            if ({paramName} is null) return {bindFailFn}(\"{param.Name}\", \"is required\");");
        return true;
    }

    private static bool EmitRouteBinding(StringBuilder code, in EndpointParameter param, string paramName, string bindFailFn)
    {
        var routeName = param.KeyName ?? param.Name;
        code.AppendLine(
            TypeNameHelper.IsStringType(param.TypeFqn)
                ? $"            if (!TryGetRouteValue(ctx, \"{routeName}\", out var {paramName})) return {bindFailFn}(\"{param.Name}\", \"is missing from route\");"
                : $"            if (!TryGetRouteValue(ctx, \"{routeName}\", out var {paramName}Raw) || !{GetTryParseExpression(param.TypeFqn, paramName + "Raw", paramName, param.CustomBinding)}) return {bindFailFn}(\"{param.Name}\", \"has invalid format\");");
        return true;
    }

    private static bool EmitQueryBinding(StringBuilder code, in EndpointParameter param, string paramName, string bindFailFn)
    {
        if (param.CustomBinding is CustomBindingMethod.BindAsync or CustomBindingMethod.BindAsyncWithParam)
            return EmitBindAsyncBinding(code, in param, paramName, bindFailFn);

        var queryKey = param.KeyName ?? param.Name;
        return param is { IsCollection: true, CollectionItemTypeFqn: { } itemType }
            ? EmitCollectionQueryBinding(code, in param, paramName, queryKey, itemType, bindFailFn)
            : EmitScalarQueryBinding(code, in param, paramName, queryKey, bindFailFn);
    }

    private static bool EmitBindAsyncBinding(StringBuilder code, in EndpointParameter param, string paramName, string bindFailFn)
    {
        var baseType = param.TypeFqn.TrimEnd('?');
        code.AppendLine($"            var {paramName} = await {baseType}.BindAsync(ctx);");
        if (param.IsNullable) return false;
        code.AppendLine($"            if ({paramName} is null) return {bindFailFn}(\"{param.Name}\", \"binding failed\");");
        return true;
    }

    private static bool EmitCollectionQueryBinding(StringBuilder code, in EndpointParameter param, string paramName,
        string queryKey, string itemType, string bindFailFn)
    {
        code.AppendLine($"            var {paramName}Raw = ctx.Request.Query[\"{queryKey}\"];");
        code.AppendLine($"            var {paramName}List = new {WellKnownTypes.Fqn.List}<{itemType}>();");
        code.AppendLine($"            foreach (var item in {paramName}Raw)");
        code.AppendLine("            {");

        var usesBindFail = false;
        if (TypeNameHelper.IsStringType(itemType))
            code.AppendLine($"                if (item is {{ Length: > 0 }} validItem) {paramName}List.Add(validItem);");
        else
        {
            usesBindFail = true;
            code.AppendLine($"                if ({GetTryParseExpression(itemType, "item", "parsedItem")}) {paramName}List.Add(parsedItem);");
            code.AppendLine($"                else if (!string.IsNullOrEmpty(item)) return {bindFailFn}(\"{param.Name}\", \"has invalid item format\");");
        }

        code.AppendLine("            }");
        var isArray = param.TypeFqn.EndsWith("[]");
        var assignment = isArray ? $"{paramName}List.ToArray()" : $"{paramName}List";
        code.AppendLine($"            var {paramName} = {assignment};");
        return usesBindFail;
    }

    private static bool EmitScalarQueryBinding(StringBuilder code, in EndpointParameter param, string paramName,
        string queryKey, string bindFailFn)
    {
        var usesBindFail = false;
        var declType = param.TypeFqn.EndsWith("?") ? param.TypeFqn : param.TypeFqn + "?";
        code.AppendLine($"            {declType} {paramName};");
        code.AppendLine($"            if (!TryGetQueryValue(ctx, \"{queryKey}\", out var {paramName}Raw))");
        code.AppendLine("            {");
        if (param.IsNullable)
            code.AppendLine($"                {paramName} = default;");
        else
        {
            usesBindFail = true;
            code.AppendLine($"                return {bindFailFn}(\"{param.Name}\", \"is required\");");
        }
        code.AppendLine("            }");
        code.AppendLine("            else");
        code.AppendLine("            {");
        if (TypeNameHelper.IsStringType(param.TypeFqn))
            code.AppendLine($"                {paramName} = {paramName}Raw;");
        else
        {
            usesBindFail = true;
            code.AppendLine($"                if (!{GetTryParseExpression(param.TypeFqn, paramName + "Raw", paramName + "Temp", param.CustomBinding)}) return {bindFailFn}(\"{param.Name}\", \"has invalid format\");");
            code.AppendLine($"                {paramName} = {paramName}Temp;");
        }
        code.AppendLine("            }");
        return usesBindFail;
    }

    private static bool EmitHeaderBinding(StringBuilder code, in EndpointParameter param, string paramName, string bindFailFn)
    {
        var key = param.KeyName ?? param.Name;
        var usesBindFail = false;

        if (param is { IsCollection: true, CollectionItemTypeFqn: { } itemType })
        {
            code.AppendLine($"            {param.TypeFqn} {paramName};");
            code.AppendLine($"            if (!ctx.Request.Headers.TryGetValue(\"{key}\", out var {paramName}Raw) || {paramName}Raw.Count is 0)");
            code.AppendLine("            {");
            if (param.IsNullable)
                code.AppendLine($"                {paramName} = default!;");
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
                TypeNameHelper.IsStringType(itemType)
                    ? $"                    if (item is {{ Length: > 0 }} validItem) {paramName}List.Add(validItem);"
                    : $"                    if ({GetTryParseExpression(itemType, "item", "parsedItem")}) {paramName}List.Add(parsedItem);");
            code.AppendLine("                }");
            var isArray = param.TypeFqn.EndsWith("[]");
            var assignment = isArray ? $"{paramName}List.ToArray()" : $"{paramName}List";
            code.AppendLine($"                {paramName} = {assignment};");
        }
        else
        {
            var declType = param.TypeFqn.EndsWith("?") ? param.TypeFqn : param.TypeFqn + "?";
            code.AppendLine($"            {declType} {paramName};");
            code.AppendLine($"            if (!ctx.Request.Headers.TryGetValue(\"{key}\", out var {paramName}Raw) || {paramName}Raw.Count is 0)");
            code.AppendLine("            {");
            if (param.IsNullable)
                code.AppendLine($"                {paramName} = default;");
            else
            {
                usesBindFail = true;
                code.AppendLine($"                return {bindFailFn}(\"{param.Name}\", \"is required\");");
            }
            code.AppendLine("            }");
            code.AppendLine("            else");
            code.AppendLine("            {");
            if (TypeNameHelper.IsStringType(param.TypeFqn))
                code.AppendLine($"                {paramName} = {paramName}Raw.ToString();");
            else
            {
                usesBindFail = true;
                code.AppendLine($"                if (!{GetTryParseExpression(param.TypeFqn, paramName + "Raw.ToString()", paramName + "Temp")}) return {bindFailFn}(\"{param.Name}\", \"has invalid format\"); {paramName} = {paramName}Temp;");
            }
        }
        code.AppendLine("            }");
        return usesBindFail;
    }

    private static bool EmitBodyBinding(StringBuilder code, in EndpointParameter param, string paramName, string bindFailFn)
    {
        code.AppendLine("            if (!ctx.Request.HasJsonContentType()) return BindFail415();");
        code.AppendLine($"            {param.TypeFqn}? {paramName};");
        code.AppendLine("            try");
        code.AppendLine("            {");
        code.AppendLine($"                {paramName} = await ctx.Request.ReadFromJsonAsync<{param.TypeFqn}>(cancellationToken: ctx.RequestAborted);");
        code.AppendLine("            }");
        code.AppendLine($"            catch ({WellKnownTypes.Fqn.JsonException})");
        code.AppendLine("            {");
        code.AppendLine($"                return {bindFailFn}(\"{param.Name}\", \"has invalid JSON format\");");
        code.AppendLine("            }");
        code.AppendLine($"            if ({paramName} is null) return {bindFailFn}(\"{param.Name}\", \"is required\");");
        return true;
    }

    private static bool EmitFormBinding(StringBuilder code, in EndpointParameter param, string paramName, string bindFailFn)
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
        var declType = param.IsNullable && !param.TypeFqn.EndsWith("?") ? param.TypeFqn + "?" : param.TypeFqn;
        code.AppendLine($"            {declType} {paramName};");
        code.AppendLine($"            if (!form.TryGetValue(\"{fieldName}\", out var {paramName}Raw) || {paramName}Raw.Count is 0)");
        code.AppendLine("            {");
        if (param.IsNullable)
            code.AppendLine($"                {paramName} = default;");
        else
        {
            usesBindFailScalar = true;
            code.AppendLine($"                return {bindFailFn}(\"{param.Name}\", \"is required\");");
        }
        code.AppendLine("            }");
        code.AppendLine("            else");
        code.AppendLine("            {");
        if (TypeNameHelper.IsStringType(param.TypeFqn))
            code.AppendLine($"                {paramName} = {paramName}Raw.ToString();");
        else
        {
            usesBindFailScalar = true;
            code.AppendLine($"                if (!{GetTryParseExpression(param.TypeFqn, paramName + "Raw.ToString()", paramName + "Temp")}) return {bindFailFn}(\"{param.Name}\", \"has invalid format\");");
            code.AppendLine($"                {paramName} = {paramName}Temp;");
        }
        code.AppendLine("            }");
        return usesBindFailScalar;
    }

    private static bool EmitAsParametersBinding(StringBuilder code, in EndpointParameter param, string paramName, string bindFailFn)
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
        code.AppendLine($"            var {paramName} = new {param.TypeFqn}({string.Join(", ", childVars)});");
        return usesBindFail;
    }

    internal static void EmitFormContentTypeGuard(StringBuilder code)
    {
        code.AppendLine("            if (!ctx.Request.HasFormContentType) return BindFail415();");
        code.AppendLine("            var form = await ctx.Request.ReadFormAsync(ctx.RequestAborted);");
        code.AppendLine();
    }

    internal static string BuildArgumentExpression(in EndpointParameter param, string paramName) =>
        param.Source switch
        {
            EndpointParameterSource.Body when !param.IsNullable => paramName + "!",
            EndpointParameterSource.Route when param is { IsNullable: false, IsNonNullableValueType: false } => paramName + "!",
            EndpointParameterSource.Query when param is { IsNullable: false, IsNonNullableValueType: true } => paramName + ".Value",
            EndpointParameterSource.Header when param is { IsNullable: false, IsNonNullableValueType: true } => paramName + ".Value",
            EndpointParameterSource.Query when param is { IsNullable: false, IsNonNullableValueType: false } => paramName + "!",
            EndpointParameterSource.Header when param is { IsNullable: false, IsNonNullableValueType: false } => paramName + "!",
            _ => paramName
        };

    internal static string GetTryParseExpression(string typeFqn, string rawName, string outputName, CustomBindingMethod customBinding = CustomBindingMethod.None)
    {
        if (customBinding is CustomBindingMethod.TryParse or CustomBindingMethod.TryParseWithFormat)
        {
            var baseType = typeFqn.TrimEnd('?');
            return $"{baseType}.TryParse({rawName}, out var {outputName})";
        }

        var normalized = typeFqn.Replace("global::", "").TrimEnd('?');
        return normalized switch
        {
            "System.Int32" or "int" => $"int.TryParse({rawName}, out var {outputName})",
            "System.Int64" or "long" => $"long.TryParse({rawName}, out var {outputName})",
            "System.Int16" or "short" => $"short.TryParse({rawName}, out var {outputName})",
            "System.Byte" or "byte" => $"byte.TryParse({rawName}, out var {outputName})",
            "System.SByte" or "sbyte" => $"sbyte.TryParse({rawName}, out var {outputName})",
            "System.UInt16" or "ushort" => $"ushort.TryParse({rawName}, out var {outputName})",
            "System.UInt32" or "uint" => $"uint.TryParse({rawName}, out var {outputName})",
            "System.UInt64" or "ulong" => $"ulong.TryParse({rawName}, out var {outputName})",
            "System.Boolean" or "bool" => $"bool.TryParse({rawName}, out var {outputName})",
            "System.Guid" => $"global::System.Guid.TryParse({rawName}, out var {outputName})",
            "System.DateTime" => $"global::System.DateTime.TryParse({rawName}, out var {outputName})",
            "System.DateTimeOffset" => $"global::System.DateTimeOffset.TryParse({rawName}, out var {outputName})",
            "System.TimeSpan" => $"global::System.TimeSpan.TryParse({rawName}, out var {outputName})",
            "System.DateOnly" => $"global::System.DateOnly.TryParse({rawName}, out var {outputName})",
            "System.TimeOnly" => $"global::System.TimeOnly.TryParse({rawName}, out var {outputName})",
            "System.Double" or "double" => $"double.TryParse({rawName}, out var {outputName})",
            "System.Single" or "float" => $"float.TryParse({rawName}, out var {outputName})",
            "System.Decimal" or "decimal" => $"decimal.TryParse({rawName}, out var {outputName})",
            _ => "false"
        };
    }

    /// <summary>
    ///     Emits the standard validation dictionary building pattern.
    ///     Consolidates the repeated logic for aggregating errors by key into string arrays.
    /// </summary>
    /// <param name="code">The StringBuilder to append to.</param>
    /// <param name="indent">Base indentation (number of spaces).</param>
    /// <param name="dictName">Name of the dictionary variable.</param>
    /// <param name="iteratorSource">The collection to iterate over.</param>
    /// <param name="iteratorVar">Name of the loop variable.</param>
    /// <param name="keyExpr">Expression to get the dictionary key.</param>
    /// <param name="valueExpr">Expression to get the value to add.</param>
    /// <param name="filterExpr">Optional filter expression (items not matching are skipped).</param>
    /// <param name="keyVarDecl">Optional local variable declaration for key (emitted before TryGetValue).</param>
    internal static void EmitValidationDictBuilder(
        StringBuilder code,
        int indent,
        string dictName,
        string iteratorSource,
        string iteratorVar,
        string keyExpr,
        string valueExpr,
        string? filterExpr = null,
        string? keyVarDecl = null)
    {
        var pad = new string(' ', indent);
        var pad4 = new string(' ', indent + 4);
        var pad8 = new string(' ', indent + 8);

        code.AppendLine($"{pad}var {dictName} = new {WellKnownTypes.Fqn.Dictionary}<string, string[]>();");
        code.AppendLine($"{pad}foreach (var {iteratorVar} in {iteratorSource})");
        code.AppendLine($"{pad}{{");

        if (filterExpr is not null)
            code.AppendLine($"{pad4}if ({filterExpr}) continue;");

        if (keyVarDecl is not null)
            code.AppendLine($"{pad4}{keyVarDecl}");

        code.AppendLine($"{pad4}if (!{dictName}.TryGetValue({keyExpr}, out var existing))");
        code.AppendLine($"{pad8}{dictName}[{keyExpr}] = new[] {{ {valueExpr} }};");
        code.AppendLine($"{pad4}else");
        code.AppendLine($"{pad4}{{");
        code.AppendLine($"{pad8}var arr = new string[existing.Length + 1];");
        code.AppendLine($"{pad8}existing.CopyTo(arr, 0);");
        code.AppendLine($"{pad8}arr[existing.Length] = {valueExpr};");
        code.AppendLine($"{pad8}{dictName}[{keyExpr}] = arr;");
        code.AppendLine($"{pad4}}}");
        code.AppendLine($"{pad}}}");
    }
}
