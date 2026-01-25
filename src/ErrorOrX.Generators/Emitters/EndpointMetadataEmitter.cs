using System.Text;

namespace ErrorOr.Generators.Emitters;

/// <summary>
///     Shared metadata emission logic for both grouped and ungrouped endpoints.
///     Ensures consistent AOT-compatible metadata emission across all endpoint types.
/// </summary>
internal static class EndpointMetadataEmitter
{
    /// <summary>
    ///     Emits all endpoint metadata: tags, accepts, produces, and middleware.
    ///     Call this after emitting WithName().
    /// </summary>
    /// <param name="code">StringBuilder to append to.</param>
    /// <param name="ep">The endpoint descriptor.</param>
    /// <param name="indent">Indentation string (e.g., "            " for ungrouped, "                " for grouped).</param>
    /// <param name="maxArity">Maximum arity for union type calculation.</param>
    public static void EmitEndpointMetadata(
        StringBuilder code,
        in EndpointDescriptor ep,
        string indent,
        int maxArity)
    {
        // Get tag name from endpoint identity
        var (tagName, _) = EndpointNameHelper.GetEndpointIdentity(ep.HandlerContainingTypeFqn, ep.HandlerMethodName);

        // 1. Tags for OpenAPI grouping
        code.AppendLine($"{indent}.WithTags(\"{tagName}\")");

        // 2. Accepts metadata (Content-Type for request body)
        EmitAcceptsMetadata(code, in ep, indent);

        // 3. Produces metadata (OpenAPI response types)
        EmitProducesMetadata(code, in ep, indent, maxArity);

        // 4. Middleware fluent calls
        var middleware = ep.Middleware;
        EmitMiddlewareCalls(code, in middleware, indent);
    }

    /// <summary>
    ///     Emits AcceptsMetadata for body or form parameters.
    ///     AOT-safe: Uses WithMetadata instead of .Accepts() which requires RouteHandlerBuilder.
    /// </summary>
    private static void EmitAcceptsMetadata(StringBuilder code, in EndpointDescriptor ep, string indent)
    {
        var bodyParam = ep.HandlerParameters.AsImmutableArray()
            .FirstOrDefault(static p => p.Source == EndpointParameterSource.Body);

        if (bodyParam.Name is not null)
        {
            code.AppendLine(
                $"{indent}.WithMetadata(new global::Microsoft.AspNetCore.Http.Metadata.AcceptsMetadata(new[] {{ \"{WellKnownTypes.Constants.ContentTypeJson}\" }}, typeof({bodyParam.TypeFqn})))");
        }
        else if (ep.HasFormParams)
        {
            code.AppendLine(
                $"{indent}.WithMetadata(new global::Microsoft.AspNetCore.Http.Metadata.AcceptsMetadata(new[] {{ \"{WellKnownTypes.Constants.ContentTypeFormData}\" }}, typeof(object)))");
        }
    }

