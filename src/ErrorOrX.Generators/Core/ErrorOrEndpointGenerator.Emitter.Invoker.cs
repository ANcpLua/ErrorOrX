using ErrorOr.Generators.Emitters;

namespace ErrorOr.Generators;

/// <summary>
///     Per-endpoint invoker emission. Generates the two-method AOT-safe pattern:
///     <list type="number">
///         <item><c>Invoke_Ep{N}</c> — typed-return wrapper for OpenAPI visibility.</item>
///         <item><c>Invoke_Ep{N}_Core</c> — body emission with binding, validation, dispatch.</item>
///     </list>
///     Also emits the bind-failure helpers (<c>BindFail</c>, <c>BindFail415</c>) and the
///     DataAnnotations validation block before the handler call.
/// </summary>
public sealed partial class ErrorOrEndpointGenerator
{
    private static void EmitInvoker(StringBuilder code, in EndpointDescriptor ep, int index, int maxArity)
    {
        var ctx = ComputeInvokerContext(in ep, index, maxArity);
        var (bodyCode, usesBindFail) = EmitBodyCode(in ep, in ctx);

        EmitWrapperMethod(code, in ctx);
        EmitCoreMethod(code, bodyCode, in ctx, usesBindFail);
    }

    private static InvokerContext ComputeInvokerContext(
        in EndpointDescriptor ep,
        int index,
        int maxArity)
    {
        var successInfo = ResultsUnionTypeBuilder.GetSuccessResponseInfo(
            ep.SuccessTypeFqn, ep.SuccessKind, ep.IsAcceptedResponse);

        var hasFormBinding = ep.HasFormParams;
        var hasBodyBinding = ep.HasBodyOrFormBinding;

        var unionResult = ResultsUnionTypeBuilder.ComputeReturnType(
            ep.SuccessTypeFqn, ep.SuccessKind,
            ep.ErrorInference.InferredErrorTypeNames, ep.ErrorInference.InferredCustomErrors,
            ep.ErrorInference.DeclaredProducesErrors, hasBodyBinding, maxArity,
            ep.IsAcceptedResponse, ep.Middleware, ep.HasParameterValidation);

        var needsAwait = ep.IsAsync || hasBodyBinding || ep.HasBindAsyncParam;

        return new InvokerContext(successInfo, unionResult, hasFormBinding, hasBodyBinding, needsAwait, index);
    }

    private static (StringBuilder Code, bool UsesBindFail) EmitBodyCode(
        in EndpointDescriptor ep,
        in InvokerContext ctx)
    {
        var bodyCode = new StringBuilder();
        var usesBindFail = ctx.HasFormBinding;

        if (ctx.HasFormBinding) EmitFormContentTypeGuard(bodyCode);

        var args = new StringBuilder();
        var validationParams = new List<(int Index, string ParamName)>();
        for (var i = 0; i < ep.HandlerParameters.Length; i++)
        {
            var param = ep.HandlerParameters[i];
            usesBindFail |= BindingCodeEmitter.EmitParameterBinding(bodyCode, in param, $"p{i}", "BindFail");
            if (i > 0) args.Append(", ");

            args.Append(BindingCodeEmitter.BuildArgumentExpression(in param, $"p{i}"));

            if (param.RequiresValidation) validationParams.Add((i, $"p{i}"));
        }

        if (validationParams.Count > 0)
            EmitBclValidation(bodyCode, validationParams, ctx.UnionResult.ReturnTypeFqn, ctx.NeedsAwait);

        var awaitKeyword = ep.IsAsync ? "await " : "";
        bodyCode.Append("            var result = ").Append(awaitKeyword).Append(ep.HandlerContainingTypeFqn)
            .Append('.').Append(ep.HandlerMethodName).Append('(').Append(args).AppendLine(");");

        EmitErrorHandling(bodyCode, in ep, in ctx);

        return (bodyCode, usesBindFail);
    }

