using Microsoft.CodeAnalysis;

namespace ErrorOr.Generators;

/// <summary>
///     Value-equatable descriptor of consumer-compilation capabilities.
///     Holds only presence booleans for optional packages so it can flow through incremental pipelines
///     without invalidating downstream steps when Roslyn supplies a new Compilation for unchanged source.
///     All symbol matching is performed by metadata-name comparison; no live symbol references are retained.
/// </summary>
internal sealed class ErrorOrContext : IEquatable<ErrorOrContext>
{
    public ErrorOrContext(Compilation compilation)
        : this(
            compilation.GetBestTypeByMetadataName(WellKnownTypes.IValidatableInfoResolver) is not null,
            compilation.GetBestTypeByMetadataName(WellKnownTypes.ApiVersionAttribute) is not null)
    {
    }

    private ErrorOrContext(bool hasValidationResolverSupport, bool hasApiVersioningSupport)
    {
        HasValidationResolverSupport = hasValidationResolverSupport;
        HasApiVersioningSupport = hasApiVersioningSupport;
    }

    /// <summary>
    ///     True if the Microsoft.Extensions.Validation package is referenced (.NET 10 validation infrastructure).
    /// </summary>
    public bool HasValidationResolverSupport { get; }

    /// <summary>
    ///     True if the Asp.Versioning.Http package is referenced.
    /// </summary>
    public bool HasApiVersioningSupport { get; }

    public bool Equals(ErrorOrContext? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return HasValidationResolverSupport == other.HasValidationResolverSupport &&
               HasApiVersioningSupport == other.HasApiVersioningSupport;
    }

    public static bool MatchesType(ISymbol? symbol, string metadataName)
    {
        return symbol is ITypeSymbol typeSymbol && MatchesMetadataName(typeSymbol, metadataName);
    }

    public static bool MatchesConstructedFrom(ITypeSymbol? typeSymbol, string metadataName)
    {
        return typeSymbol is INamedTypeSymbol namedType &&
               MatchesMetadataName(namedType.OriginalDefinition, metadataName);
    }

    public static bool HasAttribute(ISymbol symbol, string attributeMetadataName)
    {
        foreach (var attribute in symbol.GetAttributes())
            if (MatchesType(attribute.AttributeClass, attributeMetadataName))
                return true;

        return false;
    }

    public static bool IsOrImplements(ITypeSymbol? type, string metadataName)
    {
        type = type?.UnwrapNullable();
        if (type is null) return false;

        if (MatchesType(type, metadataName)) return true;

        foreach (var interfaceType in type.AllInterfaces)
            if (MatchesType(interfaceType, metadataName))
                return true;

        return false;
    }

    public static bool IsOrInheritsFrom(ITypeSymbol? type, string metadataName)
    {
        type = type?.UnwrapNullable();
        if (type is null) return false;

        for (var current = type; current is not null; current = current.BaseType)
            if (MatchesType(current, metadataName))
                return true;

        return false;
    }

    /// <summary>Checks if the type implements IFormFile.</summary>
    public static bool IsFormFile(ITypeSymbol? type)
    {
        return IsOrImplements(type, WellKnownTypes.FormFile);
    }

    /// <summary>Checks if the type is IFormFileCollection or IReadOnlyList&lt;IFormFile&gt;.</summary>
    public static bool IsFormFileCollection(ITypeSymbol? type)
    {
        type = type?.UnwrapNullable();
        if (type is null) return false;

        if (MatchesType(type, WellKnownTypes.FormFileCollection))
            return true;

        return type is INamedTypeSymbol { IsGenericType: true } named &&
               MatchesConstructedFrom(named.ConstructedFrom, WellKnownTypes.IReadOnlyListT) &&
               IsFormFile(named.TypeArguments[0]);
    }

    /// <summary>Checks if the type implements IFormCollection.</summary>
    public static bool IsFormCollection(ITypeSymbol? type)
    {
        return IsOrImplements(type, WellKnownTypes.FormCollection);
    }

    /// <summary>Checks if the type is or inherits from HttpContext.</summary>
    public static bool IsHttpContext(ITypeSymbol? type)
    {
        return IsOrInheritsFrom(type, WellKnownTypes.HttpContext);
    }

    /// <summary>Checks if the type is or inherits from System.IO.Stream.</summary>
    public static bool IsStream(ITypeSymbol? type)
    {
        return IsOrInheritsFrom(type, WellKnownTypes.Stream);
    }

    /// <summary>Checks if the type is or inherits from System.IO.Pipelines.PipeReader.</summary>
    public static bool IsPipeReader(ITypeSymbol? type)
    {
        return IsOrInheritsFrom(type, WellKnownTypes.PipeReader);
    }

    /// <summary>Checks if the type is System.Threading.CancellationToken.</summary>
    public static bool IsCancellationToken(ITypeSymbol? type)
    {
        return MatchesType(type?.UnwrapNullable(), WellKnownTypes.CancellationToken);
    }

