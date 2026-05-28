namespace ErrorOr.Generators.Emitters;

/// <summary>
///     Cross-cutting helpers shared by every binding family:
///     <list type="bullet">
///         <item>
///             <see cref="BuildArgumentExpression" /> — composes the call-site expression respecting nullability and
///             value-vs-reference type rules.
///         </item>
///         <item><see cref="GetTryParseExpression" /> — table-routed BCL-aware <c>TryParse</c> invocation per type FQN.</item>
///         <item>
///             <see cref="EmitValidationDictBuilder" /> — emits the <c>Dictionary&lt;string, string[]&gt;</c>
///             aggregation pattern used by both DataAnnotations and ErrorOr.Validation paths.
///         </item>
///     </list>
/// </summary>
internal static partial class BindingCodeEmitter
{
    internal static string BuildArgumentExpression(in EndpointParameter param, string paramName)
    {
        var source = param.Source;

        if (source == ParameterSource.Body && !param.IsNullable)
            return paramName + "!";

        if (source == ParameterSource.Route && param is { IsNullable: false, IsNonNullableValueType: false })
            return paramName + "!";

        if (source is ParameterSource.Query or ParameterSource.Header
            && param is { IsNullable: false, IsNonNullableValueType: true })
        {
            return paramName + ".Value";
        }

        if (source is ParameterSource.Query or ParameterSource.Header
            && param is { IsNullable: false, IsNonNullableValueType: false })
        {
            return paramName + "!";
        }

        return paramName;
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

        if (filterExpr is not null) code.AppendLine($"{pad4}if ({filterExpr}) continue;");

        if (keyVarDecl is not null) code.AppendLine($"{pad4}{keyVarDecl}");

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
