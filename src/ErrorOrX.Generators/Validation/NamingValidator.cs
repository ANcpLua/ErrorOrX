using System.Text;
using ANcpLua.Roslyn.Utilities.Models;
using ErrorOr.Analyzers;
using Microsoft.CodeAnalysis;

namespace ErrorOr.Generators;

/// <summary>
///     Validates naming conventions for endpoint handler methods.
/// </summary>
internal static class NamingValidator
{
    /// <summary>
    ///     Checks if a method name follows PascalCase convention and returns a diagnostic if not.
    /// </summary>
    /// <param name="methodName">The method name to validate.</param>
    /// <param name="location">The location for diagnostic reporting.</param>
    /// <returns>A diagnostic if the name is not PascalCase, null otherwise.</returns>
    public static DiagnosticInfo? ValidatePascalCase(string methodName, Location location)
    {
        if (string.IsNullOrEmpty(methodName))
        {
            return null;
        }

        if (IsPascalCase(methodName))
        {
            return null;
        }

        var suggested = ToPascalCase(methodName);
        return DiagnosticInfo.Create(
            Descriptors.MethodNameNotPascalCase,
            location,
            methodName,
            suggested);
    }

    /// <summary>
    ///     Determines if a method name follows PascalCase convention.
    /// </summary>
    /// <remarks>
    ///     PascalCase rules:
    ///     - First character must be uppercase
    ///     - No underscores (except as part of identifiers in some edge cases)
    ///     - After each word boundary, next letter should be uppercase
    ///     This method checks:
    ///     - First character is uppercase letter
    ///     - No underscores in the name
    /// </remarks>
    internal static bool IsPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        // First character must be uppercase
        if (!char.IsUpper(name[0]))
        {
            return false;
        }

        // No underscores allowed in PascalCase
        if (name.Contains("_"))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Converts a name to PascalCase.
    /// </summary>
    /// <remarks>
    ///     Handles:
    ///     - camelCase (getById -> GetById)
    ///     - snake_case (get_by_id -> GetById)
    ///     - kebab-case would be invalid C# identifier, not handled
    ///     - Mixed cases (get_ById -> GetById)
    /// </remarks>
    internal static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        // Split by underscores and capitalize each part
        var parts = name.Split('_');

        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part))
            {
                continue;
            }

            // Capitalize first letter, keep rest as-is
            sb.Append(char.ToUpperInvariant(part[0]));
            if (part.Length > 1)
            {
                sb.Append(part.Substring(1));
            }
        }

        return sb.Length > 0 ? sb.ToString() : name;
    }
}
