using System.Runtime.CompilerServices;
using ANcpLua.Roslyn.Utilities.Contexts;
using Microsoft.CodeAnalysis;

namespace ErrorOr.Generators;

internal sealed class ErrorOrContext : IEquatable<ErrorOrContext>
{
    public ErrorOrContext(Compilation compilation)
    {
        _compilation = compilation;
        Awaitable = new AwaitableContext(compilation);
    }

    private readonly Compilation _compilation;

    public AwaitableContext Awaitable { get; }

    /// <summary>
    ///     Returns true if the Microsoft.Extensions.Validation package is referenced,
    ///     meaning the consumer supports .NET 10 validation infrastructure.
    /// </summary>
    public bool HasValidationResolverSupport => GetType(WellKnownTypes.IValidatableInfoResolver) is not null;

    /// <summary>
    ///     Returns true if the Asp.Versioning.Http package is referenced.
    /// </summary>
    public bool HasApiVersioningSupport => GetType(WellKnownTypes.ApiVersionAttribute) is not null;

    public bool MatchesType(ISymbol? symbol, string metadataName)
    {
        if (symbol is not ITypeSymbol typeSymbol) return false;

        if (GetType(metadataName) is { } resolvedType) return typeSymbol.IsEqualTo(resolvedType);

        return MatchesMetadataName(typeSymbol, metadataName);
    }

    public bool MatchesConstructedFrom(ITypeSymbol? typeSymbol, string metadataName)
    {
        if (typeSymbol is not INamedTypeSymbol namedType) return false;

        if (GetConstructedFrom(metadataName) is { } constructedFrom) return namedType.IsEqualTo(constructedFrom);

        return MatchesMetadataName(namedType.OriginalDefinition, metadataName);
    }

    public bool HasAttribute(ISymbol symbol, string attributeMetadataName)
    {
        foreach (var attribute in symbol.GetAttributes())
            if (MatchesType(attribute.AttributeClass, attributeMetadataName))
                return true;

        return false;
    }

    public bool IsOrImplements(ITypeSymbol? type, string metadataName)
    {
        type = type?.UnwrapNullable();
        if (type is null) return false;

        if (GetType(metadataName) is { } resolvedType && type.IsOrImplements(resolvedType))
            return true;

        if (MatchesType(type, metadataName)) return true;

        foreach (var interfaceType in type.AllInterfaces)
            if (MatchesType(interfaceType, metadataName))
                return true;

        return false;
    }

    public bool IsOrInheritsFrom(ITypeSymbol? type, string metadataName)
    {
        type = type?.UnwrapNullable();
        if (type is null) return false;

        if (GetType(metadataName) is { } resolvedType && type.IsOrInheritsFrom(resolvedType))
            return true;

        for (var current = type; current is not null; current = current.BaseType)
            if (MatchesType(current, metadataName))
                return true;

        return false;
    }

    public bool Equals(ErrorOrContext? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return ReferenceEquals(_compilation, other._compilation);
    }

    public override bool Equals(object? obj) => Equals(obj as ErrorOrContext);

    public override int GetHashCode() => RuntimeHelpers.GetHashCode(_compilation);

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
    ///     2. Has a constructor parameter with a matching property and a ValidationAttribute (records), OR
    ///     3. Implements IValidatableObject
    /// </summary>
    public bool RequiresValidation(ITypeSymbol? type)
    {
        var validationAttribute = GetType(WellKnownTypes.ValidationAttribute);
        if (type is null || validationAttribute is null) return false;

        if (type.SpecialType is not SpecialType.None ||
            type.TypeKind is TypeKind.Enum or TypeKind.Interface)
            return false;

        if (GetType(WellKnownTypes.IValidatableObject) is { } validatableObject &&
            type.AllInterfaces.Any(i => i.IsEqualTo(validatableObject)))
            return true;

        var current = type;
        while (current is INamedTypeSymbol namedType)
        {
            foreach (var member in namedType.GetMembers())
            {
                if (member is not IPropertySymbol property) continue;

                if (property.HasAttribute(validationAttribute)) return true;
            }

            // In modern .NET, ValidationAttribute targets Parameter, so for records
            // [Required] stays on the constructor parameter, not the synthesized property.
            if (HasValidationAttributeOnConstructorParam(namedType, validationAttribute)) return true;

            current = namedType.BaseType;
        }

        return false;
    }

    /// <summary>
    ///     Checks if any constructor parameter that corresponds to a property has a validation attribute.
    ///     Handles records where attributes like [Required] target the parameter, not the property.
    /// </summary>
    private static bool HasValidationAttributeOnConstructorParam(
        INamedTypeSymbol namedType,
        INamedTypeSymbol validationAttribute)
    {
        foreach (var ctor in namedType.InstanceConstructors)
        foreach (var param in ctor.Parameters)
        {
            if (!param.HasAttribute(validationAttribute)) continue;

            // Only count if there's a matching property (record positional parameter pattern)
            foreach (var member in namedType.GetMembers(param.Name))
                if (member is IPropertySymbol)
                    return true;
        }

        return false;
    }

