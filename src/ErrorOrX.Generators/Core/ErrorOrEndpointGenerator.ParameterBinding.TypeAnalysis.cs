using System.Text.RegularExpressions;
using ANcpLua.Roslyn.Utilities.Matching;
using Microsoft.CodeAnalysis;
using SymbolMatch = ANcpLua.Roslyn.Utilities.Matching.Match;

namespace ErrorOr.Generators;

public sealed partial class ErrorOrEndpointGenerator
{
    /// <summary>
    ///     Pattern matchers for DI service detection using ANcpLua.Roslyn.Utilities.
    ///     These patterns identify types that should be resolved from DI container.
    /// </summary>
    private static readonly TypeMatcher s_serviceNameMatcher = SymbolMatch.Type()
        .Where(static t => t.Name.EndsWithOrdinal("Service") ||
                           t.Name.EndsWithOrdinal("Repository") ||
                           t.Name.EndsWithOrdinal("Handler") ||
                           t.Name.EndsWithOrdinal("Manager") ||
                           t.Name.EndsWithOrdinal("Provider") ||
                           t.Name.EndsWithOrdinal("Factory") ||
                           t.Name.EndsWithOrdinal("Client"));

    private static readonly TypeMatcher s_dbContextMatcher = SymbolMatch.Type()
        .Where(static t => t.Name.EndsWithOrdinal("Context") &&
                           (t.Name.Contains("Db") || t.Name.StartsWithOrdinal("Db")));

    private static EmptyBodyBehavior DetectEmptyBodyBehavior(ISymbol parameter)
    {
        return parameter.HasAttributeByShortName("AllowEmptyBody")
            ? EmptyBodyBehavior.Allow
            : EmptyBodyBehavior.Default;
    }

    private static RoutePrimitiveKind? TryGetRoutePrimitiveKind(ITypeSymbol type)
    {
        type = type.UnwrapNullable();

        return type.SpecialType switch
        {
            SpecialType.System_String => RoutePrimitiveKind.String,
            SpecialType.System_Int32 => RoutePrimitiveKind.Int32,
            SpecialType.System_Int64 => RoutePrimitiveKind.Int64,
            SpecialType.System_Int16 => RoutePrimitiveKind.Int16,
            SpecialType.System_Byte => RoutePrimitiveKind.Byte,
            SpecialType.System_SByte => RoutePrimitiveKind.SByte,
            SpecialType.System_UInt32 => RoutePrimitiveKind.UInt32,
            SpecialType.System_UInt64 => RoutePrimitiveKind.UInt64,
            SpecialType.System_UInt16 => RoutePrimitiveKind.UInt16,
            SpecialType.System_Boolean => RoutePrimitiveKind.Boolean,
            SpecialType.System_Decimal => RoutePrimitiveKind.Decimal,
            SpecialType.System_Double => RoutePrimitiveKind.Double,
            SpecialType.System_Single => RoutePrimitiveKind.Single,
            _ => TryGetRoutePrimitiveKindBySymbol(type)
        };
    }

    private static RoutePrimitiveKind? TryGetRoutePrimitiveKindBySymbol(ISymbol type)
    {
        if (ErrorOrContext.MatchesType(type, WellKnownTypes.Guid)) return RoutePrimitiveKind.Guid;

        if (ErrorOrContext.MatchesType(type, WellKnownTypes.DateTime)) return RoutePrimitiveKind.DateTime;

        if (ErrorOrContext.MatchesType(type, WellKnownTypes.DateTimeOffset))
            return RoutePrimitiveKind.DateTimeOffset;

        if (ErrorOrContext.MatchesType(type, WellKnownTypes.DateOnly)) return RoutePrimitiveKind.DateOnly;

        if (ErrorOrContext.MatchesType(type, WellKnownTypes.TimeOnly)) return RoutePrimitiveKind.TimeOnly;

        if (ErrorOrContext.MatchesType(type, WellKnownTypes.TimeSpan)) return RoutePrimitiveKind.TimeSpan;

        return null;
    }

    private static CustomBindingMethod DetectCustomBinding(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType || IsPrimitiveOrWellKnownType(namedType))
            return CustomBindingMethod.None;

        if (ImplementsBindableInterface(namedType)) return CustomBindingMethod.Bindable;

