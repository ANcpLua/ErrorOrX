using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;

namespace ErrorOr.Generators;

/// <summary>
///     Provides symbol lookups for common ASP.NET Core types.
///     Used by ErrorOrContext for parameter binding analysis.
/// </summary>
internal sealed class AspNetContext
{
    public AspNetContext(Compilation compilation)
    {
        FromBodyAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromBodyAttribute);
        FromServicesAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromServicesAttribute);
        FromRouteAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromRouteAttribute);
        FromQueryAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromQueryAttribute);
        FromHeaderAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromHeaderAttribute);
        FromFormAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromFormAttribute);
        FormFile = compilation.GetBestTypeByMetadataName(WellKnownTypes.FormFile);
        HttpContext = compilation.GetBestTypeByMetadataName(WellKnownTypes.HttpContext);
    }

    public INamedTypeSymbol? FromBodyAttribute { get; }
    public INamedTypeSymbol? FromServicesAttribute { get; }
    public INamedTypeSymbol? FromRouteAttribute { get; }
    public INamedTypeSymbol? FromQueryAttribute { get; }
    public INamedTypeSymbol? FromHeaderAttribute { get; }
    public INamedTypeSymbol? FromFormAttribute { get; }
    public INamedTypeSymbol? FormFile { get; }
    public INamedTypeSymbol? HttpContext { get; }

    public bool IsFormFile(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        if (FormFile is not null && type.IsOrImplements(FormFile))
            return true;

        return type.Name == "IFormFile" &&
               type.ContainingNamespace.ToDisplayString() == "Microsoft.AspNetCore.Http";
    }

    public bool IsHttpContextType(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        if (HttpContext is not null && type.IsOrInheritsFrom(HttpContext))
            return true;

        return type.Name == "HttpContext" &&
               type.ContainingNamespace.ToDisplayString() == "Microsoft.AspNetCore.Http";
    }
}