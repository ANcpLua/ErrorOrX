using ANcpLua.Roslyn.Utilities;

namespace ErrorOr.Generators;

/// <summary>
///     Collection variants to generate for discovered types.
///     Internal version for generator use - user sees the same enum emitted via PostInitializationOutput.
/// </summary>
[Flags]
internal enum CollectionKind
{
    None = 0,
    List = 1 << 0,
    Array = 1 << 1,
    Enumerable = 1 << 2,
    ReadOnlyList = 1 << 3,
    All = List | Array | Enumerable | ReadOnlyList
}

/// <summary>
///     JSON property naming policy options.
///     Internal version for generator use - user sees the same enum emitted via PostInitializationOutput.
/// </summary>
internal enum JsonNamingPolicy
{
    /// <summary>Use default naming (PascalCase).</summary>
    Default = 0,

    /// <summary>Use camelCase naming.</summary>
    CamelCase = 1,

    /// <summary>Use snake_case naming.</summary>
    SnakeCase = 2,

    /// <summary>Use kebab-case naming.</summary>
    KebabCase = 3
}

/// <summary>
///     Configuration for assembly-level [AotJsonAssembly] attribute.
///     Enables zero-config AOT JSON serialization for the entire assembly.
/// </summary>
internal readonly record struct AotJsonAssemblyConfig(
    string? ContextNamespace,
    string ContextTypeName,
    JsonNamingPolicy NamingPolicy,
    CollectionKind GenerateCollections,
    bool IncludeProblemDetails,
    bool TraversePropertyTypes,
    int MaxTraversalDepth);

/// <summary>
///     Extracted context information from [AotJson] decorated class.
///     All fields are primitives/strings for proper incremental caching.
/// </summary>
internal readonly record struct AotJsonContextInfo(
    string ContextTypeFqn,
    string ContextTypeName,
    string? Namespace,
    bool ScanEndpoints,
    EquatableArray<string> ScanNamespaces,
    EquatableArray<string> IncludeTypes,
    EquatableArray<string> ExcludeTypes,
    CollectionKind GenerateCollections,
    bool IncludeProblemDetails,
    bool TraversePropertyTypes = true,
    JsonNamingPolicy NamingPolicy = JsonNamingPolicy.CamelCase,
    int MaxTraversalDepth = 10);

/// <summary>
///     Discovered type information extracted from endpoint analysis.
///     Uses strings only - no ISymbol caching.
/// </summary>
internal readonly record struct DiscoveredTypeInfo(
    string FullyQualifiedName,
    string DisplayName,
    bool IsCollection,
    DiscoveredTypeSource Source);

/// <summary>
///     Source of how a type was discovered.
/// </summary>
internal enum DiscoveredTypeSource
{
    /// <summary>Type discovered from ErrorOr endpoint return type.</summary>
    EndpointReturn,

    /// <summary>Type discovered from endpoint method parameter.</summary>
    EndpointParameter,

    /// <summary>Type discovered from namespace scan.</summary>
    NamespaceScan,

    /// <summary>Type explicitly included via IncludeTypes.</summary>
    ExplicitInclude,

    /// <summary>Built-in type (ProblemDetails, etc.).</summary>
    BuiltIn,

    /// <summary>Type discovered from property traversal.</summary>
    PropertyTraversal
}

/// <summary>
///     Result of AotJson generation with all types to emit.
/// </summary>
internal readonly record struct AotJsonGenerationResult(
    AotJsonContextInfo Context,
    EquatableArray<DiscoveredTypeInfo> DiscoveredTypes);