    /// <summary>Checks if the type is System.Reflection.ParameterInfo.</summary>
    public static bool IsParameterInfo(ITypeSymbol? type)
    {
        return MatchesType(type?.UnwrapNullable(), WellKnownTypes.ParameterInfo);
    }

    /// <summary>
    ///     Determines if a type requires BCL validation:
    ///     1) any property (including inherited) carries an attribute deriving from ValidationAttribute, or
    ///     2) a constructor parameter with a matching property carries such an attribute (record positional syntax), or
    ///     3) the type implements IValidatableObject.
    /// </summary>
    public static bool RequiresValidation(ITypeSymbol? type)
    {
        if (type is null) return false;

        if (type.SpecialType is not SpecialType.None ||
            type.TypeKind is TypeKind.Enum or TypeKind.Interface)
            return false;

        if (IsOrImplements(type, WellKnownTypes.IValidatableObject)) return true;

        var current = type;
        while (current is INamedTypeSymbol namedType)
        {
            foreach (var member in namedType.GetMembers())
            {
                if (member is not IPropertySymbol property) continue;

                if (HasValidationAttribute(property)) return true;
            }

            if (HasValidationAttributeOnConstructorParam(namedType)) return true;

            current = namedType.BaseType;
        }

        return false;
    }

    /// <summary>
    ///     Collects validatable property descriptors for a type.
    ///     For each property, checks both property-level and constructor parameter-level attributes
    ///     (records place validation attributes on the constructor parameter, not the property).
    /// </summary>
    public static EquatableArray<ValidatablePropertyDescriptor> CollectValidatableProperties(ITypeSymbol? type)
    {
        if (type is null) return default;

        var properties = ImmutableArray.CreateBuilder<ValidatablePropertyDescriptor>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var current = type;
        while (current is INamedTypeSymbol namedType)
        {
            foreach (var member in namedType.GetMembers())
            {
                if (member is not IPropertySymbol property) continue;

                if (!seen.Add(property.Name)) continue;

                var propertyAttrs = CollectValidationAttributes(property);
                var ctorParam = FindMatchingConstructorParam(namedType, property.Name);
                var paramAttrs = ctorParam is not null
                    ? CollectValidationAttributes(ctorParam)
                    : default;

                var attrs = MergeValidationAttributes(propertyAttrs, paramAttrs);
                if (attrs.IsDefaultOrEmpty) continue;

                properties.Add(new ValidatablePropertyDescriptor(
                    property.Name,
                    property.Type.GetFullyQualifiedName(),
                    property.Name,
                    attrs));
            }

            current = namedType.BaseType;
        }

        return properties.Count > 0
            ? new EquatableArray<ValidatablePropertyDescriptor>(properties.ToImmutable())
            : default;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ErrorOrContext);
    }

    public override int GetHashCode()
    {
        var hash = HasValidationResolverSupport ? 1 : 0;
        hash = (hash * 397) ^ (HasApiVersioningSupport ? 1 : 0);
        return hash;
    }

    /// <summary>
    ///     Creates an incremental provider that resolves ErrorOrContext once per compilation.
    ///     Value equality on presence booleans prevents invalidation when Roslyn
    ///     supplies a new Compilation instance for unchanged source.
    /// </summary>
    public static IncrementalValueProvider<ErrorOrContext> CreateProvider(
        IncrementalGeneratorInitializationContext context)
    {
        return context.CompilationProvider
            .Select(static (compilation, _) => new ErrorOrContext(compilation));
    }

    private static bool HasValidationAttribute(ISymbol symbol)
    {
        foreach (var attribute in symbol.GetAttributes())
            if (IsOrInheritsFrom(attribute.AttributeClass, WellKnownTypes.ValidationAttribute))
                return true;

        return false;
    }

    private static bool HasValidationAttributeOnConstructorParam(INamedTypeSymbol namedType)
    {
        foreach (var ctor in namedType.InstanceConstructors)
        foreach (var param in ctor.Parameters)
        {
            if (!HasValidationAttribute(param)) continue;

            foreach (var member in namedType.GetMembers(param.Name))
                if (member is IPropertySymbol)
                    return true;
        }

        return false;
    }

    private static IParameterSymbol? FindMatchingConstructorParam(INamedTypeSymbol type, string propertyName)
    {
        foreach (var ctor in type.InstanceConstructors)
        foreach (var param in ctor.Parameters)
            if (string.Equals(param.Name, propertyName, StringComparison.Ordinal))
                return param;

        return null;
    }

    private static EquatableArray<ValidatableAttributeInfo> MergeValidationAttributes(
        EquatableArray<ValidatableAttributeInfo> a, EquatableArray<ValidatableAttributeInfo> b)
    {
        if (a.IsDefaultOrEmpty) return b;
        if (b.IsDefaultOrEmpty) return a;
        return new EquatableArray<ValidatableAttributeInfo>([.. a.AsImmutableArray(), .. b.AsImmutableArray()]);
    }

    private static EquatableArray<ValidatableAttributeInfo> CollectValidationAttributes(ISymbol symbol)
    {
        var attrs = ImmutableArray.CreateBuilder<ValidatableAttributeInfo>();
        foreach (var attrData in symbol.GetAttributes())
        {
            if (attrData.AttributeClass is null) continue;

            if (!IsOrInheritsFrom(attrData.AttributeClass, WellKnownTypes.ValidationAttribute)) continue;

            var ctorArgs = ImmutableArray.CreateBuilder<string>();
            foreach (var arg in attrData.ConstructorArguments) ctorArgs.Add(TypedConstantToLiteral(arg));

            var namedArgs = ImmutableArray.CreateBuilder<NamedArgLiteral>();
            foreach (var namedArg in attrData.NamedArguments)
                namedArgs.Add(new NamedArgLiteral(namedArg.Key, TypedConstantToLiteral(namedArg.Value)));

            attrs.Add(new ValidatableAttributeInfo(
                attrData.AttributeClass.GetFullyQualifiedName(),
                new EquatableArray<string>(ctorArgs.ToImmutable()),
                new EquatableArray<NamedArgLiteral>(namedArgs.ToImmutable())));
        }

        return attrs.Count > 0
            ? new EquatableArray<ValidatableAttributeInfo>(attrs.ToImmutable())
            : default;
    }

    private static string TypedConstantToLiteral(TypedConstant constant)
    {
        if (constant.Kind == TypedConstantKind.Array)
        {
            var elements = constant.Values;
            return $"new[] {{ {string.Join(", ", elements.Select(TypedConstantToLiteral))} }}";
        }

        if (constant.Value is null) return "null";

        return constant switch
        {
            { Kind: TypedConstantKind.Type, Value: ITypeSymbol typeSymbol } =>
                $"typeof({typeSymbol.GetFullyQualifiedName()})",
            { Kind: TypedConstantKind.Enum, Type: not null } =>
                $"({constant.Type.GetFullyQualifiedName()}){constant.Value}",
            _ => constant.Value switch
            {
                string s => $"\"{EscapeStringLiteral(s)}\"",
                bool b => b ? "true" : "false",
                char c => $"'{EscapeCharLiteral(c)}'",
                float f => f.ToString(CultureInfo.InvariantCulture) + "f",
                double d => d.ToString(CultureInfo.InvariantCulture) + "d",
                decimal m => m.ToString(CultureInfo.InvariantCulture) + "m",
                long l => $"{l}L",
                ulong ul => $"{ul}UL",
                uint ui => $"{ui}U",
                _ => Convert.ToString(constant.Value, CultureInfo.InvariantCulture) ?? "null"
            }
        };
    }

    private static string EscapeStringLiteral(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
            switch (c)
            {
                case '\\': sb.Append(@"\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append(@"\n"); break;
                case '\r': sb.Append(@"\r"); break;
                case '\t': sb.Append(@"\t"); break;
                case '\0': sb.Append(@"\0"); break;
                case '\a': sb.Append(@"\a"); break;
                case '\b': sb.Append(@"\b"); break;
                case '\f': sb.Append(@"\f"); break;
                case '\v': sb.Append(@"\v"); break;
                default:
                    if (char.IsControl(c) || c > 127)
                        sb.Append($"\\u{(int)c:X4}");
                    else
                        sb.Append(c);

                    break;
            }

        return sb.ToString();
    }

    private static string EscapeCharLiteral(char c)
    {
        return c switch
        {
            '\\' => @"\\",
            '\'' => @"\'",
            '\n' => @"\n",
            '\r' => @"\r",
            '\t' => @"\t",
            '\0' => @"\0",
            '\a' => @"\a",
            '\b' => @"\b",
            '\f' => @"\f",
            '\v' => @"\v",
            _ when char.IsControl(c) || c > 127 => $"\\u{(int)c:X4}",
            _ => c.ToString()
        };
    }

    private static bool MatchesMetadataName(ITypeSymbol typeSymbol, string metadataName)
    {
        var lastDot = metadataName.LastIndexOf('.');
        var shortName = lastDot >= 0 ? metadataName[(lastDot + 1)..] : metadataName;
        var shortNameWithoutAttribute =
            shortName.EndsWithOrdinal("Attribute") ? shortName[..^"Attribute".Length] : shortName;
        var fullMetadataName = GetFullMetadataName(typeSymbol);

        return fullMetadataName == metadataName ||
               typeSymbol.MetadataName == shortName ||
               typeSymbol.MetadataName == shortNameWithoutAttribute ||
               typeSymbol.Name == shortName ||
               typeSymbol.Name == shortNameWithoutAttribute;
    }

    private static string GetFullMetadataName(ITypeSymbol typeSymbol)
    {
        if (typeSymbol.ContainingType is { } containingType)
            return $"{GetFullMetadataName(containingType)}+{typeSymbol.MetadataName}";

        return typeSymbol.ContainingNamespace is { IsGlobalNamespace: false } containingNamespace
            ? $"{containingNamespace.ToDisplayString()}.{typeSymbol.MetadataName}"
            : typeSymbol.MetadataName;
    }
}