    /// <summary>
    ///     Collects validatable property descriptors for a type.
    ///     For each property, checks both property-level and constructor parameter-level attributes
    ///     (records place validation attributes on the constructor parameter, not the property).
    /// </summary>
    public EquatableArray<ValidatablePropertyDescriptor> CollectValidatableProperties(ITypeSymbol? type)
    {
        var validationAttribute = GetType(WellKnownTypes.ValidationAttribute);
        if (type is null || validationAttribute is null) return default;

        var properties = ImmutableArray.CreateBuilder<ValidatablePropertyDescriptor>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var current = type;
        while (current is INamedTypeSymbol namedType)
        {
            foreach (var member in namedType.GetMembers())
            {
                if (member is not IPropertySymbol property) continue;

                // Skip properties already collected from derived type (handles overrides/hides)
                if (!seen.Add(property.Name)) continue;

                var propertyAttrs = CollectValidationAttributes(property, validationAttribute);
                var ctorParam = FindMatchingConstructorParam(namedType, property.Name);
                var paramAttrs = ctorParam is not null
                    ? CollectValidationAttributes(ctorParam, validationAttribute)
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

    private static EquatableArray<ValidatableAttributeInfo> CollectValidationAttributes(
        ISymbol property,
        INamedTypeSymbol validationAttribute)
    {
        var attrs = ImmutableArray.CreateBuilder<ValidatableAttributeInfo>();
        foreach (var attrData in property.GetAttributes())
        {
            if (attrData.AttributeClass is null) continue;

            if (!attrData.AttributeClass.IsOrInheritsFrom(validationAttribute)) continue;

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

    /// <summary>Checks if the type implements IFormFile.</summary>
    public bool IsFormFile(ITypeSymbol? type)
    {
        return IsOrImplements(type, WellKnownTypes.FormFile);
    }

    /// <summary>Checks if the type is IFormFileCollection or IReadOnlyList&lt;IFormFile&gt;.</summary>
    public bool IsFormFileCollection(ITypeSymbol? type)
    {
        type = type?.UnwrapNullable();
        if (type is null) return false;

        if (MatchesType(type, WellKnownTypes.FormFileCollection))
            return true;

        if (type is INamedTypeSymbol { IsGenericType: true } named &&
            MatchesConstructedFrom(named.ConstructedFrom, WellKnownTypes.IReadOnlyListT))
            if (IsFormFile(named.TypeArguments[0]))
                return true;

        return false;
    }

    /// <summary>Checks if the type implements IFormCollection.</summary>
    public bool IsFormCollection(ITypeSymbol? type)
    {
        return IsOrImplements(type, WellKnownTypes.FormCollection);
    }

    /// <summary>Checks if the type is or inherits from HttpContext.</summary>
    public bool IsHttpContext(ITypeSymbol? type)
    {
        return IsOrInheritsFrom(type, WellKnownTypes.HttpContext);
    }

    /// <summary>Checks if the type is or inherits from System.IO.Stream.</summary>
    public bool IsStream(ITypeSymbol? type)
    {
        return IsOrInheritsFrom(type, WellKnownTypes.Stream);
    }

    /// <summary>Checks if the type is or inherits from System.IO.Pipelines.PipeReader.</summary>
    public bool IsPipeReader(ITypeSymbol? type)
    {
        return IsOrInheritsFrom(type, WellKnownTypes.PipeReader);
    }

    /// <summary>Checks if the type is System.Threading.CancellationToken.</summary>
    public bool IsCancellationToken(ITypeSymbol? type)
    {
        return MatchesType(type?.UnwrapNullable(), WellKnownTypes.CancellationToken);
    }

    /// <summary>Checks if the type is System.Reflection.ParameterInfo.</summary>
    public bool IsParameterInfo(ITypeSymbol? type)
    {
        return MatchesType(type?.UnwrapNullable(), WellKnownTypes.ParameterInfo);
    }

    private INamedTypeSymbol? GetType(string metadataName) => _compilation.GetBestTypeByMetadataName(metadataName);

    private INamedTypeSymbol? GetConstructedFrom(string metadataName) => GetType(metadataName)?.ConstructedFrom;

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
        if (typeSymbol.ContainingType is ITypeSymbol containingType)
            return $"{GetFullMetadataName(containingType)}+{typeSymbol.MetadataName}";

        return typeSymbol.ContainingNamespace is { IsGlobalNamespace: false } containingNamespace
            ? $"{containingNamespace.ToDisplayString()}.{typeSymbol.MetadataName}"
            : typeSymbol.MetadataName;
    }
}
