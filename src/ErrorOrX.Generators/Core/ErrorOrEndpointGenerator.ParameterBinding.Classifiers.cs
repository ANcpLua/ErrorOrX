using ANcpLua.Roslyn.Utilities.Models;
using ErrorOr.Analyzers;
using Microsoft.CodeAnalysis;

namespace ErrorOr.Generators;

/// <summary>
///     Partial class containing per-binding-source parameter classifiers.
///     Each <c>ClassifyFrom*Parameter</c> validates one attribute family and emits the
///     matching EOE0xx diagnostic on failure. <see cref="ClassifyAsParameters" /> and
///     <see cref="ClassifyFormDtoParameter" /> additionally recurse into constructor parameters
///     to build nested <see cref="EndpointParameter" /> trees.
/// </summary>
public sealed partial class ErrorOrEndpointGenerator
{
    /// <summary>
    ///     Classifies [FromRoute] parameter with proper EOE010 diagnostic.
    /// </summary>
    private static ParameterClassificationResult ClassifyFromRouteParameter(
        in ParameterMeta meta,
        ISymbol method,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        var hasTryParse = meta.CustomBinding is CustomBindingMethod.TryParse or CustomBindingMethod.TryParseWithFormat;

        // EOE010: [FromRoute] requires primitive or TryParse
        if (meta.RouteKind is null && !hasTryParse)
        {
            diagnostics.Add(DiagnosticInfo.Create(
                Descriptors.InvalidFromRouteType,
                method.Locations.FirstOrDefault() ?? Location.None,
                meta.Name,
                meta.TypeFqn));
            return ParameterClassificationResult.Error;
        }

        return ParameterSuccess(in meta, ParameterSource.Route, meta.BoundName,
            customBinding: meta.CustomBinding);
    }

    /// <summary>
    ///     Classifies implicit route parameter with proper EOE010 diagnostic.
    /// </summary>
    private static ParameterClassificationResult ClassifyImplicitRouteParameter(
        in ParameterMeta meta,
        ISymbol method,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        var hasTryParse = meta.CustomBinding is CustomBindingMethod.TryParse or CustomBindingMethod.TryParseWithFormat;

        // EOE010: Route parameters must use supported primitive types or TryParse
        if (meta.RouteKind is null && !hasTryParse)
        {
            diagnostics.Add(DiagnosticInfo.Create(
                Descriptors.InvalidFromRouteType,
                method.Locations.FirstOrDefault() ?? Location.None,
                meta.Name,
                meta.TypeFqn));
            return ParameterClassificationResult.Error;
        }

        return ParameterSuccess(in meta, ParameterSource.Route, meta.Name, customBinding: meta.CustomBinding);
    }

    /// <summary>
    ///     Classifies [FromQuery] parameter with proper EOE011 diagnostic.
    /// </summary>
    private static ParameterClassificationResult ClassifyFromQueryParameter(
        in ParameterMeta meta,
        ISymbol method,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        // Valid: primitive type
        if (meta.RouteKind is not null)
            return ParameterSuccess(in meta, ParameterSource.Query, queryName: meta.BoundName);

        // Valid: collection of primitives
        if (meta is { IsCollection: true, CollectionItemPrimitiveKind: not null })
            return ParameterSuccess(in meta, ParameterSource.Query, queryName: meta.BoundName);

        // Valid: has TryParse
        if (meta.CustomBinding is CustomBindingMethod.TryParse or CustomBindingMethod.TryParseWithFormat)
        {
            return ParameterSuccess(in meta, ParameterSource.Query, queryName: meta.BoundName,
                customBinding: meta.CustomBinding);
        }

        // EOE011: [FromQuery] only supports primitives or collections of primitives
        diagnostics.Add(DiagnosticInfo.Create(
            Descriptors.InvalidFromQueryType,
            method.Locations.FirstOrDefault() ?? Location.None,
            meta.Name,
            meta.TypeFqn));
        return ParameterClassificationResult.Error;
    }

    /// <summary>
    ///     Classifies [FromHeader] parameter with proper EOE014 diagnostic.
    /// </summary>
    private static ParameterClassificationResult ClassifyFromHeaderParameter(
        in ParameterMeta meta,
        ISymbol method,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        // Valid: primitive type (has implicit TryParse)
        if (meta.RouteKind is not null)
            return ParameterSuccess(in meta, ParameterSource.Header, headerName: meta.BoundName);

        // Valid: collection of strings or primitives
        if (meta is { IsCollection: true, CollectionItemPrimitiveKind: not null })
            return ParameterSuccess(in meta, ParameterSource.Header, headerName: meta.BoundName);

        // Valid: has TryParse
        if (meta.CustomBinding is CustomBindingMethod.TryParse or CustomBindingMethod.TryParseWithFormat)
        {
            return ParameterSuccess(in meta, ParameterSource.Header, headerName: meta.BoundName,
                customBinding: meta.CustomBinding);
        }

        // EOE014: [FromHeader] requires string, primitive with TryParse, or collection thereof
        diagnostics.Add(DiagnosticInfo.Create(
            Descriptors.InvalidFromHeaderType,
            method.Locations.FirstOrDefault() ?? Location.None,
            meta.Name,
            meta.TypeFqn));
        return ParameterClassificationResult.Error;
    }