    private static void EmitErrorHandling(
        StringBuilder bodyCode,
        in EndpointDescriptor ep,
        in InvokerContext ctx)
    {
        if (ep.Sse.IsSse)
        {
            bodyCode.AppendLine(
                $"            if (result.IsError) return {ctx.WrapReturn("ToProblem(result.Errors)")};");
            bodyCode.AppendLine(
                $"            return {ctx.WrapReturn($"{WellKnownTypes.Fqn.TypedResults.ServerSentEvents}(result.Value)")};");
        }
        else if (ctx.UnionResult.CanUseUnion)
        {
            EmitUnionTypeErrorHandling(bodyCode, in ep, in ctx);
        }
        else
        {
            // Use minimal interface (IsError/Errors/Value) instead of convenience Match API
            var successFactory = GetSuccessFactoryWithLocation(in ep, ctx.SuccessInfo);
            bodyCode.AppendLine(
                $"            if (result.IsError) return {ctx.WrapReturn("ToProblem(result.Errors)")};");
            bodyCode.AppendLine($"            return {ctx.WrapReturn(successFactory)};");
        }
    }

    private static void EmitWrapperMethod(StringBuilder code, in InvokerContext ctx)
    {
        var returnType = ctx.UnionResult.ReturnTypeFqn;
        code.AppendLine($"        private static async Task<{returnType}> {ctx.WrapperName}(HttpContext ctx)");
        code.AppendLine("        {");
        code.AppendLine($"            return await {ctx.CoreName}(ctx);");
        code.AppendLine("        }");
        code.AppendLine();
    }

    private static void EmitCoreMethod(
        StringBuilder code,
        StringBuilder bodyCode,
        in InvokerContext ctx,
        bool usesBindFail)
    {
        var returnType = ctx.UnionResult.ReturnTypeFqn;
        code.AppendLine(
            ctx.NeedsAwait
                ? $"        private static async Task<{returnType}> {ctx.CoreName}(HttpContext ctx)"
                : $"        private static Task<{returnType}> {ctx.CoreName}(HttpContext ctx)");

        code.AppendLine("        {");

        if (usesBindFail)
            EmitBindFailHelper(code, returnType, ctx.NeedsAwait, ctx.UnionResult.UsesValidationProblemFor400);

        if (ctx.HasBodyBinding) EmitBindFail415Helper(code, returnType, ctx.NeedsAwait);

        code.Append(bodyCode);
        code.AppendLine("        }");
        code.AppendLine();
    }

    /// <summary>
    ///     Emits BCL validation calls for parameters that have ValidationAttribute or implement IValidatableObject.
    ///     Uses System.ComponentModel.DataAnnotations.Validator.TryValidateObject for validation.
    /// </summary>
    private static void EmitBclValidation(StringBuilder code, List<(int Index, string ParamName)> validationParams,
        string returnTypeFqn, bool isAsync)
    {
        code.AppendLine();
        code.AppendLine("            // BCL Validation");

        foreach (var (_, paramName) in validationParams)
        {
            code.AppendLine(
                $"            var {paramName}ValidationResults = new {WellKnownTypes.Fqn.List}<{WellKnownTypes.Fqn.ValidationResult}>();");
            code.AppendLine(
                $"            if (!{WellKnownTypes.Fqn.Validator}.TryValidateObject({paramName}!, new {WellKnownTypes.Fqn.ValidationContext}({paramName}!), {paramName}ValidationResults, validateAllProperties: true))");
            code.AppendLine("            {");
            BindingCodeEmitter.EmitValidationDictBuilder(
                code, 16, "validationDict", $"{paramName}ValidationResults", "vr",
                "key", "vr.ErrorMessage ?? \"\"",
                keyVarDecl: "var key = vr.MemberNames.FirstOrDefault() ?? \"\";");

            var returnExpr = isAsync
                ? $"{WellKnownTypes.Fqn.TypedResults.ValidationProblem}(validationDict)"
                : $"Task.FromResult<{returnTypeFqn}>({WellKnownTypes.Fqn.TypedResults.ValidationProblem}(validationDict))";
            code.AppendLine($"                return {returnExpr};");
            code.AppendLine("            }");
        }

        code.AppendLine();
    }

