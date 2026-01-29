using System.Text;

namespace ErrorOr.Generators.Emitters;

internal static class BindingCodeEmitter
{
    /// <summary>
    ///     Emits parameter binding code and returns whether BindFail helper is used.
    /// </summary>
    internal static bool EmitParameterBinding(StringBuilder code, in EndpointParameter param, string paramName,
        string bindFailFn)
    {
        return param.Source switch
        {
            var s when s == ParameterSource.Route => EmitRouteBinding(code, in param, paramName, bindFailFn),
            var s when s == ParameterSource.Query => EmitQueryBinding(code, in param, paramName, bindFailFn),
            var s when s == ParameterSource.Header => EmitHeaderBinding(code, in param, paramName, bindFailFn),
            var s when s == ParameterSource.Body => EmitBodyBinding(code, in param, paramName, bindFailFn),
            var s when s == ParameterSource.Service => EmitServiceBinding(code, in param, paramName),
            var s when s == ParameterSource.KeyedService => EmitKeyedServiceBinding(code, in param, paramName),
            var s when s == ParameterSource.HttpContext => EmitHttpContextBinding(code, paramName),
            var s when s == ParameterSource.CancellationToken => EmitCancellationTokenBinding(code, paramName),
            var s when s == ParameterSource.Stream => EmitStreamBinding(code, paramName),
            var s when s == ParameterSource.PipeReader => EmitPipeReaderBinding(code, paramName),
            var s when s == ParameterSource.FormFile => EmitFormFileBinding(code, in param, paramName, bindFailFn),
            var s when s == ParameterSource.FormFiles => EmitFormFilesBinding(code, paramName),
            var s when s == ParameterSource.FormCollection => EmitFormCollectionBinding(code, paramName),
            var s when s == ParameterSource.Form => EmitFormBinding(code, in param, paramName, bindFailFn),
            var s when s == ParameterSource.AsParameters => EmitAsParametersBinding(code, in param, paramName,
                bindFailFn),
            _ => false
        };
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

        code.AppendLine($"            var {paramName} = new {param.TypeFqn}({string.Join(", ", childVars)});");
        return usesBindFail;
    }

    internal static string BuildArgumentExpression(in EndpointParameter param, string paramName)
    {
        return param.Source switch
        {
            var s when s == ParameterSource.Body && !param.IsNullable => paramName + "!",
            var s when s == ParameterSource.Route && param is { IsNullable: false, IsNonNullableValueType: false } =>
                paramName + "!",
            var s when s == ParameterSource.Query && param is { IsNullable: false, IsNonNullableValueType: true } =>
                paramName + ".Value",
            var s when s == ParameterSource.Header && param is { IsNullable: false, IsNonNullableValueType: true } =>
                paramName + ".Value",
            var s when s == ParameterSource.Query && param is { IsNullable: false, IsNonNullableValueType: false } =>
                paramName + "!",
            var s when s == ParameterSource.Header && param is { IsNullable: false, IsNonNullableValueType: false } =>
                paramName + "!",
            _ => paramName
        };
    }

    internal static string GetTryParseExpression(string typeFqn, string rawName, string outputName,
        CustomBindingMethod customBinding = CustomBindingMethod.None)
    {
        if (customBinding is CustomBindingMethod.TryParse)
        {
            var baseType = typeFqn.TrimEnd('?');
            return $"{baseType}.TryParse({rawName}, out var {outputName})";
        }

        if (customBinding is CustomBindingMethod.TryParseWithFormat)
        {
            var baseType = typeFqn.TrimEnd('?');
            return
                $"{baseType}.TryParse({rawName}, global::System.Globalization.CultureInfo.InvariantCulture, out var {outputName})";
        }

        var normalized = typeFqn.Replace("global::", "").TrimEnd('?');
        return normalized switch
        {
            // Integer types - no IFormatProvider overload
            "System.Int32" or "int" => $"int.TryParse({rawName}, out var {outputName})",
            "System.Int64" or "long" => $"long.TryParse({rawName}, out var {outputName})",
            "System.Int16" or "short" => $"short.TryParse({rawName}, out var {outputName})",
            "System.Byte" or "byte" => $"byte.TryParse({rawName}, out var {outputName})",
            "System.SByte" or "sbyte" => $"sbyte.TryParse({rawName}, out var {outputName})",
            "System.UInt16" or "ushort" => $"ushort.TryParse({rawName}, out var {outputName})",
            "System.UInt32" or "uint" => $"uint.TryParse({rawName}, out var {outputName})",
            "System.UInt64" or "ulong" => $"ulong.TryParse({rawName}, out var {outputName})",
            "System.Int128" => $"global::System.Int128.TryParse({rawName}, out var {outputName})",
            "System.UInt128" => $"global::System.UInt128.TryParse({rawName}, out var {outputName})",

            // Other types without IFormatProvider overload
            "System.Boolean" or "bool" => $"bool.TryParse({rawName}, out var {outputName})",
            "System.Guid" => $"global::System.Guid.TryParse({rawName}, out var {outputName})",
            "System.Uri" =>
                $"global::System.Uri.TryCreate({rawName}, global::System.UriKind.RelativeOrAbsolute, out var {outputName})",

            // Culture-sensitive floating point types - use InvariantCulture
            "System.Decimal" or "decimal" =>
                $"decimal.TryParse({rawName}, global::System.Globalization.NumberStyles.Number, global::System.Globalization.CultureInfo.InvariantCulture, out var {outputName})",
            "System.Double" or "double" =>
                $"double.TryParse({rawName}, global::System.Globalization.NumberStyles.Float | global::System.Globalization.NumberStyles.AllowThousands, global::System.Globalization.CultureInfo.InvariantCulture, out var {outputName})",
            "System.Single" or "float" =>
                $"float.TryParse({rawName}, global::System.Globalization.NumberStyles.Float | global::System.Globalization.NumberStyles.AllowThousands, global::System.Globalization.CultureInfo.InvariantCulture, out var {outputName})",
            "System.Half" =>
                $"global::System.Half.TryParse({rawName}, global::System.Globalization.NumberStyles.Float, global::System.Globalization.CultureInfo.InvariantCulture, out var {outputName})",

            // Culture-sensitive date/time types - use InvariantCulture
            "System.DateTime" =>
                $"global::System.DateTime.TryParse({rawName}, global::System.Globalization.CultureInfo.InvariantCulture, global::System.Globalization.DateTimeStyles.RoundtripKind, out var {outputName})",
            "System.DateTimeOffset" =>
                $"global::System.DateTimeOffset.TryParse({rawName}, global::System.Globalization.CultureInfo.InvariantCulture, global::System.Globalization.DateTimeStyles.RoundtripKind, out var {outputName})",
            "System.DateOnly" =>
                $"global::System.DateOnly.TryParse({rawName}, global::System.Globalization.CultureInfo.InvariantCulture, global::System.Globalization.DateTimeStyles.None, out var {outputName})",
            "System.TimeOnly" =>
                $"global::System.TimeOnly.TryParse({rawName}, global::System.Globalization.CultureInfo.InvariantCulture, global::System.Globalization.DateTimeStyles.None, out var {outputName})",
            "System.TimeSpan" =>
                $"global::System.TimeSpan.TryParse({rawName}, global::System.Globalization.CultureInfo.InvariantCulture, out var {outputName})",

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