    private static ParameterClassificationResult ClassifyFromFormParameter(
        in ParameterMeta meta,
        ITypeSymbol type)
    {
        if (meta.IsFormFile) return ParameterSuccess(in meta, ParameterSource.FormFile, formName: meta.BoundName);

        if (meta.IsFormFileCollection)
            return ParameterSuccess(in meta, ParameterSource.FormFiles, formName: meta.BoundName);

        if (meta.IsFormCollection)
            return ParameterSuccess(in meta, ParameterSource.FormCollection, formName: meta.BoundName);

        if (meta.RouteKind is not null || meta is { IsCollection: true, CollectionItemPrimitiveKind: not null })
            return ParameterSuccess(in meta, ParameterSource.Form, formName: meta.BoundName);

        // Complex DTO - let BCL handle form binding
        return ClassifyFormDtoParameter(in meta, type);
    }

    private static ParameterClassificationResult ClassifyFormDtoParameter(
        in ParameterMeta meta,
        ITypeSymbol type)
    {
        // For complex form DTOs, analyze the constructor to build child parameter info
        // BCL handles actual binding - we just need structure for code generation
        if (type is not INamedTypeSymbol typeSymbol)
        {
            // Non-named types get simple form binding - BCL will handle/fail at runtime
            return ParameterSuccess(in meta, ParameterSource.Form, formName: meta.BoundName);
        }

        var constructor = typeSymbol.Constructors
            .Where(static c => c.DeclaredAccessibility == Accessibility.Public && !c.IsStatic)
            .OrderByDescending(static c => c.Parameters.Length)
            .FirstOrDefault();

        if (constructor is null || constructor.Parameters.Length is 0)
        {
            // No suitable constructor - simple form binding
            return ParameterSuccess(in meta, ParameterSource.Form, formName: meta.BoundName);
        }

        // Build child parameters for DTO constructor
        var children = ImmutableArray.CreateBuilder<EndpointParameter>(constructor.Parameters.Length);

        foreach (var paramSymbol in constructor.Parameters)
        {
            var childMeta = CreateParameterMeta(paramSymbol);

            ParameterSource childSource;
            if (childMeta.IsFormFile)
                childSource = ParameterSource.FormFile;
            else if (childMeta.IsFormFileCollection)
                childSource = ParameterSource.FormFiles;
            else
                childSource = ParameterSource.Form;

            children.Add(new EndpointParameter(
                childMeta.Name,
                childMeta.TypeFqn,
                childSource,
                childMeta.BoundName,
                childMeta.IsNullable,
                childMeta.IsNonNullableValueType,
                childMeta.IsCollection,
                childMeta.CollectionItemTypeFqn,
                default));
        }

        return new ParameterClassificationResult(IsError: false, new EndpointParameter(
            meta.Name,
            meta.TypeFqn,
            ParameterSource.Form,
            meta.BoundName,
            meta.IsNullable,
            meta.IsNonNullableValueType,
IsCollection: false,
CollectionItemTypeFqn: null,
            new EquatableArray<EndpointParameter>(children.ToImmutable()),
            CustomBindingMethod.None,
            meta.RequiresValidation,
            ValidatableProperties: meta.ValidatableProperties));
    }

