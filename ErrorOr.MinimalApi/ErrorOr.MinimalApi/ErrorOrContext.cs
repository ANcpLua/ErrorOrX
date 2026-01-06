using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;

namespace ErrorOr.MinimalApi;

/// <summary>
///     Compilation context for ErrorOr.Http generator.
///     Composes AspNetContext with ErrorOr-specific types.
/// </summary>
internal sealed class ErrorOrContext
{
    /// <summary>
    ///     Base ASP.NET Core context with common types.
    /// </summary>
    public AspNetContext AspNet { get; }

    // ErrorOr-specific types (generated attributes)
    public INamedTypeSymbol? ErrorOrEndpointAttribute { get; }
    public INamedTypeSymbol? GetAttribute { get; }
    public INamedTypeSymbol? PostAttribute { get; }
    public INamedTypeSymbol? PutAttribute { get; }
    public INamedTypeSymbol? DeleteAttribute { get; }
    public INamedTypeSymbol? PatchAttribute { get; }
    public INamedTypeSymbol? ProducesErrorAttribute { get; }
    public INamedTypeSymbol? AcceptedResponseAttribute { get; }

    // Additional ASP.NET types not in AspNetContext
    public INamedTypeSymbol? FromKeyedServicesAttribute { get; }
    public INamedTypeSymbol? AsParametersAttribute { get; }
    public INamedTypeSymbol? IFormFileCollection { get; }
    public INamedTypeSymbol? IFormCollection { get; }
    public INamedTypeSymbol? IBindableFromHttpContext { get; }
    public INamedTypeSymbol? ParameterInfo { get; }

    // System types
    public INamedTypeSymbol? ObsoleteAttribute { get; }

    public ErrorOrContext(Compilation compilation)
    {
        AspNet = new AspNetContext(compilation);

        // ErrorOr.Http generated attributes
        ErrorOrEndpointAttribute = compilation.GetTypeByMetadataName(WellKnownTypes.ErrorOrEndpointAttribute);
        GetAttribute = compilation.GetTypeByMetadataName(WellKnownTypes.GetAttribute);
        PostAttribute = compilation.GetTypeByMetadataName(WellKnownTypes.PostAttribute);
        PutAttribute = compilation.GetTypeByMetadataName(WellKnownTypes.PutAttribute);
        DeleteAttribute = compilation.GetTypeByMetadataName(WellKnownTypes.DeleteAttribute);
        PatchAttribute = compilation.GetTypeByMetadataName(WellKnownTypes.PatchAttribute);
        ProducesErrorAttribute = compilation.GetTypeByMetadataName(WellKnownTypes.ProducesErrorAttribute);
        AcceptedResponseAttribute = compilation.GetTypeByMetadataName(WellKnownTypes.AcceptedResponseAttribute);

        // Additional ASP.NET types
        FromKeyedServicesAttribute = compilation.GetTypeByMetadataName(WellKnownTypes.FromKeyedServicesAttribute);
        AsParametersAttribute = compilation.GetTypeByMetadataName(WellKnownTypes.AsParametersAttribute);
        IFormFileCollection = compilation.GetTypeByMetadataName(WellKnownTypes.IFormFileCollection);
        IFormCollection = compilation.GetTypeByMetadataName(WellKnownTypes.IFormCollection);
        IBindableFromHttpContext = compilation.GetTypeByMetadataName(WellKnownTypes.IBindableFromHttpContext);
        ParameterInfo = compilation.GetTypeByMetadataName(WellKnownTypes.ParameterInfo);

        // System types
        ObsoleteAttribute = compilation.GetTypeByMetadataName(WellKnownTypes.ObsoleteAttribute);
    }

    // Convenience accessors that delegate to AspNetContext
    public INamedTypeSymbol? FromBody => AspNet.FromBodyAttribute;
    public INamedTypeSymbol? FromServices => AspNet.FromServicesAttribute;
    public INamedTypeSymbol? FromRoute => AspNet.FromRouteAttribute;
    public INamedTypeSymbol? FromQuery => AspNet.FromQueryAttribute;
    public INamedTypeSymbol? FromHeader => AspNet.FromHeaderAttribute;
    public INamedTypeSymbol? FromForm => AspNet.FromFormAttribute;
    public INamedTypeSymbol? IFormFile => AspNet.IFormFile;
    public INamedTypeSymbol? HttpContext => AspNet.HttpContext;

    // Short aliases for backward compatibility with KnownSymbols
    public INamedTypeSymbol? FromKeyedServices => FromKeyedServicesAttribute;
    public INamedTypeSymbol? AsParameters => AsParametersAttribute;
    public INamedTypeSymbol? HttpContextSymbol => HttpContext;

    // Helper methods
    public bool IsFormFile(ITypeSymbol? type) => AspNet.IsFormFile(type);

    public bool IsFormFileCollection(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        if (IFormFileCollection is not null &&
            SymbolEqualityComparer.Default.Equals(type, IFormFileCollection))
            return true;

        // Check for IReadOnlyList<IFormFile>
        if (type is INamedTypeSymbol { IsGenericType: true } named)
        {
            var origin = named.ConstructedFrom.ToDisplayString();
            if (origin == "System.Collections.Generic.IReadOnlyList<T>" &&
                IsFormFile(named.TypeArguments[0]))
                return true;
        }

        return false;
    }

    public bool IsFormCollection(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        return IFormCollection is not null &&
               SymbolEqualityComparer.Default.Equals(type, IFormCollection);
    }

    public bool IsHttpContext(ITypeSymbol? type) => AspNet.IsHttpContextType(type);

    public bool HasFromKeyedServices(IParameterSymbol parameter) =>
        FromKeyedServicesAttribute is not null && parameter.HasAttribute(FromKeyedServicesAttribute);

    public bool HasAsParameters(IParameterSymbol parameter) =>
        AsParametersAttribute is not null && parameter.HasAttribute(AsParametersAttribute);
}
