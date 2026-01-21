using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;

namespace ErrorOr.Generators;

/// <summary>
///     Required Roslyn symbols that MUST be present for endpoint generation.
///     Uses sealed record for immutability and structural equality.
/// </summary>
/// <remarks>
///     Pattern from .NET Foundation validation generator.
///     Fail-fast: if any required symbol is missing, <see cref="TryCreate" /> returns null.
/// </remarks>
internal sealed record RequiredSymbols(
    // Core ErrorOr types (REQUIRED)
    INamedTypeSymbol ErrorOrOfT,

    // ASP.NET Core types (REQUIRED for endpoint generation)
    INamedTypeSymbol HttpContext,

    // System types (REQUIRED)
    INamedTypeSymbol CancellationToken
)
{
    /// <summary>
    ///     Attempts to resolve all required symbols from the compilation.
    ///     Returns null if ANY required symbol is missing (fail-fast).
    /// </summary>
    public static RequiredSymbols? TryCreate(Compilation compilation)
    {
        // Core ErrorOr - absolutely required
        var errorOrOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.ErrorOrT)?.ConstructedFrom;
        if (errorOrOfT is null)
            return null; // ErrorOr not referenced, nothing to generate

        // ASP.NET Core HttpContext - required for endpoint generation
        var httpContext = compilation.GetBestTypeByMetadataName(WellKnownTypes.HttpContext);
        if (httpContext is null)
            return null; // Not an ASP.NET Core project

        // CancellationToken - always available in .NET
        var cancellationToken = compilation.GetBestTypeByMetadataName(WellKnownTypes.CancellationToken);
        if (cancellationToken is null)
            return null; // Should never happen, but fail-fast

        return new RequiredSymbols(
            ErrorOrOfT: errorOrOfT,
            HttpContext: httpContext,
            CancellationToken: cancellationToken
        );
    }
}