        var bindAsyncMethod = DetectBindAsyncMethod(namedType);
        return bindAsyncMethod != CustomBindingMethod.None ? bindAsyncMethod : DetectTryParseMethod(namedType);
    }

    private static bool IsPrimitiveOrWellKnownType(ITypeSymbol type)
    {
        type = type.UnwrapNullable();

        // Check for primitive types (int, string, bool, etc.)
        if (type.SpecialType is not SpecialType.None) return true;

        // Check for well-known non-primitive types that have built-in conversions
        return TryGetRoutePrimitiveKindBySymbol(type) is not null;
    }

    private static bool ImplementsBindableInterface(ITypeSymbol type)
    {
        return ErrorOrContext.IsOrImplements(type, WellKnownTypes.BindableFromHttpContext);
    }

    private static CustomBindingMethod DetectBindAsyncMethod(INamespaceOrTypeSymbol type)
    {
        foreach (var member in type.GetMembers("BindAsync"))
        {
            var result = ClassifyBindAsyncMember(member);
            if (result != CustomBindingMethod.None) return result;
        }

        return CustomBindingMethod.None;
    }

    private static CustomBindingMethod ClassifyBindAsyncMember(ISymbol member)
    {
        if (member is not IMethodSymbol { IsStatic: true, ReturnsVoid: false } method ||
            !method.ReturnType.IsTaskType() || method.Parameters.Length < 1 ||
            !ErrorOrContext.IsHttpContext(method.Parameters[0].Type))
        {
            return CustomBindingMethod.None;
        }

        if (method.Parameters.Length >= 2 && ErrorOrContext.IsParameterInfo(method.Parameters[1].Type))
            return CustomBindingMethod.BindAsyncWithParam;

        return CustomBindingMethod.BindAsync;
    }

    private static CustomBindingMethod DetectTryParseMethod(INamespaceOrTypeSymbol type)
    {
        foreach (var member in type.GetMembers("TryParse"))
        {
            var result = ClassifyTryParseMember(member);
            if (result != CustomBindingMethod.None) return result;
        }

        return CustomBindingMethod.None;
    }

    private static CustomBindingMethod ClassifyTryParseMember(ISymbol member)
    {
        if (member is not IMethodSymbol { IsStatic: true, ReturnType.SpecialType: SpecialType.System_Boolean } method ||
            method.Parameters.Length < 2 || !IsStringOrCharSpan(method.Parameters[0].Type) ||
            method.Parameters[^1].RefKind != RefKind.Out)
        {
            return CustomBindingMethod.None;
        }

        if (method.Parameters.Length >= 3)
        {
            for (var i = 1; i < method.Parameters.Length - 1; i++)
            {
                if (IsFormatProvider(method.Parameters[i].Type))
                    return CustomBindingMethod.TryParseWithFormat;
            }
        }

        return CustomBindingMethod.TryParse;
    }

    private static bool IsStringOrCharSpan(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String) return true;

        if (type is INamedTypeSymbol { IsGenericType: true } named)
        {
            return ErrorOrContext.MatchesConstructedFrom(named.ConstructedFrom, WellKnownTypes.ReadOnlySpanT) &&
                   named.TypeArguments is [{ SpecialType: SpecialType.System_Char }];
        }

        return false;
    }

    private static bool IsFormatProvider(ITypeSymbol type)
    {
        type = type.UnwrapNullable();
        return ErrorOrContext.MatchesType(type, WellKnownTypes.IFormatProvider);
    }

    private static (bool IsCollection, ITypeSymbol? ItemType, RoutePrimitiveKind? Kind) AnalyzeCollectionType(
        ITypeSymbol type)
    {
        type = type.UnwrapNullable();
        if (type.SpecialType == SpecialType.System_String) return (false, null, null);

        ITypeSymbol? itemType = null;
        if (type is IArrayTypeSymbol arrayType)
        {
            itemType = arrayType.ElementType;
        }
        else if (type is INamedTypeSymbol { IsGenericType: true } named)
        {
            var origin = named.ConstructedFrom;
            if (IsWellKnownCollection(origin)) itemType = named.TypeArguments[0];
        }

        return itemType is not null
            ? (true, itemType, TryGetRoutePrimitiveKind(itemType))
            : (false, null, null);
    }

    private static bool IsWellKnownCollection(ISymbol origin)
    {
        return ErrorOrContext.MatchesConstructedFrom(origin as ITypeSymbol, WellKnownTypes.ListT) ||
               ErrorOrContext.MatchesConstructedFrom(origin as ITypeSymbol, WellKnownTypes.IListT) ||
               ErrorOrContext.MatchesConstructedFrom(origin as ITypeSymbol, WellKnownTypes.IEnumerableT) ||
               ErrorOrContext.MatchesConstructedFrom(origin as ITypeSymbol, WellKnownTypes.IReadOnlyListT) ||
               ErrorOrContext.MatchesConstructedFrom(origin as ITypeSymbol, WellKnownTypes.ICollectionT) ||
               ErrorOrContext.MatchesConstructedFrom(origin as ITypeSymbol, WellKnownTypes.HashSetT);
    }

    private static (bool IsNullable, bool IsNonNullableValueType) GetParameterNullability(
        ITypeSymbol type,
        NullableAnnotation annotation)
    {
        if (type.IsReferenceType) return (annotation == NullableAnnotation.Annotated, false);

        return type is INamedTypeSymbol
        {
            IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Nullable_T
        }
            ? (true, false)
            : (false, true);
    }

    private static string? ExtractKeyFromKeyedServiceAttribute(ISymbol parameter)
    {
        var matcher = new AttributeNameMatcher(WellKnownTypes.FromKeyedServicesAttribute);
        var attr = parameter.GetAttributes().FirstOrDefault(a => matcher.IsMatch(a.AttributeClass));

        if (attr is null) return null;

        var val = attr.GetConstructorArgument<object>(0);
        return val switch { string s => $"\"{s}\"", null => null, _ => val.ToString() };
    }

    private static bool HasParameterAttribute(ISymbol parameter, string attributeName)
    {
        return ErrorOrContext.HasAttribute(parameter, attributeName);
    }

    private static string? TryGetAttributeName(ISymbol parameter, string attributeName)
    {
        var attributes = parameter.GetAttributes();

        var matchingAttr =
            attributes.FirstOrDefault(attr => ErrorOrContext.MatchesType(attr.AttributeClass, attributeName));
        return matchingAttr is not null ? ExtractNameFromAttribute(matchingAttr) : null;
    }

    private static string? ExtractNameFromAttribute(AttributeData? attr)
    {
        if (attr is null) return null;

        foreach (var namedArg in attr.NamedArguments)
        {
            if (string.Equals(namedArg.Key, "Name", StringComparison.OrdinalIgnoreCase) &&
                namedArg.Value.Value is string name && !string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }

        if (attr.GetConstructorArgument<string>(0) is { } ctorArg &&
            !string.IsNullOrWhiteSpace(ctorArg))
        {
            return ctorArg;
        }

        if (attr.ApplicationSyntaxReference?.GetSyntax() is { } syntax)
        {
            var syntaxText = syntax.ToString();
            var nameMatch = Regex.Match(syntaxText, """
                                                    Name\s*=\s*"(?<val>[^"]+)"
                                                    """, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture,
                TimeSpan.FromSeconds(1));
            if (nameMatch.Success) return nameMatch.Groups["val"].Value;
        }

        return null;
    }

    /// <summary>
    ///     Detects if a type is likely a DI service based on naming conventions.
    ///     Uses composable type matchers from ANcpLua.Roslyn.Utilities.
    /// </summary>
    private static bool IsLikelyServiceType(ITypeSymbol type)
    {
        // Interfaces are typically services
        if (type.TypeKind == TypeKind.Interface) return true;

        // Abstract types are typically services
        if (type.IsAbstract) return true;

        // Check using fluent matchers for common DI naming patterns
        if (type is INamedTypeSymbol namedType)
        {
            if (s_serviceNameMatcher.Matches(namedType) || s_dbContextMatcher.Matches(namedType))
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Determines if a type is a complex type (DTO) that should be bound from body.
    ///     Returns true for types that are NOT: primitives, special types, route-bindable, or collections of primitives.
    /// </summary>
    private static bool IsComplexType(ITypeSymbol type)
    {
        type = type.UnwrapNullable();

        // Primitives are not complex
        if (type.SpecialType is not SpecialType.None) return false;

        // Well-known types (Guid, DateTime, etc.) are not complex
        if (TryGetRoutePrimitiveKindBySymbol(type) is not null) return false;

        // Form file types are not complex (have special binding)
        if (ErrorOrContext.IsFormFile(type) || ErrorOrContext.IsFormFileCollection(type) ||
            ErrorOrContext.IsFormCollection(type))
        {
            return false;
        }

        // Stream types are not complex (have special binding)
        if (ErrorOrContext.IsStream(type) || ErrorOrContext.IsPipeReader(type)) return false;

        // HttpContext, CancellationToken are not complex
        if (ErrorOrContext.IsHttpContext(type) || ErrorOrContext.IsCancellationToken(type)) return false;

        // Types with TryParse or BindAsync are route-bindable, not complex
        if (type is INamedTypeSymbol namedType && !IsPrimitiveOrWellKnownType(namedType))
        {
            var customBinding = DetectCustomBinding(namedType);
            if (customBinding != CustomBindingMethod.None) return false;
        }

        // Collections of primitives are not complex
        var (isCollection, _, itemKind) = AnalyzeCollectionType(type);
        if (isCollection && itemKind is not null) return false;

        // Interface or abstract types are services, not complex DTOs
        if (type.TypeKind == TypeKind.Interface || type.IsAbstract) return false;

        // Service types by naming convention are not complex DTOs
        return !IsLikelyServiceType(type);
        // Everything else is complex (DTOs, records, classes)
    }
}
