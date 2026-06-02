using Microsoft.CodeAnalysis;

namespace ErrorOr.Generators;

/// <summary>
///     Partial class containing symbol-to-meta extraction for parameter binding.
///     Converts <see cref="IParameterSymbol" /> into <see cref="ParameterMeta" /> for downstream classification.
/// </summary>
public sealed partial class ErrorOrEndpointGenerator
{
    private static ParameterMeta CreateParameterMeta(
        IParameterSymbol parameter)
    {
        var type = parameter.Type;
        var typeFqn = type.GetFullyQualifiedName();

        var flags = BuildFlags(parameter, type, parameter.NullableAnnotation);
        var specialKind = DetectSpecialKind(type);

        var (isCollection, itemType, itemPrimitiveKind) = AnalyzeCollectionType(type);
        if (isCollection) flags |= ParameterFlags.Collection;

        // Determine bound name based on explicit attribute or default to parameter name
        var boundName = DetermineBoundName(parameter, flags);

        var serviceKey = flags.HasFlag(ParameterFlags.FromKeyedServices)
            ? ExtractKeyFromKeyedServiceAttribute(parameter)
            : null;

        var validatableProperties = flags.HasFlag(ParameterFlags.RequiresValidation)
            ? ErrorOrContext.CollectValidatableProperties(type)
            : default;

        return new ParameterMeta(
            parameter.Name,
            typeFqn,
            TryGetRoutePrimitiveKind(type),
            flags,
            specialKind,
            serviceKey,
            boundName,
            itemType?.GetFullyQualifiedName(),
            itemPrimitiveKind,
            DetectCustomBinding(type),
            DetectEmptyBodyBehavior(parameter),
            validatableProperties,
            FormatDefaultValue(parameter));
    }

    /// <summary>
    ///     Extracts binding metadata from a public settable/init property of an <c>[AsParameters]</c> type.
    ///     Mirrors <see cref="CreateParameterMeta" /> but reads from an <see cref="IPropertySymbol" />.
    ///     Per ASP.NET Core's <c>PropertyAsParameterInfo</c>, a property's field initializer is NOT a binding
    ///     default — optionality comes from nullability only (non-nullable property ⇒ required) — so no
    ///     default-value expression is produced here.
    /// </summary>
    private static ParameterMeta CreateParameterMetaFromProperty(IPropertySymbol property)
    {
        var type = property.Type;
        var typeFqn = type.GetFullyQualifiedName();

        var flags = BuildFlags(property, type, property.NullableAnnotation);
        var specialKind = DetectSpecialKind(type);

        var (isCollection, itemType, itemPrimitiveKind) = AnalyzeCollectionType(type);
        if (isCollection) flags |= ParameterFlags.Collection;

        var boundName = DetermineBoundName(property, flags);

        var serviceKey = flags.HasFlag(ParameterFlags.FromKeyedServices)
            ? ExtractKeyFromKeyedServiceAttribute(property)
            : null;

        return new ParameterMeta(
            property.Name,
            typeFqn,
            TryGetRoutePrimitiveKind(type),
            flags,
            specialKind,
            serviceKey,
            boundName,
            itemType?.GetFullyQualifiedName(),
            itemPrimitiveKind,
            DetectCustomBinding(type),
            DetectEmptyBodyBehavior(property),
            default);
    }

    /// <summary>
    ///     Renders a constructor/method parameter's compile-time default into a C# literal the binder
    ///     can assign when a query value is absent (making the parameter optional, per the minimal-API
    ///     rule that nullability OR a default value applies to all binding sources). Returns null when
    ///     the parameter has no explicit default or the constant is not representable as a literal.
    /// </summary>
    private static string? FormatDefaultValue(IParameterSymbol parameter)
    {
        if (!parameter.HasExplicitDefaultValue) return null;

        var value = parameter.ExplicitDefaultValue;
        if (value is null) return "null";

        if (parameter.Type is INamedTypeSymbol { TypeKind: TypeKind.Enum })
        {
            var underlying = Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatPrimitive(
                value, quoteStrings: false, useHexadecimalNumbers: false);
            if (underlying is null) return null;

            var enumFqn = parameter.Type.GetFullyQualifiedName();
            return $"({enumFqn}){underlying}";
        }

        return Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatPrimitive(
            value, quoteStrings: true, useHexadecimalNumbers: false);
    }