    /// <summary>
    ///     Classifies [AsParameters] with proper EOE012/EOE013/EOE016/EOE017 diagnostics.
    /// </summary>
    private static ParameterClassificationResult ClassifyAsParameters(
        in ParameterMeta meta,
        ITypeSymbol type,
        ImmutableHashSet<string> routeParameters,
        ISymbol method,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        ErrorOrContext context,
        HttpVerb httpVerb)
    {
        // EOE017: [AsParameters] cannot be nullable
        if (meta.IsNullable)
        {
            diagnostics.Add(DiagnosticInfo.Create(Descriptors.NullableAsParametersNotSupported,
                method.Locations.FirstOrDefault() ?? Location.None, meta.Name));
            return ParameterClassificationResult.Error;
        }

        // EOE012: [AsParameters] can only be used on (non-primitive, non-enum) class or struct types.
        // `int` IS an INamedTypeSymbol — the prior `is not INamedTypeSymbol` guard let primitives slip
        // through silently. Filter by SpecialType + TypeKind to catch primitives, enums, and value types
        // that don't make sense as parameter-expansion targets.
        if (type is not INamedTypeSymbol typeSymbol
            || type.SpecialType is not SpecialType.None
            || type.TypeKind is TypeKind.Enum)
        {
            diagnostics.Add(DiagnosticInfo.Create(Descriptors.InvalidAsParametersType, method, meta.Name,
                meta.TypeFqn));
            return ParameterClassificationResult.Error;
        }

        var constructor = typeSymbol.Constructors
            .Where(static c => c.DeclaredAccessibility == Accessibility.Public && !c.IsStatic)
            .OrderByDescending(static c => c.Parameters.Length)
            .FirstOrDefault();

        // EOE013: [AsParameters] type must have an accessible constructor
        if (constructor is null)
        {
            diagnostics.Add(DiagnosticInfo.Create(Descriptors.AsParametersNoConstructor, method,
                typeSymbol.ToDisplayString()));
            return ParameterClassificationResult.Error;
        }

        // EOE016 (property-level): [AsParameters] also binds public properties per Microsoft Learn,
        // so a nested [AsParameters] on a property must be caught. The constructor-param scan below
        // catches the record/primary-constructor case; this catches the regular-property case.
        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is IPropertySymbol property &&
                ErrorOrContext.HasAttribute(property, WellKnownTypes.AsParametersAttribute))
            {
                diagnostics.Add(DiagnosticInfo.Create(Descriptors.NestedAsParametersNotSupported,
                    method.Locations.FirstOrDefault() ?? Location.None,
                    typeSymbol.ToDisplayString(),
                    property.Name));
                return ParameterClassificationResult.Error;
            }
        }

        var children = ImmutableArray.CreateBuilder<EndpointParameter>();
        foreach (var paramSymbol in constructor.Parameters)
        {
            var childMeta = CreateParameterMeta(paramSymbol);

            // EOE016 (ctor-param level): Nested [AsParameters] not supported
            if (childMeta.HasAsParameters)
            {
                diagnostics.Add(DiagnosticInfo.Create(Descriptors.NestedAsParametersNotSupported,
                    method.Locations.FirstOrDefault() ?? Location.None,
                    typeSymbol.ToDisplayString(),
                    paramSymbol.Name));
                return ParameterClassificationResult.Error;
            }

            var result = ClassifyParameter(in childMeta, paramSymbol.Type, routeParameters, method, diagnostics,
                context, httpVerb);

            if (result.IsError) return ParameterClassificationResult.Error;

            children.Add(result.Parameter);
        }

        // [AsParameters] binds public settable/init properties too (ASP.NET parity via PropertyAsParameterInfo),
        // not just constructor parameters. Properties already provided positionally by the constructor
        // (records' primary-ctor properties) are skipped so they aren't bound twice. Each remaining property
        // is classified like a top-level parameter and assigned through an object initializer at emit time —
        // which also satisfies `required` members, lifting the prior "can't bind required init-only" limitation.
        var ctorBoundNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ctorParam in constructor.Parameters)
            ctorBoundNames.Add(ctorParam.Name);

        var initProperties = ImmutableArray.CreateBuilder<EndpointParameter>();
        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IPropertySymbol property
                || property.IsStatic
                || property.IsIndexer
                || property.DeclaredAccessibility != Accessibility.Public)
                continue;

            // Must be assignable via an object initializer (public set or init accessor).
            if (property.SetMethod is not { DeclaredAccessibility: Accessibility.Public })
                continue;

            if (ctorBoundNames.Contains(property.Name))
                continue;

            var propMeta = CreateParameterMetaFromProperty(property);
            var propResult = ClassifyParameter(in propMeta, property.Type, routeParameters, method, diagnostics,
                context, httpVerb);

            if (propResult.IsError) return ParameterClassificationResult.Error;

            initProperties.Add(propResult.Parameter);
        }

        return new ParameterClassificationResult(IsError: false, new EndpointParameter(
            meta.Name,
            meta.TypeFqn,
            ParameterSource.AsParameters,
KeyName: null,
            meta.IsNullable,
            meta.IsNonNullableValueType,
IsCollection: false,
CollectionItemTypeFqn: null,
            new EquatableArray<EndpointParameter>(children.ToImmutable()),
            CustomBindingMethod.None,
            meta.RequiresValidation,
            ValidatableProperties: meta.ValidatableProperties,
            InitProperties: new EquatableArray<EndpointParameter>(initProperties.ToImmutable())));
    }
}
