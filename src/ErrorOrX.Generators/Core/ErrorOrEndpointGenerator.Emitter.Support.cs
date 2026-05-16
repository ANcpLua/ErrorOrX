using ErrorOr.Generators.Emitters;

namespace ErrorOr.Generators;

public sealed partial class ErrorOrEndpointGenerator
{
    private static void EmitSupportMethods(StringBuilder code)
    {
        code.AppendLine(
            "        private static bool TryGetRouteValue(HttpContext ctx, string name, out string? value)");
        code.AppendLine("        {");
        code.AppendLine(
            "            if (!ctx.Request.RouteValues.TryGetValue(name, out var raw) || raw is null) { value = null; return false; }");
        code.AppendLine("            value = raw.ToString(); return value is not null;");
        code.AppendLine("        }");
        code.AppendLine();
        code.AppendLine(
            "        private static bool TryGetQueryValue(HttpContext ctx, string name, out string? value)");
        code.AppendLine("        {");
        code.AppendLine(
            "            if (!ctx.Request.Query.TryGetValue(name, out var raw) || raw.Count is 0) { value = null; return false; }");
        code.AppendLine("            value = raw.ToString(); return value is not null;");
        code.AppendLine("        }");
        code.AppendLine();
        code.AppendLine(
            $"        private static {WellKnownTypes.Fqn.Result} ToProblem({WellKnownTypes.Fqn.ReadOnlyList}<{WellKnownTypes.Fqn.Error}> errors)");
        code.AppendLine("        {");
        code.AppendLine($"            if (errors.Count is 0) return {WellKnownTypes.Fqn.TypedResults.Problem}();");
        code.AppendLine("            var hasValidation = false;");
        code.AppendLine(
            $"            for (var i = 0; i < errors.Count; i++) if (errors[i].Type == {WellKnownTypes.Fqn.ErrorType}.Validation) {{ hasValidation = true; break; }}");
        code.AppendLine("            if (hasValidation)");
        code.AppendLine("            {");
        BindingCodeEmitter.EmitValidationDictBuilder(
            code, 16, "dict", "errors", "e",
            "e.Code", "e.Description",
            $"e.Type != {WellKnownTypes.Fqn.ErrorType}.Validation");
        code.AppendLine($"                return {WellKnownTypes.Fqn.TypedResults.ValidationProblem}(dict);");
        code.AppendLine("            }");
        code.AppendLine("            var first = errors[0];");
        code.AppendLine($"            var problem = new {WellKnownTypes.Fqn.ProblemDetails}");
        code.AppendLine("            {");
        code.AppendLine("                Title = first.Code,");
        code.AppendLine("                Detail = first.Description,");
        code.AppendLine(
            $"                Status = first.Type switch {{ {ErrorMapping.GenerateStatusSwitch(WellKnownTypes.Fqn.ErrorType)} }}");
        code.AppendLine("            };");
        code.AppendLine("            problem.Type = $\"https://httpstatuses.io/{problem.Status}\";");
        code.AppendLine("            return problem.Status switch");
        code.AppendLine("            {");
        foreach (var caseExpr in ErrorMapping.GenerateStatusToFactoryCases())
            code.AppendLine($"                {caseExpr},");
        code.AppendLine($"                _ => {ErrorMapping.GetDefaultProblemFactory()}");
        code.AppendLine("            };");
        code.AppendLine("        }");
    }

    private static ImmutableArray<EndpointDescriptor> SortEndpoints(ImmutableArray<EndpointDescriptor> endpoints)
    {
        return
        [
            .. endpoints
                .OrderBy(static e => e.HttpVerb)
                .ThenBy(static e => e.Pattern, StringComparer.Ordinal)
                .ThenBy(static e => e.HandlerMethodName, StringComparer.Ordinal)
        ];
    }

    private static List<string> CollectJsonTypes(ImmutableArray<EndpointDescriptor> endpoints)
    {
        var types = new HashSet<string>(StringComparer.Ordinal);
        foreach (var ep in endpoints)
        {
            foreach (var p in ep.HandlerParameters)
            {
                if (p.Source == ParameterSource.Body)
                    types.Add(p.TypeFqn);
            }

            if (ep.Sse is { IsSse: true, SseItemTypeFqn: not null })
            {
                types.Add(ep.Sse.SseItemTypeFqn);
            }
            else
            {
                var successInfo = ResultsUnionTypeBuilder.GetSuccessResponseInfo(
                    ep.SuccessTypeFqn,
                    ep.SuccessKind,
                    ep.IsAcceptedResponse);
                if (successInfo.HasBody) types.Add(ep.SuccessTypeFqn);
            }
        }

        var sorted = types.ToList();
        sorted.Sort(StringComparer.Ordinal);
        return sorted;
    }

}