    private static ParameterFlags BuildFlags(ISymbol parameter, ITypeSymbol type, NullableAnnotation nullableAnnotation)
    {
        var flags = ParameterFlags.None;

        if (HasParameterAttribute(parameter, WellKnownTypes.FromBodyAttribute))
            flags |= ParameterFlags.FromBody;

        if (HasParameterAttribute(parameter, WellKnownTypes.FromRouteAttribute))
            flags |= ParameterFlags.FromRoute;

        if (HasParameterAttribute(parameter, WellKnownTypes.FromQueryAttribute))
            flags |= ParameterFlags.FromQuery;

        if (HasParameterAttribute(parameter, WellKnownTypes.FromHeaderAttribute))
            flags |= ParameterFlags.FromHeader;

        if (HasParameterAttribute(parameter, WellKnownTypes.FromFormAttribute))
            flags |= ParameterFlags.FromForm;

        if (HasParameterAttribute(parameter, WellKnownTypes.FromServicesAttribute))
            flags |= ParameterFlags.FromServices;

        if (HasParameterAttribute(parameter, WellKnownTypes.FromKeyedServicesAttribute))
            flags |= ParameterFlags.FromKeyedServices;

        if (HasParameterAttribute(parameter, WellKnownTypes.AsParametersAttribute))
            flags |= ParameterFlags.AsParameters;

        var (isNullable, isNonNullableValueType) = GetParameterNullability(type, nullableAnnotation);
        if (isNullable) flags |= ParameterFlags.Nullable;

        if (isNonNullableValueType) flags |= ParameterFlags.NonNullableValueType;

        if (ErrorOrContext.RequiresValidation(type)) flags |= ParameterFlags.RequiresValidation;

        return flags;
    }

    private static SpecialParameterKind DetectSpecialKind(ITypeSymbol type)
    {
        if (ErrorOrContext.IsHttpContext(type)) return SpecialParameterKind.HttpContext;

        if (ErrorOrContext.IsCancellationToken(type)) return SpecialParameterKind.CancellationToken;

        if (ErrorOrContext.IsFormFile(type)) return SpecialParameterKind.FormFile;

        if (ErrorOrContext.IsFormFileCollection(type)) return SpecialParameterKind.FormFileCollection;

        if (ErrorOrContext.IsFormCollection(type)) return SpecialParameterKind.FormCollection;

        if (ErrorOrContext.IsStream(type)) return SpecialParameterKind.Stream;

        return ErrorOrContext.IsPipeReader(type) ? SpecialParameterKind.PipeReader : SpecialParameterKind.None;
    }

    private static string DetermineBoundName(ISymbol parameter, ParameterFlags flags)
    {
        // Try to get explicit name from binding attribute
        if (flags.HasFlag(ParameterFlags.FromRoute))
        {
            return TryGetAttributeName(parameter, WellKnownTypes.FromRouteAttribute) ??
                   parameter.Name;
        }

        if (flags.HasFlag(ParameterFlags.FromQuery))
        {
            return TryGetAttributeName(parameter, WellKnownTypes.FromQueryAttribute) ??
                   parameter.Name;
        }

        if (flags.HasFlag(ParameterFlags.FromHeader))
        {
            return TryGetAttributeName(parameter, WellKnownTypes.FromHeaderAttribute) ??
                   parameter.Name;
        }

        if (flags.HasFlag(ParameterFlags.FromForm))
            return TryGetAttributeName(parameter, WellKnownTypes.FromFormAttribute) ?? parameter.Name;

        return parameter.Name;
    }

    private readonly struct AttributeNameMatcher
    {
        private readonly string _fullName;
        private readonly string _shortName;
        private readonly string _shortNameWithoutAttr;

        public AttributeNameMatcher(string fullName)
        {
            _fullName = fullName;
            var lastDot = fullName.LastIndexOf('.');
            _shortName = lastDot >= 0 ? fullName[(lastDot + 1)..] : fullName;
            _shortNameWithoutAttr =
                _shortName.EndsWithOrdinal("Attribute") ? _shortName[..^"Attribute".Length] : _shortName;
        }

        public bool IsMatch(ISymbol? attributeClass)
        {
            if (attributeClass is not ITypeSymbol typeSymbol) return false;

            var display = typeSymbol.GetFullyQualifiedName();

            if (display.StartsWithOrdinal("global::")) display = display[8..];

            // Strict match: Must match FQN or ShortName (if FQN not available/provided)
            // We drop loose EndsWith matching to avoid collisions
            return display == _fullName ||
                   display == _shortName ||
                   display == _shortNameWithoutAttr;
        }
    }
}