/// <summary>
///     Optional Roslyn symbols that MAY be present.
///     These are features that degrade gracefully if missing.
/// </summary>
internal sealed record OptionalSymbols(
    // ASP.NET Core MVC Attributes
    INamedTypeSymbol? FromBodyAttribute,
    INamedTypeSymbol? FromServicesAttribute,
    INamedTypeSymbol? FromRouteAttribute,
    INamedTypeSymbol? FromQueryAttribute,
    INamedTypeSymbol? FromHeaderAttribute,
    INamedTypeSymbol? FromFormAttribute,
    INamedTypeSymbol? FromKeyedServicesAttribute,
    INamedTypeSymbol? AsParametersAttribute,

    // ErrorOr custom attributes
    INamedTypeSymbol? ProducesErrorAttribute,
    INamedTypeSymbol? AcceptedResponseAttribute,
    INamedTypeSymbol? ReturnsErrorAttribute,

    // Error type
    INamedTypeSymbol? Error,

    // Form types
    INamedTypeSymbol? FormFile,
    INamedTypeSymbol? FormFileCollection,
    INamedTypeSymbol? FormCollection,
    INamedTypeSymbol? BindableFromHttpContext,
    INamedTypeSymbol? ParameterInfo,

    // Stream types
    INamedTypeSymbol? Stream,
    INamedTypeSymbol? PipeReader,

    // Result markers
    INamedTypeSymbol? SuccessMarker,
    INamedTypeSymbol? CreatedMarker,
    INamedTypeSymbol? UpdatedMarker,
    INamedTypeSymbol? DeletedMarker,

    // Task types
    INamedTypeSymbol? TaskOfT,
    INamedTypeSymbol? ValueTaskOfT,

    // Collection types
    INamedTypeSymbol? ListOfT,
    INamedTypeSymbol? IListOfT,
    INamedTypeSymbol? IEnumerableOfT,
    INamedTypeSymbol? IAsyncEnumerableOfT,
    INamedTypeSymbol? IReadOnlyListOfT,
    INamedTypeSymbol? ICollectionOfT,
    INamedTypeSymbol? HashSetOfT,
    INamedTypeSymbol? SseItemOfT,

    // Primitive well-known types
    INamedTypeSymbol? Guid,
    INamedTypeSymbol? DateTime,
    INamedTypeSymbol? DateTimeOffset,
    INamedTypeSymbol? DateOnly,
    INamedTypeSymbol? TimeOnly,
    INamedTypeSymbol? TimeSpan,

    // Misc
    INamedTypeSymbol? ReadOnlySpanOfT,
    INamedTypeSymbol? IFormatProvider,

    // Middleware attributes
    INamedTypeSymbol? AuthorizeAttribute,
    INamedTypeSymbol? AllowAnonymousAttribute,
    INamedTypeSymbol? EnableRateLimitingAttribute,
    INamedTypeSymbol? DisableRateLimitingAttribute,
    INamedTypeSymbol? OutputCacheAttribute,
    INamedTypeSymbol? EnableCorsAttribute,
    INamedTypeSymbol? DisableCorsAttribute,

    // Validation types
    INamedTypeSymbol? ValidationAttribute,
    INamedTypeSymbol? IValidatableObject
)
{
    /// <summary>
    ///     Resolves all optional symbols. Never fails - just returns null for missing symbols.
    /// </summary>
    public static OptionalSymbols Create(Compilation compilation) =>
        new(
            FromBodyAttribute: compilation.GetBestTypeByMetadataName(WellKnownTypes.FromBodyAttribute),
            FromServicesAttribute: compilation.GetBestTypeByMetadataName(WellKnownTypes.FromServicesAttribute),
            FromRouteAttribute: compilation.GetBestTypeByMetadataName(WellKnownTypes.FromRouteAttribute),
            FromQueryAttribute: compilation.GetBestTypeByMetadataName(WellKnownTypes.FromQueryAttribute),
            FromHeaderAttribute: compilation.GetBestTypeByMetadataName(WellKnownTypes.FromHeaderAttribute),
            FromFormAttribute: compilation.GetBestTypeByMetadataName(WellKnownTypes.FromFormAttribute),
            FromKeyedServicesAttribute: compilation.GetBestTypeByMetadataName(WellKnownTypes.FromKeyedServicesAttribute),
            AsParametersAttribute: compilation.GetBestTypeByMetadataName(WellKnownTypes.AsParametersAttribute),

            ProducesErrorAttribute: compilation.GetBestTypeByMetadataName(WellKnownTypes.ProducesErrorAttribute),
            AcceptedResponseAttribute: compilation.GetBestTypeByMetadataName(WellKnownTypes.AcceptedResponseAttribute),
            ReturnsErrorAttribute: compilation.GetBestTypeByMetadataName(WellKnownTypes.ReturnsErrorAttribute),

            Error: compilation.GetBestTypeByMetadataName(WellKnownTypes.Error),

            FormFile: compilation.GetBestTypeByMetadataName(WellKnownTypes.FormFile),
            FormFileCollection: compilation.GetBestTypeByMetadataName(WellKnownTypes.FormFileCollection),
            FormCollection: compilation.GetBestTypeByMetadataName(WellKnownTypes.FormCollection),
            BindableFromHttpContext: compilation.GetBestTypeByMetadataName(WellKnownTypes.BindableFromHttpContext),
            ParameterInfo: compilation.GetBestTypeByMetadataName(WellKnownTypes.ParameterInfo),

            Stream: compilation.GetBestTypeByMetadataName(WellKnownTypes.Stream),
            PipeReader: compilation.GetBestTypeByMetadataName(WellKnownTypes.PipeReader),

            SuccessMarker: compilation.GetBestTypeByMetadataName(WellKnownTypes.Success),
            CreatedMarker: compilation.GetBestTypeByMetadataName(WellKnownTypes.Created),
            UpdatedMarker: compilation.GetBestTypeByMetadataName(WellKnownTypes.Updated),
            DeletedMarker: compilation.GetBestTypeByMetadataName(WellKnownTypes.Deleted),

            TaskOfT: compilation.GetBestTypeByMetadataName(WellKnownTypes.TaskT)?.ConstructedFrom,
            ValueTaskOfT: compilation.GetBestTypeByMetadataName(WellKnownTypes.ValueTaskT)?.ConstructedFrom,

            ListOfT: compilation.GetBestTypeByMetadataName(WellKnownTypes.ListT)?.ConstructedFrom,
            IListOfT: compilation.GetBestTypeByMetadataName(WellKnownTypes.IListT)?.ConstructedFrom,
            IEnumerableOfT: compilation.GetBestTypeByMetadataName(WellKnownTypes.IEnumerableT)?.ConstructedFrom,
            IAsyncEnumerableOfT: compilation.GetBestTypeByMetadataName(WellKnownTypes.IAsyncEnumerableT)?.ConstructedFrom,
            IReadOnlyListOfT: compilation.GetBestTypeByMetadataName(WellKnownTypes.IReadOnlyListT)?.ConstructedFrom,
            ICollectionOfT: compilation.GetBestTypeByMetadataName(WellKnownTypes.ICollectionT)?.ConstructedFrom,
            HashSetOfT: compilation.GetBestTypeByMetadataName(WellKnownTypes.HashSetT)?.ConstructedFrom,
            SseItemOfT: compilation.GetBestTypeByMetadataName(WellKnownTypes.SseItemT)?.ConstructedFrom,

            Guid: compilation.GetBestTypeByMetadataName(WellKnownTypes.Guid),
            DateTime: compilation.GetBestTypeByMetadataName(WellKnownTypes.DateTime),
            DateTimeOffset: compilation.GetBestTypeByMetadataName(WellKnownTypes.DateTimeOffset),
            DateOnly: compilation.GetBestTypeByMetadataName(WellKnownTypes.DateOnly),
            TimeOnly: compilation.GetBestTypeByMetadataName(WellKnownTypes.TimeOnly),
            TimeSpan: compilation.GetBestTypeByMetadataName(WellKnownTypes.TimeSpan),

            ReadOnlySpanOfT: compilation.GetBestTypeByMetadataName(WellKnownTypes.ReadOnlySpanT)?.ConstructedFrom,
            IFormatProvider: compilation.GetBestTypeByMetadataName(WellKnownTypes.IFormatProvider),

            AuthorizeAttribute: compilation.GetBestTypeByMetadataName(WellKnownTypes.AuthorizeAttribute),
            AllowAnonymousAttribute: compilation.GetBestTypeByMetadataName(WellKnownTypes.AllowAnonymousAttribute),
            EnableRateLimitingAttribute: compilation.GetBestTypeByMetadataName(WellKnownTypes.EnableRateLimitingAttribute),
            DisableRateLimitingAttribute: compilation.GetBestTypeByMetadataName(WellKnownTypes.DisableRateLimitingAttribute),
            OutputCacheAttribute: compilation.GetBestTypeByMetadataName(WellKnownTypes.OutputCacheAttribute),
            EnableCorsAttribute: compilation.GetBestTypeByMetadataName(WellKnownTypes.EnableCorsAttribute),
            DisableCorsAttribute: compilation.GetBestTypeByMetadataName(WellKnownTypes.DisableCorsAttribute),

            ValidationAttribute: compilation.GetBestTypeByMetadataName(WellKnownTypes.ValidationAttribute),
            IValidatableObject: compilation.GetBestTypeByMetadataName(WellKnownTypes.IValidatableObject)
        );
}

