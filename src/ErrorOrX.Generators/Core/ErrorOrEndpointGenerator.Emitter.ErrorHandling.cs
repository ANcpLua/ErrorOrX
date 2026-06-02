using ErrorOr.Generators.Emitters;

namespace ErrorOr.Generators;

/// <summary>
///     Emits the error-to-result dispatch inside <c>Invoke_Ep{N}_Core</c>:
///     <list type="bullet">
///         <item>Union-type path that switches on <see cref="ErrorMapping" /> ErrorType-to-Result factories.</item>
///         <item>Validation handling (DataAnnotations + Error.Validation aggregation).</item>
///         <item>ProblemDetails construction and Location-header emission for Created+Id responses.</item>
///     </list>
/// </summary>
public sealed partial class ErrorOrEndpointGenerator
{
    private static void EmitUnionTypeErrorHandling(
        StringBuilder code,
        in EndpointDescriptor ep,
        in InvokerContext ctx)
    {
        var handlerFqn = $"{ep.HandlerContainingTypeFqn}.{ep.HandlerMethodName}";
        code.AppendLine("            if (result.IsError)");
        code.AppendLine("            {");
        code.AppendLine("                if (result.Errors.Count is 0)");
        code.AppendLine("                    throw new global::System.InvalidOperationException(");
        code.AppendLine($"                        \"ErrorOrX: handler '{handlerFqn}' returned an ErrorOr<T> with IsError=true but Errors.Count=0. \" +");
        code.AppendLine("                        \"This is an invariant violation in the ErrorOr<T> instance — likely a default(ErrorOr<T>) leak or a custom constructor bug. \" +");
        code.AppendLine("                        \"Fix the handler to return a fully-constructed Error, or use the Error/T implicit converters.\");");
        code.AppendLine("                var first = result.Errors[0];");

        EmitValidationHandling(code, in ep, in ctx);
        EmitProblemDetailsBuilding(code);
        EmitErrorTypeSwitch(code, in ep, in ctx);

        code.AppendLine("            }");
        code.AppendLine();

        var successFactory = GetSuccessFactoryWithLocation(in ep, ctx.SuccessInfo);
        code.AppendLine($"            return {ctx.WrapReturn(successFactory)};");
    }

    private static string GetSuccessFactoryWithLocation(in EndpointDescriptor ep, SuccessResponseInfo successInfo)
    {
        // POST + Created(201) + body with Id property → emit Location header
        if (ep.HttpVerb == HttpVerb.Post
            && successInfo is { StatusCode: 201, HasBody: true }
            && ep.LocationIdPropertyName is { Length: > 0 } idProp)
        {
            return
                $"{WellKnownTypes.Fqn.TypedResults.Created}($\"{{ctx.Request.Path}}/{{result.Value.{idProp}}}\", result.Value)";
        }

        return successInfo.Factory;
    }

    private static void EmitValidationHandling(StringBuilder code, in EndpointDescriptor ep,
        in InvokerContext ctx)
    {
        var hasValidation = !ep.ErrorInference.InferredErrorTypeNames.IsDefaultOrEmpty &&
                            ep.ErrorInference.InferredErrorTypeNames.AsImmutableArray()
                                .Contains(ErrorMapping.Validation);

        if (!hasValidation) return;

        code.AppendLine($"                if (first.Type == {WellKnownTypes.Fqn.ErrorType}.Validation)");
        code.AppendLine("                {");
        BindingCodeEmitter.EmitValidationDictBuilder(
            code, 20, "validationDict", "result.Errors", "e",
            "e.Code", "e.Description",
            $"e.Type != {WellKnownTypes.Fqn.ErrorType}.Validation");
        code.AppendLine(
            $"                    return {ctx.WrapReturn($"{WellKnownTypes.Fqn.TypedResults.ValidationProblem}(validationDict)")};");
        code.AppendLine("                }");
    }

    private static void EmitProblemDetailsBuilding(StringBuilder code)
    {
        code.AppendLine($"                var problem = new {WellKnownTypes.Fqn.ProblemDetails}");
        code.AppendLine("                {");
        code.AppendLine("                    Title = first.Code,");
        code.AppendLine("                    Detail = first.Description,");
        code.AppendLine(
            $"                    Status = first.Type switch {{ {ErrorMapping.GenerateStatusSwitch(WellKnownTypes.Fqn.ErrorType)} }}");
        code.AppendLine("                };");
        code.AppendLine("                problem.Type = $\"https://httpstatuses.io/{problem.Status}\";");
        code.AppendLine("                ApplyProblemMetadata(problem, first);");
        code.AppendLine();
    }

    private static void EmitErrorTypeSwitch(StringBuilder code, in EndpointDescriptor ep,
        in InvokerContext ctx)
    {
        var handlerFqn = $"{ep.HandlerContainingTypeFqn}.{ep.HandlerMethodName}";
        code.AppendLine("                switch (first.Type)");
        code.AppendLine("                {");

        if (!ep.ErrorInference.InferredErrorTypeNames.IsDefaultOrEmpty)
        {
            foreach (var errorTypeName in ep.ErrorInference.InferredErrorTypeNames.AsImmutableArray()
                         .Where(static e => e != ErrorMapping.Validation)
                         .Distinct()
                         .OrderBy(static x => x, StringComparer.Ordinal))
            {
                var factory = ErrorMapping.GetFactory(errorTypeName);

                code.AppendLine($"                    case {WellKnownTypes.Fqn.ErrorType}.{errorTypeName}:");
                code.AppendLine($"                        return {ctx.WrapReturn(factory)};");
            }
        }

        // No silent default → 500 Failure fallback. If a handler returns an Error.Type
        // the generator-time analyzer didn't infer (dead-code branch, dynamic dispatch,
        // unwalkable method body), throw so the gap surfaces in logs with the specific
        // Error.Type + handler identity — instead of being papered over with a generic 500.
        code.AppendLine("                    default:");
        code.AppendLine("                        throw new global::System.InvalidOperationException(");
        code.AppendLine($"                            $\"ErrorOrX: handler '{handlerFqn}' returned Error.Type={{first.Type}} (Code='{{first.Code}}') which was not inferred at generation time. \" +");
        code.AppendLine("                            \"Add [ReturnsError(ErrorType.\" + first.Type + \", \\\"\" + first.Code + \"\\\")] to the handler or its interface method, \" +");
        code.AppendLine("                            \"or remove the unreachable Error.Type from the handler. Generation-time inference cannot see this code path.\");");
        code.AppendLine("                }");
    }
}
