using System.Text;

namespace ErrorOr.Generators;

/// <summary>
///     Centralized utilities for type and endpoint name manipulation.
///     Consolidates normalization, extraction, comparison, and identity logic.
/// </summary>
internal static class TypeNameHelper
{
    private const string GlobalPrefix = "global::";

    private const string EndpointsSuffix = "Endpoints";

    /// <summary>
    ///     Removes the global:: prefix from a fully-qualified type name.
    /// </summary>
    public static string StripGlobalPrefix(string typeFqn) =>
        typeFqn.StartsWith(GlobalPrefix)
            ? typeFqn[GlobalPrefix.Length..]
            : typeFqn;

    /// <summary>
    ///     Normalizes a type name by removing all global:: prefixes (including inside generics)
    ///     and trailing nullable marker.
    /// </summary>
    public static string Normalize(string typeFqn)
    {
        // Remove ALL global:: prefixes (including inside generics like List<global::Todo>)
        var result = typeFqn.Replace(GlobalPrefix, string.Empty);

        // Remove nullable suffix (for reference types)
        if (result.EndsWith("?"))
            result = result[..^1];

        return result;
    }

    /// <summary>
    ///     Unwraps Nullable&lt;T&gt; or nullable reference type annotation to get the underlying type.
    /// </summary>
    /// <param name="typeFqn">The fully-qualified type name.</param>
    /// <param name="shouldUnwrap">If false, returns typeFqn unchanged.</param>
    /// <returns>The unwrapped type name, or the original if not nullable.</returns>
    public static string UnwrapNullable(string typeFqn, bool shouldUnwrap = true)
    {
        if (!shouldUnwrap)
            return typeFqn;

        // Handle nullable reference type annotation (string?)
        if (typeFqn.EndsWith("?", StringComparison.Ordinal))
            return typeFqn[..^1];

        // Handle Nullable<T> for value types
        var normalized = Normalize(typeFqn);
        if (normalized.StartsWith("System.Nullable<", StringComparison.Ordinal) &&
            normalized.EndsWith(">", StringComparison.Ordinal))
            return normalized["System.Nullable<".Length..^1];

        return typeFqn;
    }

    /// <summary>
    ///     Extracts the short type name from a fully-qualified name.
    ///     e.g., "global::System.Collections.Generic.List" -> "List"
    /// </summary>
    public static string ExtractShortName(string typeFqn)
    {
        var normalized = StripGlobalPrefix(typeFqn);

        // Handle array types
        var isArray = normalized.EndsWith("[]");
        var baseName = isArray ? normalized[..^2] : normalized;

        // Handle :: prefix (from global:: stripping edge cases)
        if (baseName.StartsWith("::"))
            baseName = baseName[2..];

        var lastDot = baseName.LastIndexOf('.');
        var shortName = lastDot >= 0 ? baseName[(lastDot + 1)..] : baseName;

        return isArray ? shortName + "[]" : shortName;
    }

    /// <summary>
    ///     Checks if the type name represents System.String.
    /// </summary>
    public static bool IsStringType(string typeFqn)
    {
        var normalized = Normalize(typeFqn);
        return normalized is "string" or "String" or "System.String";
    }

    /// <summary>
    ///     Checks if the type name represents a primitive JSON type that doesn't need explicit registration.
    /// </summary>
    public static bool IsPrimitiveJsonType(string typeFqn)
    {
        var normalized = Normalize(typeFqn);
        return normalized is
            "System.String" or "string" or
            "System.Int32" or "int" or
            "System.Int64" or "long" or
            "System.Boolean" or "bool" or
            "System.Double" or "double" or
            "System.Decimal" or "decimal";
    }

    /// <summary>
    ///     Compares two type names for equality, handling global:: prefixes and keyword aliases.
    /// </summary>
    public static bool TypeNamesMatch(string type1, string type2)
    {
        var normalized1 = Normalize(type1);
        var normalized2 = Normalize(type2);

        // Direct match
        if (normalized1 == normalized2)
            return true;

        var alias1 = GetKeywordAlias(normalized1);
        var alias2 = GetKeywordAlias(normalized2);

        if (alias1 is not null && alias1 == normalized2)
            return true;
        if (alias2 is not null && alias2 == normalized1)
            return true;

        return alias1 is not null && alias2 is not null && alias1 == alias2;
    }

    /// <summary>
    ///     Gets the C# keyword alias for a BCL type name, or null if none exists.
    /// </summary>
    public static string? GetKeywordAlias(string typeName)
    {
        var normalized = Normalize(typeName);
        return normalized switch
        {
            "System.Int32" or "Int32" => "int",
            "System.Int64" or "Int64" => "long",
            "System.Int16" or "Int16" => "short",
            "System.Byte" or "Byte" => "byte",
            "System.SByte" or "SByte" => "sbyte",
            "System.UInt32" or "UInt32" => "uint",
            "System.UInt64" or "UInt64" => "ulong",
            "System.UInt16" or "UInt16" => "ushort",
            "System.Single" or "Single" => "float",
            "System.Double" or "Double" => "double",
            "System.Decimal" or "Decimal" => "decimal",
            "System.Boolean" or "Boolean" => "bool",
            "System.String" or "String" => "string",
            _ => null
        };
    }

    /// <summary>
    ///     Computes the tag name from a containing type name.
    ///     Strips "Endpoints" suffix if present (e.g., "TodoEndpoints" -> "Todo").
    /// </summary>
    public static string GetTagName(string className) =>
        className.EndsWith(EndpointsSuffix)
            ? className[..^EndpointsSuffix.Length]
            : className;

    /// <summary>
    ///     Computes both tag name and operation ID for an endpoint.
    /// </summary>
    public static (string TagName, string OperationId) GetEndpointIdentity(string containingTypeFqn, string methodName)
    {
        var className = ExtractShortName(containingTypeFqn);
        var tagName = GetTagName(className);
        var normalizedType = StripGlobalPrefix(containingTypeFqn);
        var opPrefix = SanitizeIdentifier(normalizedType);

        return (tagName, $"{opPrefix}_{methodName}");
    }

    private static string SanitizeIdentifier(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
            sb.Append(char.IsLetterOrDigit(c) ? c : '_');
        return sb.ToString();
    }
}
