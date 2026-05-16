namespace ErrorOr.Generators;

/// <summary>
///     Information about a user-defined JsonSerializerContext.
/// </summary>
internal readonly record struct JsonContextInfo(
    string ClassName,
    string? Namespace,
    EquatableArray<string> SerializableTypes,
    bool HasCamelCasePolicy);

/// <summary>
///     Represents a parameter for OpenAPI documentation.
/// </summary>
internal readonly record struct OpenApiParameterInfo(
    string Name,
    string Location,
    bool Required,
    string SchemaType,
    string? SchemaFormat);

/// <summary>
///     Immutable endpoint info for OpenAPI generation.
/// </summary>
internal readonly record struct OpenApiEndpointInfo(
    string OperationId,
    string TagName,
    string? Summary,
    string? Description,
    string HttpMethod,
    string Pattern,
    EquatableArray<(string ParamName, string Description)> ParameterDocs,
    EquatableArray<OpenApiParameterInfo> Parameters);

/// <summary>
///     Immutable type metadata for schema generation.
/// </summary>
internal readonly record struct TypeMetadataInfo(
    string TypeKey,
    string Description);
