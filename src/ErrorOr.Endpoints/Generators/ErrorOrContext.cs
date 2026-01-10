using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;

// ReSharper disable InconsistentNaming

namespace ErrorOr.Endpoints.Generators;

/// <summary>
///     Compilation context for ErrorOr.Endpoints generator.
///     Composes AspNetContext with ErrorOr-specific types.
/// </summary>
internal sealed class ErrorOrContext
{
    public ErrorOrContext(Compilation compilation)
    {
        AspNet = new AspNetContext(compilation);

        // ErrorOr.Endpoints generated attributes
        ErrorOrEndpointAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.ErrorOrEndpointAttribute);
        GetAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.GetAttribute);
        PostAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.PostAttribute);
        PutAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.PutAttribute);
        DeleteAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.DeleteAttribute);
        PatchAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.PatchAttribute);
        ProducesErrorAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.ProducesErrorAttribute);
        AcceptedResponseAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.AcceptedResponseAttribute);
        ReturnsErrorAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.ReturnsErrorAttribute);
        ErrorOrOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.ErrorOrT)?.ConstructedFrom;
        ErrorType = compilation.GetBestTypeByMetadataName(WellKnownTypes.ErrorType);
        Error = compilation.GetBestTypeByMetadataName(WellKnownTypes.Error);

        // Additional ASP.NET types
        FromKeyedServicesAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromKeyedServicesAttribute);
        AsParametersAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.AsParametersAttribute);
        FormFileCollection = compilation.GetBestTypeByMetadataName(WellKnownTypes.FormFileCollection);
        FormCollection = compilation.GetBestTypeByMetadataName(WellKnownTypes.FormCollection);
        BindableFromHttpContext = compilation.GetBestTypeByMetadataName(WellKnownTypes.BindableFromHttpContext);
        ParameterInfo = compilation.GetBestTypeByMetadataName(WellKnownTypes.ParameterInfo);
        TypedResults = compilation.GetBestTypeByMetadataName(WellKnownTypes.TypedResults);
        SseItemOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.SseItemT)?.ConstructedFrom;

        // System types
        ObsoleteAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.ObsoleteAttribute);
        CancellationToken = compilation.GetBestTypeByMetadataName(WellKnownTypes.CancellationToken);
        NullableOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.NullableT)?.ConstructedFrom;

        // Result markers
        SuccessMarker = compilation.GetBestTypeByMetadataName(WellKnownTypes.Success);
        CreatedMarker = compilation.GetBestTypeByMetadataName(WellKnownTypes.Created);
        UpdatedMarker = compilation.GetBestTypeByMetadataName(WellKnownTypes.Updated);
        DeletedMarker = compilation.GetBestTypeByMetadataName(WellKnownTypes.Deleted);

        // Task types
        TaskOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.TaskT)?.ConstructedFrom;
        ValueTaskOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.ValueTaskT)?.ConstructedFrom;

        // Collection types
        ListOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.ListT)?.ConstructedFrom;
        IListOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.IListT)?.ConstructedFrom;
        IEnumerableOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.IEnumerableT)?.ConstructedFrom;
        IAsyncEnumerableOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.AsyncEnumerableT)
            ?.ConstructedFrom;
        IReadOnlyListOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.IReadOnlyListT)
            ?.ConstructedFrom;
        ICollectionOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.ICollectionT)?.ConstructedFrom;
        HashSetOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.HashSetT)?.ConstructedFrom;

        // Primitive well-known types
        Guid = compilation.GetBestTypeByMetadataName(WellKnownTypes.Guid);
        DateTime = compilation.GetBestTypeByMetadataName(WellKnownTypes.DateTime);
        DateTimeOffset = compilation.GetBestTypeByMetadataName(WellKnownTypes.DateTimeOffset);
        DateOnly = compilation.GetBestTypeByMetadataName(WellKnownTypes.DateOnly);
        TimeOnly = compilation.GetBestTypeByMetadataName(WellKnownTypes.TimeOnly);
        TimeSpan = compilation.GetBestTypeByMetadataName(WellKnownTypes.TimeSpan);

        // Misc
        ReadOnlySpanOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.ReadOnlySpanT)?.ConstructedFrom;
        IFormatProvider = compilation.GetBestTypeByMetadataName(WellKnownTypes.IFormatProvider);
        Stream = compilation.GetBestTypeByMetadataName(WellKnownTypes.Stream);
        PipeReader = compilation.GetBestTypeByMetadataName(WellKnownTypes.PipeReader);

        TypedResultsProblem = Pick(TypedResults, "Problem", 1);
        TypedResultsValidationProblem = Pick(TypedResults, "ValidationProblem", 1);
        TypedResultsUnauthorized = Pick(TypedResults, "Unauthorized", 0);
        TypedResultsForbid = Pick(TypedResults, "Forbid", 0);
    }

    /// <summary>
    ///     Base ASP.NET Core context with common types.
    /// </summary>
    public AspNetContext AspNet { get; }

    // Result markers
    public INamedTypeSymbol? SuccessMarker { get; }
    public INamedTypeSymbol? CreatedMarker { get; }
    public INamedTypeSymbol? UpdatedMarker { get; }
    public INamedTypeSymbol? DeletedMarker { get; }

    // Task types
    public INamedTypeSymbol? TaskOfT { get; }
    public INamedTypeSymbol? ValueTaskOfT { get; }

    // Collection types
    public INamedTypeSymbol? ListOfT { get; }
    public INamedTypeSymbol? IListOfT { get; }
    public INamedTypeSymbol? IEnumerableOfT { get; }
    public INamedTypeSymbol? IAsyncEnumerableOfT { get; }
    public INamedTypeSymbol? IReadOnlyListOfT { get; }
    public INamedTypeSymbol? ICollectionOfT { get; }
    public INamedTypeSymbol? HashSetOfT { get; }
    public INamedTypeSymbol? SseItemOfT { get; }
    public INamedTypeSymbol? ErrorOrOfT { get; }
    public INamedTypeSymbol? ErrorType { get; }
    public INamedTypeSymbol? Error { get; }

    // Primitive well-known types
    public INamedTypeSymbol? Guid { get; }
    public INamedTypeSymbol? DateTime { get; }
    public INamedTypeSymbol? DateTimeOffset { get; }
    public INamedTypeSymbol? DateOnly { get; }
    public INamedTypeSymbol? TimeOnly { get; }
    public INamedTypeSymbol? TimeSpan { get; }

    // Misc
    public INamedTypeSymbol? ReadOnlySpanOfT { get; }
    public INamedTypeSymbol? IFormatProvider { get; }
    public INamedTypeSymbol? Stream { get; }
    public INamedTypeSymbol? PipeReader { get; }
    public INamedTypeSymbol? NullableOfT { get; }
    public INamedTypeSymbol? TypedResults { get; }

    public IMethodSymbol? TypedResultsProblem { get; }
    public IMethodSymbol? TypedResultsValidationProblem { get; }
    public IMethodSymbol? TypedResultsUnauthorized { get; }
    public IMethodSymbol? TypedResultsForbid { get; }

    // ErrorOr-specific types (generated attributes)
    public INamedTypeSymbol? ErrorOrEndpointAttribute { get; }
    public INamedTypeSymbol? GetAttribute { get; }
    public INamedTypeSymbol? PostAttribute { get; }
    public INamedTypeSymbol? PutAttribute { get; }
    public INamedTypeSymbol? DeleteAttribute { get; }
    public INamedTypeSymbol? PatchAttribute { get; }
    public INamedTypeSymbol? ProducesErrorAttribute { get; }
    public INamedTypeSymbol? AcceptedResponseAttribute { get; }
    public INamedTypeSymbol? ReturnsErrorAttribute { get; }

    // Additional ASP.NET types not in AspNetContext
    public INamedTypeSymbol? FromKeyedServicesAttribute { get; }
    public INamedTypeSymbol? AsParametersAttribute { get; }
    public INamedTypeSymbol? FormFileCollection { get; }
    public INamedTypeSymbol? FormCollection { get; }
    public INamedTypeSymbol? BindableFromHttpContext { get; }
    public INamedTypeSymbol? ParameterInfo { get; }

    // System types
    public INamedTypeSymbol? ObsoleteAttribute { get; }
    public INamedTypeSymbol? CancellationToken { get; }

    // Convenience accessors that delegate to AspNetContext
    public INamedTypeSymbol? FromBody => AspNet.FromBodyAttribute;
    public INamedTypeSymbol? FromServices => AspNet.FromServicesAttribute;
    public INamedTypeSymbol? FromRoute => AspNet.FromRouteAttribute;
    public INamedTypeSymbol? FromQuery => AspNet.FromQueryAttribute;
    public INamedTypeSymbol? FromHeader => AspNet.FromHeaderAttribute;
    public INamedTypeSymbol? FromForm => AspNet.FromFormAttribute;
    public INamedTypeSymbol? FormFile => AspNet.FormFile;
    public INamedTypeSymbol? HttpContext => AspNet.HttpContext;

    // Short aliases for backward compatibility with KnownSymbols
    public INamedTypeSymbol? FromKeyedServices => FromKeyedServicesAttribute;
    public INamedTypeSymbol? AsParameters => AsParametersAttribute;
    public INamedTypeSymbol? HttpContextSymbol => HttpContext;

    // Helper methods
    public bool IsFormFile(ITypeSymbol? type)
    {
        return AspNet.IsFormFile(type);
    }

    public bool IsFormFileCollection(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        if (FormFileCollection is not null &&
            SymbolEqualityComparer.Default.Equals(type, FormFileCollection))
            return true;

        // Check for IReadOnlyList<IFormFile>
        if (IReadOnlyListOfT is not null &&
            type is INamedTypeSymbol { IsGenericType: true } named &&
            SymbolEqualityComparer.Default.Equals(named.ConstructedFrom, IReadOnlyListOfT))
            if (IsFormFile(named.TypeArguments[0]))
                return true;

        return false;
    }

    public bool IsFormCollection(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        return FormCollection is not null && type.IsOrImplements(FormCollection);
    }

    public bool IsHttpContext(ITypeSymbol? type)
    {
        return AspNet.IsHttpContextType(type);
    }

    public bool HasFromKeyedServices(IParameterSymbol parameter)
    {
        return FromKeyedServicesAttribute is not null && parameter.GetAttributes().Any(a =>
            SymbolEqualityComparer.Default.Equals(a.AttributeClass, FromKeyedServicesAttribute));
    }

    public bool HasAsParameters(IParameterSymbol parameter)
    {
        return AsParametersAttribute is not null && parameter.GetAttributes().Any(a =>
            SymbolEqualityComparer.Default.Equals(a.AttributeClass, AsParametersAttribute));
    }

    public static ITypeSymbol UnwrapNullable(ITypeSymbol type)
    {
        return type is INamedTypeSymbol
        {
            IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Nullable_T
        } n
            ? n.TypeArguments[0]
            : type;
    }

    private static IMethodSymbol? Pick(INamespaceOrTypeSymbol? type, string name, int paramCount)
    {
        return type?
            .GetMembers(name).OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.IsStatic && m.Parameters.Length == paramCount);
    }
}