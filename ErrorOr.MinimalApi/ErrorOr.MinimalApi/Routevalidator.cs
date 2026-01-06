using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using ANcpLua.Roslyn.Utilities.Models;
using Microsoft.CodeAnalysis;

namespace ErrorOr.Http.Generators;

/// <summary>
///     Validates route patterns and parameters at compile time.
///     Supports all ASP.NET Core Minimal API route constraints.
/// </summary>
/// <remarks>
///     Reference: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing#route-constraints
/// </remarks>
internal static class RouteValidator
{
    // Matches {paramName} or {paramName:constraint} or {paramName:constraint(arg)} or {*catchAll} or {paramName?}
    private static readonly Regex s_routeParameterRegexInstance = new(
        @"\{(?<star>\*)?(?<n>[a-zA-Z_][a-zA-Z0-9_]*)(?::(?<constraint>[a-zA-Z]+)(?:\([^)]*\))?)?(?<optional>\?)?\}",
        RegexOptions.Compiled);

    /// <summary>
    ///     Maps route constraints to their expected CLR types.
    ///     Complete coverage of ASP.NET Core Minimal API route constraints.
    /// </summary>
    /// <remarks>
    ///     Format-only constraints are excluded as they don't constrain CLR type:
    ///     - regex(...), min(...), max(...), range(...), minlength(...), maxlength(...), length(...)
    ///     - required, nonfile
    ///
    ///     These format constraints accept string but validate format at runtime.
    /// </remarks>
    private static readonly FrozenDictionary<string, string[]> s_constraintToTypes =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            // Integer types
            ["int"] = ["System.Int32", "int"],
            ["long"] = ["System.Int64", "long"],
            ["short"] = ["System.Int16", "short"],
            ["byte"] = ["System.Byte", "byte"],
            ["sbyte"] = ["System.SByte", "sbyte"],

            // Unsigned integer types
            ["uint"] = ["System.UInt32", "uint"],
            ["ulong"] = ["System.UInt64", "ulong"],
            ["ushort"] = ["System.UInt16", "ushort"],

            // Floating point types
            ["decimal"] = ["System.Decimal", "decimal"],
            ["double"] = ["System.Double", "double"],
            ["float"] = ["System.Single", "float"],

            // Boolean
            ["bool"] = ["System.Boolean", "bool"],

            // Identifier types
            ["guid"] = ["System.Guid"],

            // Date/time types
            ["datetime"] = ["System.DateTime"],
            ["datetimeoffset"] = ["System.DateTimeOffset"],
            ["dateonly"] = ["System.DateOnly"],
            ["timeonly"] = ["System.TimeOnly"],
            ["timespan"] = ["System.TimeSpan"],

            // String format constraints (type is string, format validated at runtime)
            ["alpha"] = ["System.String", "string"],

            // NOTE: These are FORMAT constraints that work on strings
            // They don't constrain the CLR type - just validate the string format at runtime
            // We include them to avoid false positives when users correctly use string parameters
            ["minlength"] = ["System.String", "string"],
            ["maxlength"] = ["System.String", "string"],
            ["length"] = ["System.String", "string"],
            ["regex"] = ["System.String", "string"],
            ["required"] = ["System.String", "string"],
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Constraints that are format-only and should not trigger type mismatch warnings
    ///     when the parameter is any type that supports ToString().
    /// </summary>
    private static readonly FrozenSet<string> s_formatOnlyConstraints =
        new[] { "min", "max", "range", "minlength", "maxlength", "length", "regex", "required", "nonfile" }
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Extracts route parameters with their constraints from a route pattern.
    /// </summary>
    public static ImmutableArray<RouteParameterInfo> ExtractRouteParameters(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return ImmutableArray<RouteParameterInfo>.Empty;

        var matches = s_routeParameterRegexInstance.Matches(pattern);
        if (matches.Count == 0)
            return ImmutableArray<RouteParameterInfo>.Empty;

        var builder = ImmutableArray.CreateBuilder<RouteParameterInfo>(matches.Count);

        foreach (Match match in matches)
        {
            var name = match.Groups["n"].Value;
            var constraint = match.Groups["constraint"].Success ? match.Groups["constraint"].Value : null;
            var isOptional = match.Groups["optional"].Success;
            var isCatchAll = match.Groups["star"].Success;

            builder.Add(new RouteParameterInfo(name, constraint, isOptional, isCatchAll));
        }

        return builder.ToImmutable();
    }

