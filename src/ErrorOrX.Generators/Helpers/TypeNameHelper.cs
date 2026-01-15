namespace ErrorOr.Generators;

/// <summary>
///     Centralized utilities for type and endpoint name manipulation.
///     Consolidates normalization, extraction, comparison, and identity logic.
/// </summary>
internal static class TypeNameHelper
{
    private const string GlobalPrefix = "global::";

    /// <summary>
    ///     Removes the global:: prefix from a fully-qualified type name.
    /// </summary>
    public static string StripGlobalPrefix(string typeFqn)
    {
        return typeFqn.StartsWith(GlobalPrefix)
            ? typeFqn[GlobalPrefix.Length..]
            : typeFqn;
    }

    /// <summary>
    ///     Normalizes a type name by removing global:: prefix and trailing nullable marker.
    /// </summary>
    public static string Normalize(string typeFqn)
    {
        var result = StripGlobalPrefix(typeFqn);

        // Remove nullable suffix (for reference types)
        if (result.EndsWith("?"))
            result = result[..^1];

        return result;
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
    ///     Compares two type names for equality, handling various formats.
    ///     Supports global::, short names, and namespace suffixes.
    /// </summary>
    public static bool TypeNamesMatch(string type1, string type2)
    {
        var normalized1 = Normalize(type1);
        var normalized2 = Normalize(type2);

        // Direct match
        if (normalized1 == normalized2)
            return true;

        // Extract short names and compare
        var short1 = ExtractShortName(normalized1);
        var short2 = ExtractShortName(normalized2);

        return short1 == short2 ||
               normalized1.EndsWith(short2) ||
               normalized2.EndsWith(short1);
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

    private const string EndpointsSuffix = "Endpoints";

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
    public static (string TagName, string OperationId) GetEndpointIdentity(string className, string methodName)
    {
        var tagName = GetTagName(className);
        return (tagName, $"{tagName}_{methodName}");
    }
}