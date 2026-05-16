using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ErrorOr.Generators;

/// <summary>
///     Generates OpenAPI transformers from XML documentation on ErrorOr endpoints.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed partial class OpenApiTransformerGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Attributes are emitted by ErrorOrEndpointGenerator (shared across both generators)
        var endpoints = CombineHttpMethodProviders(context);

        var typeMetadata = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is TypeDeclarationSyntax,
                static (ctx, ct) => ExtractTypeMetadata(ctx, ct))
            .WhereNotNull()
            .CollectAsEquatableArray();

        context.RegisterSourceOutput(
            endpoints.Combine(typeMetadata),
            static (spc, data) => Emit(spc, data.Left.AsImmutableArray(), data.Right.AsImmutableArray()));
    }

    private static IncrementalValueProvider<EquatableArray<OpenApiEndpointInfo>> CombineHttpMethodProviders(
        IncrementalGeneratorInitializationContext context)
    {
        return IncrementalProviderExtensions.CombineAll(
            CreateEndpointProvider(context, WellKnownTypes.GetAttribute),
            CreateEndpointProvider(context, WellKnownTypes.PostAttribute),
            CreateEndpointProvider(context, WellKnownTypes.PutAttribute),
            CreateEndpointProvider(context, WellKnownTypes.DeleteAttribute),
            CreateEndpointProvider(context, WellKnownTypes.PatchAttribute),
            CreateEndpointProvider(context, WellKnownTypes.HeadAttribute),
            CreateEndpointProvider(context, WellKnownTypes.OptionsAttribute),
            CreateEndpointProvider(context, WellKnownTypes.TraceAttribute),
            CreateEndpointProvider(context, WellKnownTypes.ErrorOrEndpointAttribute));
    }

    private static IncrementalValuesProvider<OpenApiEndpointInfo> CreateEndpointProvider(
        IncrementalGeneratorInitializationContext context,
        string attributeName)
    {
        return context.SyntaxProvider
            .ForAttributeWithMetadataName(
                attributeName,
                static (node, _) => node is MethodDeclarationSyntax,
                static (ctx, ct) => ExtractOpenApiMetadata(ctx, ct))
            .WhereNotNull();
    }

    private static string GetPattern(AttributeData attr)
    {
        if (attr.GetConstructorArgument<string>(0) is { } p &&
            !string.IsNullOrWhiteSpace(p))
        {
            return p;
        }

        return "/";
    }

    private static (string? httpMethod, string? pattern) GetBaseAttributeInfo(AttributeData attr)
    {
        var method = attr.GetConstructorArgument<string>(0);
        var pattern = attr.GetConstructorArgument<string>(1);

        return string.IsNullOrWhiteSpace(method)
            ? (null, null)
            : (method, string.IsNullOrWhiteSpace(pattern) ? "/" : pattern);
    }
}