    /// <summary>
    ///     Emits ProducesResponseTypeMetadata for OpenAPI documentation.
    ///     AOT-safe: Uses WithMetadata instead of .Produces().
    /// </summary>
    private static void EmitProducesMetadata(StringBuilder code, in EndpointDescriptor ep, string indent, int maxArity)
    {
        // SSE endpoints have special content type
        if (ep.IsSse)
        {
            code.AppendLine(
                $"{indent}.WithMetadata(new {WellKnownTypes.Fqn.ProducesResponseTypeMetadata}(200, null, new[] {{ \"text/event-stream\" }}))");
            return;
        }

        var successInfo = ResultsUnionTypeBuilder.GetSuccessResponseInfo(
            ep.SuccessTypeFqn,
            ep.SuccessKind,
            ep.IsAcceptedResponse);

        var hasBodyBinding = ep.HasBodyOrFormBinding;

        var unionResult = ResultsUnionTypeBuilder.ComputeReturnType(
            ep.SuccessTypeFqn,
            ep.SuccessKind,
            ep.InferredErrorTypeNames,
            ep.InferredCustomErrors,
            ep.DeclaredProducesErrors,
            hasBodyBinding,
            maxArity,
            ep.IsAcceptedResponse,
            ep.Middleware);

        // Only emit explicit produces metadata when we can't use union types
        // (Union types provide this metadata automatically)
        if (!unionResult.CanUseUnion)
        {
            // Success response
            EmitProducesMetadataLine(code, indent, successInfo.StatusCode,
                successInfo.HasBody ? ep.SuccessTypeFqn : null,
                WellKnownTypes.Constants.ContentTypeJson);

            // Error responses
            foreach (var statusCode in unionResult.ExplicitProduceCodes.AsImmutableArray().Distinct()
                         .OrderBy(static x => x))
            {
                EmitProducesMetadataLine(code, indent, statusCode,
                    statusCode == 400 ? WellKnownTypes.Fqn.HttpValidationProblemDetails : WellKnownTypes.Fqn.ProblemDetails,
                    WellKnownTypes.Constants.ContentTypeProblemJson);
            }
        }
    }

    /// <summary>
    ///     Emits a single ProducesResponseTypeMetadata line.
    /// </summary>
    private static void EmitProducesMetadataLine(StringBuilder code, string indent, int statusCode, string? typeFqn, string contentType)
    {
        code.AppendLine(typeFqn is not null
            ? $"{indent}.WithMetadata(new {WellKnownTypes.Fqn.ProducesResponseTypeMetadata}({statusCode}, typeof({typeFqn}), new[] {{ \"{contentType}\" }}))"
            : $"{indent}.WithMetadata(new {WellKnownTypes.Fqn.ProducesResponseTypeMetadata}({statusCode}))");
    }

    /// <summary>
    ///     Emits middleware fluent calls based on BCL attributes detected on the endpoint.
    /// </summary>
    private static void EmitMiddlewareCalls(StringBuilder code, in MiddlewareInfo middleware, string indent)
    {
        if (!middleware.HasAny)
            return;

        // Authorization: [Authorize] / [Authorize("Policy")] / [AllowAnonymous]
        if (middleware.AllowAnonymous)
            code.AppendLine($"{indent}.AllowAnonymous()");
        else if (middleware.RequiresAuthorization)
            code.AppendLine(middleware.AuthorizationPolicy is not null
                ? $"{indent}.RequireAuthorization(\"{middleware.AuthorizationPolicy}\")"
                : $"{indent}.RequireAuthorization()");

        // Rate Limiting: [EnableRateLimiting("policy")] / [DisableRateLimiting]
        if (middleware.DisableRateLimiting)
            code.AppendLine($"{indent}.DisableRateLimiting()");
        else if (middleware is { EnableRateLimiting: true, RateLimitingPolicy: not null })
            code.AppendLine($"{indent}.RequireRateLimiting(\"{middleware.RateLimitingPolicy}\")");

        // Output Caching: [OutputCache] / [OutputCache(Duration = 60)] / [OutputCache(PolicyName = "x")]
        if (middleware.EnableOutputCache)
        {
            if (middleware.OutputCachePolicy is not null)
                code.AppendLine($"{indent}.CacheOutput(\"{middleware.OutputCachePolicy}\")");
            else if (middleware.OutputCacheDuration is { } duration)
                code.AppendLine($"{indent}.CacheOutput(p => p.Expire(global::System.TimeSpan.FromSeconds({duration})))");
            else
                code.AppendLine($"{indent}.CacheOutput()");
        }

        // CORS: [EnableCors("policy")] / [DisableCors]
        if (middleware.DisableCors)
            code.AppendLine($"{indent}.DisableCors()");
        else if (middleware is { EnableCors: true, CorsPolicy: not null })
            code.AppendLine($"{indent}.RequireCors(\"{middleware.CorsPolicy}\")");
    }
}
