using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;

// ReSharper disable InconsistentNaming

namespace ErrorOr.Generators;

/// <summary>
///     Compilation context for ErrorOr.Endpoints generator.
///     Composes ASP.NET Core types with ErrorOr-specific types.
/// </summary>
internal sealed class ErrorOrContext
{
    public ErrorOrContext(Compilation compilation)
    {
        // ASP.NET Core MVC Attributes
        FromBodyAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromBodyAttribute);
        FromServicesAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromServicesAttribute);
        FromRouteAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromRouteAttribute);
        FromQueryAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromQueryAttribute);
        FromHeaderAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromHeaderAttribute);
        FromFormAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromFormAttribute);

        // ErrorOr types
        ProducesErrorAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.ProducesErrorAttribute);
        AcceptedResponseAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.AcceptedResponseAttribute);
        ReturnsErrorAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.ReturnsErrorAttribute);
        ErrorOrOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.ErrorOrT)?.ConstructedFrom;
        Error = compilation.GetBestTypeByMetadataName(WellKnownTypes.Error);

        // Additional ASP.NET types
        FromKeyedServicesAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromKeyedServicesAttribute);
        AsParametersAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.AsParametersAttribute);
        FormFileCollection = compilation.GetBestTypeByMetadataName(WellKnownTypes.FormFileCollection);
        FormCollection = compilation.GetBestTypeByMetadataName(WellKnownTypes.FormCollection);
        FormFile = compilation.GetBestTypeByMetadataName(WellKnownTypes.FormFile);
        HttpContext = compilation.GetBestTypeByMetadataName(WellKnownTypes.HttpContext);
        BindableFromHttpContext = compilation.GetBestTypeByMetadataName(WellKnownTypes.BindableFromHttpContext);
        ParameterInfo = compilation.GetBestTypeByMetadataName(WellKnownTypes.ParameterInfo);
        SseItemOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.SseItemT)?.ConstructedFrom;

        // System types
        CancellationToken = compilation.GetBestTypeByMetadataName(WellKnownTypes.CancellationToken);

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
        IAsyncEnumerableOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.IAsyncEnumerableT)
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

        // Middleware attributes (BCL)
        AuthorizeAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.AuthorizeAttribute);
        AllowAnonymousAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.AllowAnonymousAttribute);
        EnableRateLimitingAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.EnableRateLimitingAttribute);
        DisableRateLimitingAttribute =
            compilation.GetBestTypeByMetadataName(WellKnownTypes.DisableRateLimitingAttribute);
        OutputCacheAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.OutputCacheAttribute);
        EnableCorsAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.EnableCorsAttribute);
        DisableCorsAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.DisableCorsAttribute);

        // BCL Validation types (for automatic validation detection)
        ValidationAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.ValidationAttribute);
        IValidatableObject = compilation.GetBestTypeByMetadataName(WellKnownTypes.IValidatableObject);
    }

    // ASP.NET Core MVC Attributes
    public INamedTypeSymbol? FromBodyAttribute { get; }
    public INamedTypeSymbol? FromServicesAttribute { get; }
    public INamedTypeSymbol? FromRouteAttribute { get; }
    public INamedTypeSymbol? FromQueryAttribute { get; }
    public INamedTypeSymbol? FromHeaderAttribute { get; }
    public INamedTypeSymbol? FromFormAttribute { get; }

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

    // ErrorOr types
    public INamedTypeSymbol? ProducesErrorAttribute { get; }
    public INamedTypeSymbol? AcceptedResponseAttribute { get; }
    public INamedTypeSymbol? ReturnsErrorAttribute { get; }

    // Additional ASP.NET types
    public INamedTypeSymbol? FromKeyedServicesAttribute { get; }
    public INamedTypeSymbol? AsParametersAttribute { get; }
    public INamedTypeSymbol? FormFileCollection { get; }
    public INamedTypeSymbol? FormCollection { get; }
    public INamedTypeSymbol? FormFile { get; }
    public INamedTypeSymbol? HttpContext { get; }
    public INamedTypeSymbol? BindableFromHttpContext { get; }
    public INamedTypeSymbol? ParameterInfo { get; }

    // System types
    public INamedTypeSymbol? CancellationToken { get; }

    // Middleware attributes (BCL)
    public INamedTypeSymbol? AuthorizeAttribute { get; }
    public INamedTypeSymbol? AllowAnonymousAttribute { get; }
    public INamedTypeSymbol? EnableRateLimitingAttribute { get; }
    public INamedTypeSymbol? DisableRateLimitingAttribute { get; }
    public INamedTypeSymbol? OutputCacheAttribute { get; }
    public INamedTypeSymbol? EnableCorsAttribute { get; }
    public INamedTypeSymbol? DisableCorsAttribute { get; }

    // BCL Validation types
    public INamedTypeSymbol? ValidationAttribute { get; }
    public INamedTypeSymbol? IValidatableObject { get; }

    // Convenience accessors
    public INamedTypeSymbol? FromBody => FromBodyAttribute;
    public INamedTypeSymbol? FromServices => FromServicesAttribute;
    public INamedTypeSymbol? FromRoute => FromRouteAttribute;
    public INamedTypeSymbol? FromQuery => FromQueryAttribute;
    public INamedTypeSymbol? FromHeader => FromHeaderAttribute;
    public INamedTypeSymbol? FromForm => FromFormAttribute;
    public INamedTypeSymbol? FromKeyedServices => FromKeyedServicesAttribute;
    public INamedTypeSymbol? AsParameters => AsParametersAttribute;

    /// <summary>
    ///     Determines if a type requires BCL validation.
    ///     Returns true if the type:
    ///     1. Has any property with an attribute deriving from ValidationAttribute, OR
    ///     2. Implements IValidatableObject
    ///     This enables automatic validation detection without hardcoding specific attributes.
    /// </summary>
    public bool RequiresValidation(ITypeSymbol? type)
    {
        try
        {
            if (type is null || ValidationAttribute is null)
                return false;

            // Skip primitives and strings - they don't need object-level validation
            if (type.SpecialType is not SpecialType.None ||
                type.TypeKind is TypeKind.Enum or TypeKind.Interface)
                return false;

            // Check if type implements IValidatableObject
            if (IValidatableObject is not null &&
                type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, IValidatableObject)))
                return true;

            // Check if any property has a ValidationAttribute descendant
            foreach (var member in type.GetMembers())
            {
                if (member is not IPropertySymbol property)
                    continue;

                foreach (var attribute in property.GetAttributes())
                    if (attribute.AttributeClass is not null &&
                        attribute.AttributeClass.IsOrInheritsFrom(ValidationAttribute))
                        return true;
            }

            return false;
        }
        catch
        {
            // Gracefully degrade if validation detection fails
            return false;
        }
    }

    #region Helper Methods

    /// <summary>Checks if the type implements IFormFile.</summary>
    public bool IsFormFile(ITypeSymbol? type)
    {
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

    /// <summary>Checks if the type implements IFormCollection.</summary>
    public bool IsFormCollection(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        return FormCollection is not null && type.IsOrImplements(FormCollection);
    }

    /// <summary>Checks if the type is or inherits from HttpContext.</summary>
    public bool IsHttpContext(ITypeSymbol? type)
    {
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

        type = UnwrapNullable(type);
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

        type = UnwrapNullable(type);
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

        type = UnwrapNullable(type);
        if (CancellationToken is not null)
            return SymbolEqualityComparer.Default.Equals(type, CancellationToken);

        return type.Name == "CancellationToken" &&
               type.ContainingNamespace.ToDisplayString() == "System.Threading";
    }

    /// <summary>Checks if the type is System.Reflection.ParameterInfo.</summary>
    public bool IsParameterInfo(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        type = UnwrapNullable(type);
        if (ParameterInfo is not null)
            return SymbolEqualityComparer.Default.Equals(type, ParameterInfo);

        return type.Name == "ParameterInfo" &&
               type.ContainingNamespace.ToDisplayString() == "System.Reflection";
    }

    /// <summary>Unwraps Nullable&lt;T&gt; to get the underlying type.</summary>
    public static ITypeSymbol UnwrapNullable(ITypeSymbol type)
    {
        return type is INamedTypeSymbol
        {
            IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Nullable_T
        } n
            ? n.TypeArguments[0]
            : type;
    }

    #endregion
}