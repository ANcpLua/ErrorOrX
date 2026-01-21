using Microsoft.CodeAnalysis;

namespace ErrorOr.Generators;

/// <summary>
///     Pure extension methods for <see cref="ITypeSymbol" /> manipulation.
///     Complements <see cref="TypeNameHelper" /> (string-based) with symbol-based operations.
/// </summary>
/// <remarks>
///     Pattern from .NET Foundation validation generator.
///     Dependencies passed as parameters for testability and explicit contracts.
/// </remarks>
internal static class ITypeSymbolExtensions
{
    /// <summary>
    ///     Unwraps a type through Nullable{T}, nullable annotation, and optionally collections.
    /// </summary>
    /// <param name="type">The type to unwrap.</param>
    /// <param name="iEnumerableOfT">Optional IEnumerable{T} symbol to unwrap collections.</param>
    /// <returns>The innermost unwrapped type.</returns>
    public static ITypeSymbol UnwrapType(this ITypeSymbol type, INamedTypeSymbol? iEnumerableOfT = null)
    {
        // 1. Unwrap Nullable<T> (value types like int?)
        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
            type is INamedTypeSymbol { TypeArguments.Length: 1 } nullableNamed)
            type = nullableNamed.TypeArguments[0];

        // 2. Remove nullable annotation (reference types like string?)
        if (type.NullableAnnotation == NullableAnnotation.Annotated)
            type = type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);

        // 3. Unwrap IEnumerable<T> / List<T> if symbol provided
        if (iEnumerableOfT is not null &&
            type is INamedTypeSymbol namedType &&
            namedType.IsEnumerable(iEnumerableOfT) &&
            namedType.TypeArguments.Length == 1)
            type = namedType.TypeArguments[0];

        return type;
    }

    /// <summary>
    ///     Checks if the type is or implements IEnumerable{T} (excluding string).
    /// </summary>
    public static bool IsEnumerable(this ITypeSymbol type, INamedTypeSymbol iEnumerableOfT)
    {
        // String is IEnumerable<char> but should not be treated as a collection
        if (type.SpecialType == SpecialType.System_String)
            return false;

        // Direct match
        if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, iEnumerableOfT))
            return true;

        // Check interfaces
        foreach (var iface in type.AllInterfaces)
            if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, iEnumerableOfT))
                return true;

        return false;
    }

    /// <summary>
    ///     Checks if the type implements a specific interface.
    /// </summary>
    public static bool ImplementsInterface(this ITypeSymbol type, INamedTypeSymbol interfaceSymbol)
    {
        foreach (var iface in type.AllInterfaces)
            if (SymbolEqualityComparer.Default.Equals(iface, interfaceSymbol))
                return true;

        return false;
    }

    /// <summary>
    ///     Checks if the type inherits from a validation attribute base class.
    /// </summary>
    public static bool InheritsFromValidationAttribute(
        this ITypeSymbol typeSymbol,
        INamedTypeSymbol validationAttributeSymbol)
    {
        var baseType = typeSymbol.BaseType;
        while (baseType is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(baseType, validationAttributeSymbol))
                return true;
            baseType = baseType.BaseType;
        }

        return false;
    }

    /// <summary>
    ///     Finds a property by name, searching through base types.
    /// </summary>
    public static IPropertySymbol? FindPropertyIncludingBaseTypes(
        this INamedTypeSymbol typeSymbol,
        string propertyName)
    {
        var current = typeSymbol;
        while (current is not null)
        {
            foreach (var member in current.GetMembers())
                if (member is IPropertySymbol property &&
                    string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    return property;

            current = current.BaseType;
        }

        return null;
    }

    /// <summary>
    ///     Checks if a type is exempt from validation (special ASP.NET Core types).
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="httpContext">HttpContext symbol.</param>
    /// <param name="cancellationToken">CancellationToken symbol.</param>
    /// <param name="formFile">IFormFile symbol (optional).</param>
    /// <param name="formCollection">IFormCollection symbol (optional).</param>
    /// <param name="stream">Stream symbol (optional).</param>
    /// <param name="pipeReader">PipeReader symbol (optional).</param>
    public static bool IsExemptType(
        this ITypeSymbol type,
        INamedTypeSymbol httpContext,
        INamedTypeSymbol cancellationToken,
        INamedTypeSymbol? formFile = null,
        INamedTypeSymbol? formCollection = null,
        INamedTypeSymbol? stream = null,
        INamedTypeSymbol? pipeReader = null)
    {
        if (SymbolEqualityComparer.Default.Equals(type, httpContext))
            return true;
        if (SymbolEqualityComparer.Default.Equals(type, cancellationToken))
            return true;
        if (formFile is not null && SymbolEqualityComparer.Default.Equals(type, formFile))
            return true;
        if (formCollection is not null && SymbolEqualityComparer.Default.Equals(type, formCollection))
            return true;
        if (stream is not null && SymbolEqualityComparer.Default.Equals(type, stream))
            return true;
        if (pipeReader is not null && SymbolEqualityComparer.Default.Equals(type, pipeReader))
            return true;

        return false;
    }

    /// <summary>
    ///     Gets the element type from a collection type, if applicable.
    /// </summary>
    public static ITypeSymbol? GetCollectionElementType(
        this ITypeSymbol type,
        INamedTypeSymbol iEnumerableOfT)
    {
        if (type.SpecialType == SpecialType.System_String)
            return null;

        if (type is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            // Check direct implementation
            if (SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, iEnumerableOfT) &&
                namedType.TypeArguments.Length == 1)
                return namedType.TypeArguments[0];

            // Check interfaces
            foreach (var iface in namedType.AllInterfaces)
                if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, iEnumerableOfT) &&
                    iface.TypeArguments.Length == 1)
                    return iface.TypeArguments[0];
        }

        return null;
    }
}