/// <summary>
///     Combined symbol context for endpoint generation.
///     Wraps <see cref="RequiredSymbols" /> and <see cref="OptionalSymbols" />.
/// </summary>
internal sealed class SymbolContext
{
    private SymbolContext(RequiredSymbols required, OptionalSymbols optional)
    {
        Required = required;
        Optional = optional;
    }

    public RequiredSymbols Required { get; }
    public OptionalSymbols Optional { get; }

    // Convenience accessors that delegate to Required
    public INamedTypeSymbol ErrorOrOfT => Required.ErrorOrOfT;
    public INamedTypeSymbol HttpContext => Required.HttpContext;
    public INamedTypeSymbol CancellationToken => Required.CancellationToken;

    // Convenience accessors that delegate to Optional (most common ones)
    public INamedTypeSymbol? FromBodyAttribute => Optional.FromBodyAttribute;
    public INamedTypeSymbol? FromServicesAttribute => Optional.FromServicesAttribute;
    public INamedTypeSymbol? FromRouteAttribute => Optional.FromRouteAttribute;
    public INamedTypeSymbol? FromQueryAttribute => Optional.FromQueryAttribute;
    public INamedTypeSymbol? FromHeaderAttribute => Optional.FromHeaderAttribute;
    public INamedTypeSymbol? FromFormAttribute => Optional.FromFormAttribute;
    public INamedTypeSymbol? FromKeyedServicesAttribute => Optional.FromKeyedServicesAttribute;
    public INamedTypeSymbol? AsParametersAttribute => Optional.AsParametersAttribute;
    public INamedTypeSymbol? Error => Optional.Error;
    public INamedTypeSymbol? ValidationAttribute => Optional.ValidationAttribute;
    public INamedTypeSymbol? IValidatableObject => Optional.IValidatableObject;

    /// <summary>
    ///     Creates a SymbolContext if all required symbols are present.
    ///     Returns null if any required symbol is missing.
    /// </summary>
    public static SymbolContext? TryCreate(Compilation compilation)
    {
        var required = RequiredSymbols.TryCreate(compilation);
        if (required is null)
            return null;

        var optional = OptionalSymbols.Create(compilation);
        return new SymbolContext(required, optional);
    }
}