    /// <summary>
    ///     Validates a route pattern and returns diagnostics for any issues.
    /// </summary>
    public static ImmutableArray<DiagnosticInfo> ValidatePattern(
        string pattern,
        IMethodSymbol method,
        string attributeName)
    {
        var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

        // Check for empty pattern
        if (string.IsNullOrWhiteSpace(pattern))
        {
            diagnostics.Add(DiagnosticInfo.Create(
                DiagnosticDescriptors.InvalidRoutePattern,
                method,
                pattern,
                "Route pattern cannot be empty"));
            return diagnostics.ToImmutable();
        }

        // Check for empty parameter names: {}
        if (pattern.Contains("{}"))
        {
            diagnostics.Add(DiagnosticInfo.Create(
                DiagnosticDescriptors.InvalidRoutePattern,
                method,
                pattern,
                "Route contains empty parameter '{}'. Parameter names are required."));
        }

        // Check for unclosed braces
        var openCount = pattern.Count(static c => c == '{');
        var closeCount = pattern.Count(static c => c == '}');
        if (openCount != closeCount)
        {
            diagnostics.Add(DiagnosticInfo.Create(
                DiagnosticDescriptors.InvalidRoutePattern,
                method,
                pattern,
                $"Route has mismatched braces: {openCount} '{{' and {closeCount} '}}'"));
        }

        // Check for duplicate parameter names
        var routeParams = ExtractRouteParameters(pattern);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var param in routeParams)
        {
            if (!seen.Add(param.Name))
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    DiagnosticDescriptors.InvalidRoutePattern,
                    method,
                    pattern,
                    $"Route contains duplicate parameter '{{{param.Name}}}'"));
            }
        }

        return diagnostics.ToImmutable();
    }

    /// <summary>
    ///     Validates that all route parameters are bound to method parameters.
    /// </summary>
    public static ImmutableArray<DiagnosticInfo> ValidateParameterBindings(
        string pattern,
        ImmutableArray<RouteParameterInfo> routeParams,
        ImmutableArray<MethodParameterInfo> methodParams,
        IMethodSymbol method)
    {
        var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

        // Build lookup of method parameters by their bound route name
        var boundRouteNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mp in methodParams)
        {
            if (mp.BoundRouteName is not null)
                boundRouteNames.Add(mp.BoundRouteName);
        }

        // Check each route parameter is bound
        foreach (var rp in routeParams)
        {
            if (!boundRouteNames.Contains(rp.Name))
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    DiagnosticDescriptors.RouteParameterNotBound,
                    method,
                    pattern,
                    rp.Name));
            }
        }

        return diagnostics.ToImmutable();
    }

    /// <summary>
    ///     Validates that route constraints match their bound method parameter types.
    /// </summary>
    public static ImmutableArray<DiagnosticInfo> ValidateConstraintTypes(
        ImmutableArray<RouteParameterInfo> routeParams,
        ImmutableArray<MethodParameterInfo> methodParams,
        IMethodSymbol method)
    {
        var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

        // Build lookup of method parameters by their bound route name
        var methodParamsByRouteName = new Dictionary<string, MethodParameterInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var mp in methodParams)
        {
            if (mp.BoundRouteName is not null && mp.TypeFqn is not null)
                methodParamsByRouteName[mp.BoundRouteName] = mp;
        }

        foreach (var rp in routeParams)
        {
            // Skip if no constraint or not bound to a method parameter
            if (rp.Constraint is null || !methodParamsByRouteName.TryGetValue(rp.Name, out var mp))
                continue;

            // Skip format-only constraints (min, max, range, etc.) - they don't constrain CLR type
            if (s_formatOnlyConstraints.Contains(rp.Constraint))
                continue;

            // Catch-all parameters must be string
            if (rp.IsCatchAll)
            {
                if (!IsStringType(mp.TypeFqn!))
                {
                    diagnostics.Add(DiagnosticInfo.Create(
                        DiagnosticDescriptors.RouteConstraintTypeMismatch,
                        method,
                        rp.Name,
                        "*",
                        "string",
                        mp.Name,
                        NormalizeTypeName(mp.TypeFqn!)));
                }

                continue;
            }

            // Look up expected types for this constraint
            if (!s_constraintToTypes.TryGetValue(rp.Constraint, out var expectedTypes))
                continue; // Unknown constraint - skip validation (could be custom)

            // Get the actual type, unwrapping Nullable<T> for optional parameters
            var actualTypeFqn = UnwrapNullableType(mp.TypeFqn!, rp.IsOptional || mp.IsNullable);

            // Check if actual type matches any expected type
            var matches = false;
            foreach (var expected in expectedTypes)
            {
                if (TypeNamesMatch(actualTypeFqn, expected))
                {
                    matches = true;
                    break;
                }
            }

            if (!matches)
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    DiagnosticDescriptors.RouteConstraintTypeMismatch,
                    method,
                    rp.Name,
                    rp.Constraint,
                    expectedTypes[0],
                    mp.Name,
                    NormalizeTypeName(mp.TypeFqn!)));
            }
        }

        return diagnostics.ToImmutable();
    }

    /// <summary>
    ///     Unwraps Nullable&lt;T&gt; to get the underlying type.
    /// </summary>
    private static string UnwrapNullableType(string typeFqn, bool shouldUnwrap)
    {
        if (!shouldUnwrap)
            return typeFqn;

        // Handle nullable reference type annotation (string?)
        if (typeFqn.EndsWith("?", StringComparison.Ordinal))
            return typeFqn[..^1];

        // Handle Nullable<T> for value types
        var normalized = NormalizeTypeName(typeFqn);
        if (normalized.StartsWith("System.Nullable<", StringComparison.Ordinal) &&
            normalized.EndsWith(">", StringComparison.Ordinal))
        {
            return normalized["System.Nullable<".Length..^1];
        }

        return typeFqn;
    }

    private static bool IsStringType(string typeFqn)
    {
        var normalized = NormalizeTypeName(typeFqn);
        return normalized is "string" or "String" or "System.String";
    }

    private static bool TypeNamesMatch(string actualFqn, string expected)
    {
        var normalizedActual = NormalizeTypeName(actualFqn);

        // Direct match
        if (string.Equals(normalizedActual, expected, StringComparison.Ordinal))
            return true;

        // Suffix match (e.g., "System.Int32" ends with ".Int32" for expected "Int32")
        if (normalizedActual.EndsWith("." + expected, StringComparison.Ordinal))
            return true;

        // Handle keyword aliases (int vs Int32, etc.)
        var aliasedActual = GetTypeKeywordAlias(normalizedActual);
        if (aliasedActual is not null && string.Equals(aliasedActual, expected, StringComparison.Ordinal))
            return true;

        return false;
    }

    /// <summary>
    ///     Gets the C# keyword alias for a BCL type name, or null if none exists.
    /// </summary>
    private static string? GetTypeKeywordAlias(string typeName)
    {
        return typeName switch
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

    private static string NormalizeTypeName(string typeFqn)
    {
        var result = typeFqn;

        // Remove global:: prefix
        if (result.StartsWith("global::", StringComparison.Ordinal))
            result = result["global::".Length..];

        // Remove nullable suffix (for reference types)
        if (result.EndsWith("?", StringComparison.Ordinal))
            result = result[..^1];

        return result;
    }
}

/// <summary>
///     Information about a route parameter extracted from the route template.
/// </summary>
internal readonly record struct RouteParameterInfo(
    string Name,
    string? Constraint,
    bool IsOptional,
    bool IsCatchAll);

/// <summary>
///     Information about a method parameter relevant to route binding.
/// </summary>
internal readonly record struct MethodParameterInfo(
    string Name,
    string? BoundRouteName,
    string? TypeFqn = null,
    bool IsNullable = false);