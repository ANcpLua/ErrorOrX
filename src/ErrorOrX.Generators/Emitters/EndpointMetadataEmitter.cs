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

        // 2. Deprecation metadata from [Obsolete] attribute
        EmitDeprecationMetadata(code, in ep, indent);

        // 3. Accepts metadata (Content-Type for request body)
        EmitAcceptsMetadata(code, in ep, indent);

        // 4. Produces metadata (OpenAPI response types)
        EmitProducesMetadata(code, in ep, indent, maxArity);

        // 5. Middleware fluent calls
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
            .FirstOrDefault(static p => p.Source == ParameterSource.Body);

        if (bodyParam.Name is not null)
            code.AppendLine(
                $"{indent}.WithMetadata(new global::Microsoft.AspNetCore.Http.Metadata.AcceptsMetadata(new[] {{ \"{WellKnownTypes.Constants.ContentTypeJson}\" }}, typeof({bodyParam.TypeFqn})))");
        else if (ep.HasFormParams)
            code.AppendLine(
                $"{indent}.WithMetadata(new global::Microsoft.AspNetCore.Http.Metadata.AcceptsMetadata(new[] {{ \"{WellKnownTypes.Constants.ContentTypeFormData}\" }}, typeof(object)))");
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
            ep.Middleware,
            ep.HasParameterValidation);

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
                EmitProducesMetadataLine(code, indent, statusCode,
                    statusCode == 400
                        ? WellKnownTypes.Fqn.HttpValidationProblemDetails
                        : WellKnownTypes.Fqn.ProblemDetails,
                    WellKnownTypes.Constants.ContentTypeProblemJson);
        }
    }

    /// <summary>
    ///     Emits a single ProducesResponseTypeMetadata line.
    /// </summary>
    private static void EmitProducesMetadataLine(StringBuilder code, string indent, int statusCode, string? typeFqn,
        string contentType)
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
        {
            code.AppendLine($"{indent}.AllowAnonymous()");
        }
        else if (middleware.RequiresAuthorization)
        {
            var policies = middleware.AuthorizationPolicies.AsImmutableArray();
            if (policies.IsDefaultOrEmpty)
                code.AppendLine($"{indent}.RequireAuthorization()");
            else if (policies.Length == 1)
                code.AppendLine($"{indent}.RequireAuthorization(\"{policies[0]}\")");
            else
                code.AppendLine(
                    $"{indent}.RequireAuthorization({string.Join(", ", policies.Select(static p => $"\"{p}\""))})");
        }

        // Rate Limiting: [EnableRateLimiting("policy")] / [EnableRateLimiting] / [DisableRateLimiting]
        if (middleware.DisableRateLimiting)
            code.AppendLine($"{indent}.DisableRateLimiting()");
        else if (middleware.EnableRateLimiting)
            code.AppendLine(middleware.RateLimitingPolicy is not null
                ? $"{indent}.RequireRateLimiting(\"{middleware.RateLimitingPolicy}\")"
                : $"{indent}.RequireRateLimiting()");

        // Output Caching: [OutputCache] / [OutputCache(Duration = 60)] / [OutputCache(PolicyName = "x")]
        if (middleware.EnableOutputCache)
        {
            if (middleware.OutputCachePolicy is not null)
                code.AppendLine($"{indent}.CacheOutput(\"{middleware.OutputCachePolicy}\")");
            else if (middleware.OutputCacheDuration is { } duration)
                code.AppendLine(
                    $"{indent}.CacheOutput(p => p.Expire(global::System.TimeSpan.FromSeconds({duration})))");
            else
                code.AppendLine($"{indent}.CacheOutput()");
        }

        // CORS: [EnableCors("policy")] / [EnableCors] / [DisableCors]
        if (middleware.DisableCors)
            code.AppendLine($"{indent}.DisableCors()");
        else if (middleware.EnableCors)
            code.AppendLine(middleware.CorsPolicy is not null
                ? $"{indent}.RequireCors(\"{middleware.CorsPolicy}\")"
                : $"{indent}.RequireCors()");
    }

    /// <summary>
    ///     Emits deprecation metadata from [Obsolete] attribute.
    ///     Adds ObsoleteAttribute metadata to the endpoint for OpenAPI documentation.
    /// </summary>
    private static void EmitDeprecationMetadata(StringBuilder code, in EndpointDescriptor ep, string indent)
    {
        if (!ep.HasMetadata(MetadataKeys.Deprecated))
            return;

        var message = ep.GetMetadata(MetadataKeys.DeprecatedMessage);
        if (message is not null)
        {
            // Escape any quotes in the message
            var escapedMessage = message.Replace("\"", "\\\"");
            code.AppendLine($"{indent}.WithMetadata(new global::System.ObsoleteAttribute(\"{escapedMessage}\"))");
        }
        else
        {
            code.AppendLine($"{indent}.WithMetadata(new global::System.ObsoleteAttribute())");
        }
    }
}
