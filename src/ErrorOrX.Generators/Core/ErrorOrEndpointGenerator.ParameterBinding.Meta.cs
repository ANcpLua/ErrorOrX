using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;

namespace ErrorOr.Generators;

/// <summary>
///     Partial class containing symbol-to-meta extraction for parameter binding.
///     Converts <see cref="IParameterSymbol"/> into <see cref="ParameterMeta"/> for downstream classification.
/// </summary>
public sealed partial class ErrorOrEndpointGenerator
{
    private static ParameterMeta CreateParameterMeta(
        IParameterSymbol parameter,
        ErrorOrContext context)
    {
        var type = parameter.Type;
        var typeFqn = type.GetFullyQualifiedName();

        var flags = BuildFlags(parameter, type, context);
        var specialKind = DetectSpecialKind(type, context);

        var (isCollection, itemType, itemPrimitiveKind) = AnalyzeCollectionType(type, context);
        if (isCollection) flags |= ParameterFlags.Collection;

        // Determine bound name based on explicit attribute or default to parameter name
        var boundName = DetermineBoundName(parameter, flags, context);

        var serviceKey = flags.HasFlag(ParameterFlags.FromKeyedServices)
            ? ExtractKeyFromKeyedServiceAttribute(parameter)
            : null;

        var validatableProperties = flags.HasFlag(ParameterFlags.RequiresValidation)
            ? ErrorOrContext.CollectValidatableProperties(type)
            : default;

        return new ParameterMeta(
            parameter.Name,
            typeFqn,
            TryGetRoutePrimitiveKind(type, context),
            flags,
            specialKind,
            serviceKey,
            boundName,
            itemType?.GetFullyQualifiedName(),
            itemPrimitiveKind,
            DetectCustomBinding(type, context),
            DetectEmptyBodyBehavior(parameter),
            validatableProperties);
    }

    private static ParameterFlags BuildFlags(IParameterSymbol parameter, ITypeSymbol type, ErrorOrContext context)
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

        var (isNullable, isNonNullableValueType) = GetParameterNullability(type, parameter.NullableAnnotation);
        if (isNullable) flags |= ParameterFlags.Nullable;

        if (isNonNullableValueType) flags |= ParameterFlags.NonNullableValueType;

        if (ErrorOrContext.RequiresValidation(type)) flags |= ParameterFlags.RequiresValidation;

        return flags;
    }

    private static SpecialParameterKind DetectSpecialKind(ITypeSymbol type, ErrorOrContext context)
    {
        if (ErrorOrContext.IsHttpContext(type)) return SpecialParameterKind.HttpContext;

        if (ErrorOrContext.IsCancellationToken(type)) return SpecialParameterKind.CancellationToken;

        if (ErrorOrContext.IsFormFile(type)) return SpecialParameterKind.FormFile;

        if (ErrorOrContext.IsFormFileCollection(type)) return SpecialParameterKind.FormFileCollection;

        if (ErrorOrContext.IsFormCollection(type)) return SpecialParameterKind.FormCollection;

        if (ErrorOrContext.IsStream(type)) return SpecialParameterKind.Stream;

        return ErrorOrContext.IsPipeReader(type) ? SpecialParameterKind.PipeReader : SpecialParameterKind.None;
    }

    private static string DetermineBoundName(ISymbol parameter, ParameterFlags flags, ErrorOrContext context)
    {
        // Try to get explicit name from binding attribute
        if (flags.HasFlag(ParameterFlags.FromRoute))
        {
            return TryGetAttributeName(parameter, context, WellKnownTypes.FromRouteAttribute) ??
                   parameter.Name;
        }

        if (flags.HasFlag(ParameterFlags.FromQuery))
        {
            return TryGetAttributeName(parameter, context, WellKnownTypes.FromQueryAttribute) ??
                   parameter.Name;
        }

        if (flags.HasFlag(ParameterFlags.FromHeader))
        {
            return TryGetAttributeName(parameter, context, WellKnownTypes.FromHeaderAttribute) ??
                   parameter.Name;
        }

        if (flags.HasFlag(ParameterFlags.FromForm))
            return TryGetAttributeName(parameter, context, WellKnownTypes.FromFormAttribute) ?? parameter.Name;

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
