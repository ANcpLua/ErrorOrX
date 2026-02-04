using Microsoft.CodeAnalysis;

namespace ErrorOr.Generators;

internal sealed class ErrorOrContext
{
    public ErrorOrContext(Compilation compilation)
    {
        FromBodyAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromBodyAttribute);
        FromServicesAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromServicesAttribute);
        FromRouteAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromRouteAttribute);
        FromQueryAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromQueryAttribute);
        FromHeaderAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromHeaderAttribute);
        FromFormAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromFormAttribute);

        ProducesErrorAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.ProducesErrorAttribute);
        AcceptedResponseAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.AcceptedResponseAttribute);
        ReturnsErrorAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.ReturnsErrorAttribute);
        ErrorOrOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.ErrorOrT)?.ConstructedFrom;
        Error = compilation.GetBestTypeByMetadataName(WellKnownTypes.Error);

        FromKeyedServicesAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromKeyedServicesAttribute);
        AsParametersAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.AsParametersAttribute);
        FormFileCollection = compilation.GetBestTypeByMetadataName(WellKnownTypes.FormFileCollection);
        FormCollection = compilation.GetBestTypeByMetadataName(WellKnownTypes.FormCollection);
        FormFile = compilation.GetBestTypeByMetadataName(WellKnownTypes.FormFile);
        HttpContext = compilation.GetBestTypeByMetadataName(WellKnownTypes.HttpContext);
        BindableFromHttpContext = compilation.GetBestTypeByMetadataName(WellKnownTypes.BindableFromHttpContext);
        ParameterInfo = compilation.GetBestTypeByMetadataName(WellKnownTypes.ParameterInfo);
        SseItemOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.SseItemT)?.ConstructedFrom;

        CancellationToken = compilation.GetBestTypeByMetadataName(WellKnownTypes.CancellationToken);

        SuccessMarker = compilation.GetBestTypeByMetadataName(WellKnownTypes.Success);
        CreatedMarker = compilation.GetBestTypeByMetadataName(WellKnownTypes.Created);
        UpdatedMarker = compilation.GetBestTypeByMetadataName(WellKnownTypes.Updated);
        DeletedMarker = compilation.GetBestTypeByMetadataName(WellKnownTypes.Deleted);

        TaskOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.TaskT)?.ConstructedFrom;
        ValueTaskOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.ValueTaskT)?.ConstructedFrom;

        ListOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.ListT)?.ConstructedFrom;
        IListOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.IListT)?.ConstructedFrom;
        IEnumerableOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.IEnumerableT)?.ConstructedFrom;
        IAsyncEnumerableOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.IAsyncEnumerableT)
            ?.ConstructedFrom;
        IReadOnlyListOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.IReadOnlyListT)
            ?.ConstructedFrom;
        ICollectionOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.ICollectionT)?.ConstructedFrom;
        HashSetOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.HashSetT)?.ConstructedFrom;

        Guid = compilation.GetBestTypeByMetadataName(WellKnownTypes.Guid);
        DateTime = compilation.GetBestTypeByMetadataName(WellKnownTypes.DateTime);
        DateTimeOffset = compilation.GetBestTypeByMetadataName(WellKnownTypes.DateTimeOffset);
        DateOnly = compilation.GetBestTypeByMetadataName(WellKnownTypes.DateOnly);
        TimeOnly = compilation.GetBestTypeByMetadataName(WellKnownTypes.TimeOnly);
        TimeSpan = compilation.GetBestTypeByMetadataName(WellKnownTypes.TimeSpan);

        ReadOnlySpanOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.ReadOnlySpanT)?.ConstructedFrom;
        IFormatProvider = compilation.GetBestTypeByMetadataName(WellKnownTypes.IFormatProvider);
        Stream = compilation.GetBestTypeByMetadataName(WellKnownTypes.Stream);
        PipeReader = compilation.GetBestTypeByMetadataName(WellKnownTypes.PipeReader);

        AuthorizeAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.AuthorizeAttribute);
        AllowAnonymousAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.AllowAnonymousAttribute);
        EnableRateLimitingAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.EnableRateLimitingAttribute);
        DisableRateLimitingAttribute =
            compilation.GetBestTypeByMetadataName(WellKnownTypes.DisableRateLimitingAttribute);
        OutputCacheAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.OutputCacheAttribute);
        EnableCorsAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.EnableCorsAttribute);
        DisableCorsAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.DisableCorsAttribute);

        ValidationAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.ValidationAttribute);
        IValidatableObject = compilation.GetBestTypeByMetadataName(WellKnownTypes.IValidatableObject);

        ApiVersionAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.ApiVersionAttribute);
        ApiVersionNeutralAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.ApiVersionNeutralAttribute);
        MapToApiVersionAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.MapToApiVersionAttribute);

        RouteGroupAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.RouteGroupAttribute);
    }

    private INamedTypeSymbol? FromBodyAttribute { get; }
    private INamedTypeSymbol? FromServicesAttribute { get; }
    private INamedTypeSymbol? FromRouteAttribute { get; }
    private INamedTypeSymbol? FromQueryAttribute { get; }
    private INamedTypeSymbol? FromHeaderAttribute { get; }
    private INamedTypeSymbol? FromFormAttribute { get; }

    public INamedTypeSymbol? SuccessMarker { get; }
    public INamedTypeSymbol? CreatedMarker { get; }
    public INamedTypeSymbol? UpdatedMarker { get; }
    public INamedTypeSymbol? DeletedMarker { get; }

    public INamedTypeSymbol? TaskOfT { get; }
    public INamedTypeSymbol? ValueTaskOfT { get; }

    public INamedTypeSymbol? ListOfT { get; }
    public INamedTypeSymbol? IListOfT { get; }
    public INamedTypeSymbol? IEnumerableOfT { get; }
    public INamedTypeSymbol? IAsyncEnumerableOfT { get; }
    public INamedTypeSymbol? IReadOnlyListOfT { get; }
    public INamedTypeSymbol? ICollectionOfT { get; }
    public INamedTypeSymbol? HashSetOfT { get; }
    public INamedTypeSymbol? SseItemOfT { get; }
    public INamedTypeSymbol? ErrorOrOfT { get; }
    public INamedTypeSymbol? Error { get; }

    public INamedTypeSymbol? Guid { get; }
    public INamedTypeSymbol? DateTime { get; }
    public INamedTypeSymbol? DateTimeOffset { get; }
    public INamedTypeSymbol? DateOnly { get; }
    public INamedTypeSymbol? TimeOnly { get; }
    public INamedTypeSymbol? TimeSpan { get; }

    public INamedTypeSymbol? ReadOnlySpanOfT { get; }
    public INamedTypeSymbol? IFormatProvider { get; }
    private INamedTypeSymbol? Stream { get; }
    private INamedTypeSymbol? PipeReader { get; }

    public INamedTypeSymbol? ProducesErrorAttribute { get; }
    public INamedTypeSymbol? AcceptedResponseAttribute { get; }
    public INamedTypeSymbol? ReturnsErrorAttribute { get; }

    private INamedTypeSymbol? FromKeyedServicesAttribute { get; }
    private INamedTypeSymbol? AsParametersAttribute { get; }
    private INamedTypeSymbol? FormFileCollection { get; }
    private INamedTypeSymbol? FormCollection { get; }
    private INamedTypeSymbol? FormFile { get; }
    private INamedTypeSymbol? HttpContext { get; }
    public INamedTypeSymbol? BindableFromHttpContext { get; }
    private INamedTypeSymbol? ParameterInfo { get; }

    private INamedTypeSymbol? CancellationToken { get; }

    public INamedTypeSymbol? AuthorizeAttribute { get; }
    public INamedTypeSymbol? AllowAnonymousAttribute { get; }
    public INamedTypeSymbol? EnableRateLimitingAttribute { get; }
    public INamedTypeSymbol? DisableRateLimitingAttribute { get; }
    public INamedTypeSymbol? OutputCacheAttribute { get; }
    public INamedTypeSymbol? EnableCorsAttribute { get; }
    public INamedTypeSymbol? DisableCorsAttribute { get; }

    private INamedTypeSymbol? ValidationAttribute { get; }
    private INamedTypeSymbol? IValidatableObject { get; }

    public INamedTypeSymbol? ApiVersionAttribute { get; }
    public INamedTypeSymbol? ApiVersionNeutralAttribute { get; }
    public INamedTypeSymbol? MapToApiVersionAttribute { get; }

    public INamedTypeSymbol? RouteGroupAttribute { get; }

    /// <summary>
    ///     Returns true if the Asp.Versioning.Http package is referenced.
    /// </summary>
    public bool HasApiVersioningSupport => ApiVersionAttribute is not null;

    public INamedTypeSymbol? FromBody => FromBodyAttribute;
    public INamedTypeSymbol? FromServices => FromServicesAttribute;
    public INamedTypeSymbol? FromRoute => FromRouteAttribute;
    public INamedTypeSymbol? FromQuery => FromQueryAttribute;
    public INamedTypeSymbol? FromHeader => FromHeaderAttribute;
    public INamedTypeSymbol? FromForm => FromFormAttribute;
    public INamedTypeSymbol? FromKeyedServices => FromKeyedServicesAttribute;
    public INamedTypeSymbol? AsParameters => AsParametersAttribute;

    /// <summary>
    ///     Creates an incremental provider that resolves ErrorOrContext once per compilation.
    ///     This avoids the N+1 performance issue where ErrorOrContext was created N times
    ///     (once per endpoint), causing 90+ symbol lookups per endpoint.
    /// </summary>
    public static IncrementalValueProvider<ErrorOrContext> CreateProvider(
        IncrementalGeneratorInitializationContext context)
    {
        return context.CompilationProvider
            .Select(static (compilation, _) => new ErrorOrContext(compilation));
    }

    /// <summary>
    ///     Determines if a type requires BCL validation.
    ///     Returns true if the type:
    ///     1. Has any property (including inherited) with an attribute deriving from ValidationAttribute, OR
    ///     2. Implements IValidatableObject
    ///     Used during parameter binding to mark parameters for validation metadata.
    ///     ASP.NET Core runtime automatically invokes validation if metadata is present.
    ///     This enables automatic validation detection without hardcoding specific attributes.
    /// </summary>
    public bool RequiresValidation(ITypeSymbol? type)
    {
        if (type is null || ValidationAttribute is null)
            return false;

        if (type.SpecialType is not SpecialType.None ||
            type.TypeKind is TypeKind.Enum or TypeKind.Interface)
            return false;

        if (IValidatableObject is not null &&
            type.AllInterfaces.Any(i => i.IsEqualTo(IValidatableObject)))
            return true;

        var current = type;
        while (current is INamedTypeSymbol namedType)
        {
            foreach (var member in namedType.GetMembers())
            {
                if (member is not IPropertySymbol property)
                    continue;

                if (property.HasAttribute(ValidationAttribute))
                    return true;
            }

            current = namedType.BaseType;
        }

        return false;
    }

    /// <summary>Checks if the type implements IFormFile.</summary>
    public bool IsFormFile(ITypeSymbol? type)
    {
        type = type?.UnwrapNullable();
        if (type is null)
            return false;

        if (FormFile is not null && type.IsOrImplements(FormFile))
            return true;

        return type.Name == "IFormFile" &&
               type.ContainingNamespace.ToDisplayString() == "Microsoft.AspNetCore.Http";
    }

    /// <summary>Checks if the type is IFormFileCollection or IReadOnlyList&lt;IFormFile&gt;.</summary>
    public bool IsFormFileCollection(ITypeSymbol? type)
    {
        type = type?.UnwrapNullable();
        if (type is null)
            return false;

        if (FormFileCollection is not null &&
            type.IsEqualTo(FormFileCollection))
            return true;

        if (IReadOnlyListOfT is not null &&
            type is INamedTypeSymbol { IsGenericType: true } named &&
            named.ConstructedFrom.IsEqualTo(IReadOnlyListOfT))
            if (IsFormFile(named.TypeArguments[0]))
                return true;

        return false;
    }

    /// <summary>Checks if the type implements IFormCollection.</summary>
    public bool IsFormCollection(ITypeSymbol? type)
    {
        type = type?.UnwrapNullable();
        if (type is null)
            return false;

        return FormCollection is not null && type.IsOrImplements(FormCollection);
    }

    /// <summary>Checks if the type is or inherits from HttpContext.</summary>
    public bool IsHttpContext(ITypeSymbol? type)
    {
        type = type?.UnwrapNullable();
        if (type is null)
            return false;

        if (HttpContext is not null && type.IsOrInheritsFrom(HttpContext))
            return true;

        return type.Name == "HttpContext" &&
               type.ContainingNamespace.ToDisplayString() == "Microsoft.AspNetCore.Http";
    }

    /// <summary>Checks if the type is or inherits from System.IO.Stream.</summary>
    public bool IsStream(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        type = type.UnwrapNullable();
        if (Stream is not null)
            return type.IsOrInheritsFrom(Stream);

        return type.Name == "Stream" &&
               type.ContainingNamespace.ToDisplayString() == "System.IO";
    }

    /// <summary>Checks if the type is or inherits from System.IO.Pipelines.PipeReader.</summary>
    public bool IsPipeReader(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        type = type.UnwrapNullable();
        if (PipeReader is not null)
            return type.IsOrInheritsFrom(PipeReader);

        return type.Name == "PipeReader" &&
               type.ContainingNamespace.ToDisplayString() == "System.IO.Pipelines";
    }

    /// <summary>Checks if the type is System.Threading.CancellationToken.</summary>
    public bool IsCancellationToken(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        type = type.UnwrapNullable();
        if (CancellationToken is not null)
            return type.IsEqualTo(CancellationToken);

        return type.Name == "CancellationToken" &&
               type.ContainingNamespace.ToDisplayString() == "System.Threading";
    }

    /// <summary>Checks if the type is System.Reflection.ParameterInfo.</summary>
    public bool IsParameterInfo(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        type = type.UnwrapNullable();
        if (ParameterInfo is not null)
            return type.IsEqualTo(ParameterInfo);

        return type.Name == "ParameterInfo" &&
               type.ContainingNamespace.ToDisplayString() == "System.Reflection";
    }
}