    private static void EmitBindFailHelper(StringBuilder code, string returnTypeFqn, bool isAsync,
        bool useValidationProblem)
    {
        var returnType = isAsync ? returnTypeFqn : $"Task<{returnTypeFqn}>";

        if (useValidationProblem)
        {
            // Use ValidationProblem to match the Results<..., ValidationProblem, ...> union type
            const string ValidationProblemExpr =
                $"{WellKnownTypes.Fqn.TypedResults.ValidationProblem}(new {WellKnownTypes.Fqn.Dictionary}<string, string[]> {{ [param] = [reason] }})";
            var returnExpr =
                isAsync ? ValidationProblemExpr : $"Task.FromResult<{returnTypeFqn}>({ValidationProblemExpr})";

            code.AppendLine($"            static {returnType} BindFail(string param, string reason)");
            code.AppendLine($"                => {returnExpr};");
        }
        else
        {
            // Use BadRequest<ProblemDetails> to match the Results<..., BadRequest<ProblemDetails>, ...> union type
            code.AppendLine(
                $"            static {WellKnownTypes.Fqn.ProblemDetails} CreateBindProblem(string param, string reason) => new()");
            code.AppendLine("            {");
            code.AppendLine("                Title = \"Bad Request\",");
            code.AppendLine("                Detail = $\"Parameter '{param}' {reason}.\",");
            code.AppendLine("                Status = 400,");
            code.AppendLine($"                Type = \"{WellKnownTypes.Constants.HttpStatusesBaseUrl}400\",");
            code.AppendLine("            };");
            code.AppendLine();

            const string BadRequestExpr =
                $"{WellKnownTypes.Fqn.TypedResults.BadRequest}(CreateBindProblem(param, reason))";
            var returnExpr = isAsync ? BadRequestExpr : $"Task.FromResult<{returnTypeFqn}>({BadRequestExpr})";

            code.AppendLine($"            static {returnType} BindFail(string param, string reason)");
            code.AppendLine($"                => {returnExpr};");
        }

        code.AppendLine();
    }

    private static void EmitBindFail415Helper(StringBuilder code, string returnTypeFqn, bool isAsync)
    {
        const string Expr = $"{WellKnownTypes.Fqn.TypedResults.StatusCode}(415)";
        var returnExpr = isAsync ? Expr : $"Task.FromResult<{returnTypeFqn}>({Expr})";
        var returnType = isAsync ? returnTypeFqn : $"Task<{returnTypeFqn}>";

        code.AppendLine($"            static {returnType} BindFail415()");
        code.AppendLine($"                => {returnExpr};");
        code.AppendLine();
    }

    private static void EmitFormContentTypeGuard(StringBuilder code)
    {
        code.AppendLine(
            "            if (!ctx.Request.HasFormContentType) return BindFail415();");
        code.AppendLine("            var form = await ctx.Request.ReadFormAsync(ctx.RequestAborted);");
        code.AppendLine();
    }

    /// <summary>
    ///     Context for invoker emission, holding precomputed values and providing helper methods.
    /// </summary>
    private readonly record struct InvokerContext(
        SuccessResponseInfo SuccessInfo,
        UnionTypeResult UnionResult,
        bool HasFormBinding,
        bool HasBodyBinding,
        bool NeedsAwait,
        int Index)
    {
        public string WrapperName => $"Invoke_Ep{Index}";
        public string CoreName => $"Invoke_Ep{Index}_Core";

        public string WrapReturn(string expr)
        {
            return NeedsAwait ? expr : $"Task.FromResult<{UnionResult.ReturnTypeFqn}>({expr})";
        }
    }
}
