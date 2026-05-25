namespace ErrorOr.Generators;

/// <summary>
///     Represents a metadata entry for an endpoint.
/// </summary>
internal readonly record struct MetadataEntry(string Key, string Value);

/// <summary>
///     Classifies the success response type for HTTP status code mapping.
/// </summary>
internal enum SuccessKind
{
    Payload,
    Success,
    Created,
    Updated,
    Deleted
}

/// <summary>
///     Success response information for OpenAPI metadata.
/// </summary>
internal readonly record struct SuccessResponseInfo(
    string ResultTypeFqn,
    int StatusCode,
    bool HasBody,
    string Factory);

/// <summary>
///     Result of union type computation.
/// </summary>
internal readonly record struct UnionTypeResult(
    bool CanUseUnion,
    string ReturnTypeFqn,
    EquatableArray<int> ExplicitProduceCodes,
    bool UsesValidationProblemFor400 = false);

/// <summary>
///     Complete descriptor for an ErrorOr endpoint used for code generation.
/// </summary>
internal readonly record struct EndpointDescriptor(
    HttpVerb HttpVerb,
    string Pattern,
    string SuccessTypeFqn,
    SuccessKind SuccessKind,
    bool IsAsync,
    string HandlerContainingTypeFqn,
    string HandlerMethodName,
    EquatableArray<EndpointParameter> HandlerParameters,
    ErrorInferenceInfo ErrorInference,
    SseInfo Sse = default,
    bool IsAcceptedResponse = false,
    string? LocationIdPropertyName = null,
    MiddlewareInfo Middleware = default,
    VersioningInfo Versioning = default,
    RouteGroupInfo RouteGroup = default,
    EquatableArray<MetadataEntry> Metadata = default,
    string? CustomHttpMethod = null)
{
    /// <summary>Gets the HTTP method string for emission (e.g., "GET", "POST", or custom like "CONNECT").</summary>
    public string HttpMethod => CustomHttpMethod ?? HttpVerb.ToHttpString();

    /// <summary>
    ///     Returns true if any parameter is bound from body.
    /// </summary>
    public bool HasBodyParam
    {
        get
        {
            foreach (var p in HandlerParameters.AsImmutableArray())
                if (p.Source == ParameterSource.Body)
                    return true;

            return false;
        }
    }

    /// <summary>
    ///     Returns true if any parameter is bound from form-related sources.
    /// </summary>
    public bool HasFormParams
    {
        get
        {
            foreach (var p in HandlerParameters.AsImmutableArray())
                if (p.Source.IsFormRelated())
                    return true;

            return false;
        }
    }

    /// <summary>
    ///     Returns true if endpoint has body or form binding (for OpenAPI and validation).
    ///     Uses single-pass enumeration to avoid multiple iterations.
    /// </summary>
    public bool HasBodyOrFormBinding
    {
        get
        {
            foreach (var p in HandlerParameters.AsImmutableArray())
                if (p.Source == ParameterSource.Body || p.Source.IsFormRelated())
                    return true;

            return false;
        }
    }

    /// <summary>
    ///     Returns true if any parameter uses BindAsync custom binding.
    /// </summary>
    public bool HasBindAsyncParam
    {
        get
        {
            foreach (var p in HandlerParameters.AsImmutableArray())
                if (p.CustomBinding is CustomBindingMethod.BindAsync or CustomBindingMethod.BindAsyncWithParam)
                    return true;

            return false;
        }
    }

    /// <summary>
    ///     Returns true if any parameter requires DataAnnotations validation.
    /// </summary>
    public bool HasParameterValidation
    {
        get
        {
            foreach (var p in HandlerParameters.AsImmutableArray())
                if (p.RequiresValidation)
                    return true;

            return false;
        }
    }

    /// <summary>Gets metadata value by key, or null if not found.</summary>
    public string? GetMetadata(string key)
    {
        foreach (var entry in Metadata.AsImmutableArray())
            if (entry.Key == key)
                return entry.Value;

        return null;
    }

    /// <summary>Returns true if metadata with the given key exists.</summary>
    public bool HasMetadata(string key)
    {
        foreach (var entry in Metadata.AsImmutableArray())
            if (entry.Key == key)
                return true;

        return false;
    }
}

/// <summary>
///     Well-known metadata key constants.
/// </summary>
internal static class MetadataKeys
{
    public const string Deprecated = "erroror:deprecated";
    public const string DeprecatedMessage = "erroror:deprecated-message";
    public const string OpenApiExtension = "openapi:x-";
    public const string CustomTag = "openapi:tag";
}
