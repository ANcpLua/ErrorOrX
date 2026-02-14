using System.Collections.Frozen;
using System.Text.RegularExpressions;
using ANcpLua.Roslyn.Utilities.Models;
using ErrorOr.Analyzers;
using Microsoft.CodeAnalysis;

namespace ErrorOr.Generators;

/// <summary>
///     Validates route patterns and parameters at compile time.
///     Supports all ASP.NET Core Minimal API route constraints.
/// </summary>
/// <remarks>
///     Reference: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing#route-constraints
/// </remarks>
internal static class RouteValidator
{
    private static readonly Regex SRouteParameterRegexInstance = new(
        @"(?<!\{)\{(?<star>\*)?(?<n>[a-zA-Z_][a-zA-Z0-9_]*)(?<constraints>(?::[a-zA-Z]+(?:\([^)]*\))?)*)(?<optional>\?)?\}(?!\})",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture,
        TimeSpan.FromSeconds(1));

    private static readonly Regex SIndividualConstraintRegex = new(
        @":(?<name>[a-zA-Z]+)(?:\((?<args>[^)]*)\))?",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture,
        TimeSpan.FromSeconds(1));

    /// <summary>
    ///     Maps route constraints to their expected CLR types.
    ///     Complete coverage of ASP.NET Core Minimal API route constraints.
    /// </summary>
    /// <remarks>
    ///     Format-only constraints are excluded as they don't constrain CLR type:
    ///     - regex(...), min(...), max(...), range(...), minlength(...), maxlength(...), length(...)
    ///     - required, nonfile
    ///     These format constraints accept string but validate format at runtime.
    /// </remarks>
    internal static readonly FrozenDictionary<string, string[]> ConstraintToTypes =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["int"] = ["System.Int32", "int"],
            ["long"] = ["System.Int64", "long"],
            ["short"] = ["System.Int16", "short"],
            ["byte"] = ["System.Byte", "byte"],
            ["sbyte"] = ["System.SByte", "sbyte"],
            ["uint"] = ["System.UInt32", "uint"],
            ["ulong"] = ["System.UInt64", "ulong"],
            ["ushort"] = ["System.UInt16", "ushort"],
            ["decimal"] = ["System.Decimal", "decimal"],
            ["double"] = ["System.Double", "double"],
            ["float"] = ["System.Single", "float"],
            ["bool"] = ["System.Boolean", "bool"],
            ["guid"] = ["System.Guid"],
            ["datetime"] = ["System.DateTime"],
            ["datetimeoffset"] = ["System.DateTimeOffset"],
            ["dateonly"] = ["System.DateOnly"],
            ["timeonly"] = ["System.TimeOnly"],
            ["timespan"] = ["System.TimeSpan"],
            ["alpha"] = ["System.String", "string"],
            ["minlength"] = ["System.String", "string"],
            ["maxlength"] = ["System.String", "string"],
            ["length"] = ["System.String", "string"],
            ["regex"] = ["System.String", "string"],
            ["required"] = ["System.String", "string"],
            ["file"] = ["System.String", "string"]
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Constraints that are format-only and should not trigger type mismatch warnings
    ///     when the parameter is any type that supports ToString().
    /// </summary>
    internal static readonly FrozenSet<string> FormatOnlyConstraints =
        new[]
            {
                "min", "max", "range", "minlength", "maxlength", "length", "regex", "required", "nonfile"
            }
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Extracts route parameters with their constraints from a route pattern.
    /// </summary>
    public static ImmutableArray<RouteParameterInfo> ExtractRouteParameters(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return ImmutableArray<RouteParameterInfo>.Empty;
        }

        var cleanPattern = pattern.Replace("{{", "").Replace("}}", "");

        var matches = SRouteParameterRegexInstance.Matches(cleanPattern);
        if (matches.Count is 0)
        {
            return ImmutableArray<RouteParameterInfo>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<RouteParameterInfo>(matches.Count);

        foreach (Match match in matches)
        {
            var name = match.Groups["n"].Value;
            var constraintsRaw = match.Groups["constraints"].Value;
            var isOptional = match.Groups["optional"].Success;
            var isCatchAll = match.Groups["star"].Success;

            var constraintList = new List<string>();
            if (!string.IsNullOrEmpty(constraintsRaw))
            {
                var cMatches = SIndividualConstraintRegex.Matches(constraintsRaw);
                foreach (Match cMatch in cMatches)
                    constraintList.Add(cMatch.Groups["name"].Value);
            }

            var primaryConstraint = constraintList.FirstOrDefault(static c => ConstraintToTypes.ContainsKey(c))
                                    ?? constraintList.FirstOrDefault();

            builder.Add(new RouteParameterInfo(name, primaryConstraint, isOptional, isCatchAll));
        }

        return builder.ToImmutable();
    }

    /// <summary>
    ///     Validates a route pattern and returns diagnostics for any issues.
    /// </summary>
    public static ImmutableArray<DiagnosticInfo> ValidatePattern(
        string pattern,
        IMethodSymbol method)
    {
        var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();
        var location = method.Locations.FirstOrDefault() ?? Location.None;

        if (string.IsNullOrWhiteSpace(pattern))
        {
            diagnostics.Add(DiagnosticInfo.Create(
                Descriptors.InvalidRoutePattern,
                location,
                pattern,
                "Route pattern cannot be empty"));
            return diagnostics.ToImmutable();
        }

        var escapedStripped = pattern.Replace("{{", "").Replace("}}", "");
        if (escapedStripped.Contains("{}"))
        {
            diagnostics.Add(DiagnosticInfo.Create(
                Descriptors.InvalidRoutePattern,
                location,
                pattern,
                "Route contains empty parameter '{}'. Parameter names are required."));
        }

        var openCount = escapedStripped.Count(static c => c == '{');
        var closeCount = escapedStripped.Count(static c => c == '}');
        if (openCount != closeCount)
        {
            diagnostics.Add(DiagnosticInfo.Create(
                Descriptors.InvalidRoutePattern,
                location,
                pattern,
                $"Route has mismatched braces: {openCount} '{{' and {closeCount} '}}'"));
        }

        var routeParams = ExtractRouteParameters(pattern);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var param in routeParams)
        {
            if (!seen.Add(param.Name))
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    Descriptors.InvalidRoutePattern,
                    location,
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
        ImmutableArray<RouteMethodParameterInfo> methodParams,
        IMethodSymbol method)
    {
        var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();
        var location = method.Locations.FirstOrDefault() ?? Location.None;

        var boundRouteNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mp in methodParams)
        {
            if (mp.BoundRouteName is not null)
            {
                boundRouteNames.Add(mp.BoundRouteName);
            }
        }

        foreach (var rp in routeParams)
        {
            if (!boundRouteNames.Contains(rp.Name))
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    Descriptors.RouteParameterNotBound,
                    location,
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
        ImmutableArray<RouteMethodParameterInfo> methodParams,
        IMethodSymbol method)
    {
        var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();
        var location = method.Locations.FirstOrDefault() ?? Location.None;

        // Pass diagnostics to detect duplicate route parameter bindings (EOE032)
        var methodParamsByRouteName = BuildRouteParameterLookup(methodParams, diagnostics, location, true);

        foreach (var rp in routeParams)
            ValidateRouteConstraint(rp, methodParamsByRouteName, location, diagnostics);

        return diagnostics.ToImmutable();
    }

    /// <summary>
    ///     Builds a lookup dictionary of method parameters keyed by their bound route name.
    /// </summary>
    /// <param name="methodParams">The method parameters to index.</param>
    /// <param name="requireTypeFqn">If true, only include parameters with non-null TypeFqn.</param>
    /// <returns>Dictionary keyed by route name (case-insensitive).</returns>
    internal static Dictionary<string, RouteMethodParameterInfo> BuildRouteParameterLookup(
        ImmutableArray<RouteMethodParameterInfo> methodParams,
        bool requireTypeFqn = false)
    {
        return BuildRouteParameterLookup(methodParams, null, Location.None, requireTypeFqn);
    }

    /// <summary>
    ///     Builds a lookup dictionary of method parameters keyed by their bound route name.
    ///     Reports EOE032 diagnostic when duplicate route parameter names are detected.
    /// </summary>
    /// <param name="methodParams">The method parameters to index.</param>
    /// <param name="diagnostics">Optional builder to collect diagnostics for duplicates.</param>
    /// <param name="location">Location for diagnostic reporting.</param>
    /// <param name="requireTypeFqn">If true, only include parameters with non-null TypeFqn.</param>
    /// <returns>Dictionary keyed by route name (case-insensitive).</returns>
    internal static Dictionary<string, RouteMethodParameterInfo> BuildRouteParameterLookup(
        ImmutableArray<RouteMethodParameterInfo> methodParams,
        ImmutableArray<DiagnosticInfo>.Builder? diagnostics,
        Location location,
        bool requireTypeFqn = false)
    {
        var lookup = new Dictionary<string, RouteMethodParameterInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var param in methodParams)
        {
            if (requireTypeFqn && param.TypeFqn is null)
            {
                continue;
            }

            var routeName = param.BoundRouteName ?? param.Name;

            // First parameter wins for deterministic behavior
            if (lookup.TryGetValue(routeName, out var existingParam))
                // Report EOE032 for duplicate route parameter binding
            {
                diagnostics?.Add(DiagnosticInfo.Create(
                    Descriptors.DuplicateRouteParameterBinding,
                    location,
                    routeName,
                    existingParam.Name,
                    param.Name));
            }
            else
            {
                lookup[routeName] = param;
            }
        }

        return lookup;
    }

    private static void ValidateRouteConstraint(
        RouteParameterInfo routeParam,
        IReadOnlyDictionary<string, RouteMethodParameterInfo> methodParamsByRouteName,
        Location location,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        if (!TryGetConstraintContext(routeParam, methodParamsByRouteName, out var methodParam, out var typeFqn,
                out var constraint))
        {
            return;
        }

        if (FormatOnlyConstraints.Contains(constraint))
        {
            return;
        }

        if (routeParam.IsCatchAll)
        {
            AddCatchAllMismatch(routeParam, methodParam, typeFqn, location, diagnostics);
            return;
        }

        if (!ConstraintToTypes.TryGetValue(constraint, out var expectedTypes))
        {
            return; // Unknown constraint - skip validation (could be custom)
        }

        var actualTypeFqn = typeFqn.UnwrapNullable(routeParam.IsOptional || methodParam.IsNullable);
        if (MatchesExpectedType(actualTypeFqn, expectedTypes))
        {
            return;
        }

        diagnostics.Add(DiagnosticInfo.Create(
            Descriptors.RouteConstraintTypeMismatch,
            location,
            routeParam.Name,
            constraint,
            expectedTypes[0],
            methodParam.Name,
            typeFqn.NormalizeTypeName()));
    }

    private static bool TryGetConstraintContext(
        RouteParameterInfo routeParam,
        IReadOnlyDictionary<string, RouteMethodParameterInfo> methodParamsByRouteName,
        out RouteMethodParameterInfo methodParam,
        out string typeFqn,
        out string constraint)
    {
        methodParam = default;
        typeFqn = string.Empty;
        constraint = string.Empty;

        if (routeParam.Constraint is null)
        {
            return false;
        }

        if (!methodParamsByRouteName.TryGetValue(routeParam.Name, out var mp))
        {
            return false;
        }

        if (mp.TypeFqn is null)
        {
            return false;
        }

        methodParam = mp;
        typeFqn = mp.TypeFqn;
        constraint = routeParam.Constraint;
        return true;
    }

    private static void AddCatchAllMismatch(
        RouteParameterInfo routeParam,
        RouteMethodParameterInfo methodParam,
        string typeFqn,
        Location location,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        if (typeFqn.IsStringType())
        {
            return;
        }

        diagnostics.Add(DiagnosticInfo.Create(
            Descriptors.RouteConstraintTypeMismatch,
            location,
            routeParam.Name,
            "*",
            "string",
            methodParam.Name,
            typeFqn.NormalizeTypeName()));
    }

    private static bool MatchesExpectedType(string actualTypeFqn, IEnumerable<string> expectedTypes)
    {
        var normalizedActual = actualTypeFqn.NormalizeTypeName();

        foreach (var expected in expectedTypes)
        {
            if (string.Equals(normalizedActual, expected, StringComparison.Ordinal))
            {
                return true;
            }

            if (normalizedActual.EndsWithOrdinal("." + expected))
            {
                return true;
            }

            var aliasedActual = normalizedActual.GetCSharpKeyword();
            if (aliasedActual is not null && string.Equals(aliasedActual, expected, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Detects duplicate routes across all registered endpoints.
    /// </summary>
    public static ImmutableArray<Diagnostic> DetectDuplicateRoutes(ImmutableArray<EndpointDescriptor> endpoints)
    {
        if (endpoints.IsDefaultOrEmpty || endpoints.Length < 2)
        {
            return ImmutableArray<Diagnostic>.Empty;
        }

        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var routeMap = new Dictionary<string, EndpointDescriptor>(StringComparer.OrdinalIgnoreCase);

        foreach (var ep in endpoints)
        {
            var normalizedKey = CanonicalizeRoute(ep.HttpMethod, ep.Pattern);

            if (routeMap.TryGetValue(normalizedKey, out var existing))
            {
                diagnostics.Add(Diagnostic.Create(
                    Descriptors.DuplicateRoute,
                    Location.None,
                    ep.HttpMethod.ToUpperInvariant(),
                    ep.Pattern,
                    existing.HandlerContainingTypeFqn.ExtractShortTypeName(),
                    existing.HandlerMethodName));
            }
            else
            {
                routeMap[normalizedKey] = ep;
            }
        }

        return diagnostics.ToImmutable();
    }

    /// <summary>
    ///     Canonicalizes a route for duplicate detection.
    ///     Matches ASP.NET Core logic: method + segment structure + constraint types + catch-all.
    /// </summary>
    private static string CanonicalizeRoute(string httpMethod, string pattern)
    {
        var method = httpMethod.ToUpperInvariant();

        var p = pattern.Trim();
        if (!p.StartsWithOrdinal("/"))
        {
            p = "/" + p;
        }

        if (p.Length > 1 && p.EndsWithOrdinal("/"))
        {
            p = p[..^1];
        }

        var segments = p.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string>(segments.Length + 1)
        {
            method
        };

        foreach (var segment in segments)
        {
            var cleanSegment = segment.Replace("{{", "").Replace("}}", "");

            var match = SRouteParameterRegexInstance.Match(cleanSegment);
            if (match.Success)
            {
                var isCatchAll = match.Groups["star"].Success;
                var constraintsRaw = match.Groups["constraints"].Value;

                var cList = new List<string>();
                if (!string.IsNullOrEmpty(constraintsRaw))
                {
                    var cMatches = SIndividualConstraintRegex.Matches(constraintsRaw);
                    foreach (Match cm in cMatches)
                    {
                        var cName = cm.Groups["name"].Value.ToLowerInvariant();
                        if (ConstraintToTypes.ContainsKey(cName))
                        {
                            cList.Add(cName);
                        }
                    }
                }

                cList.Sort(StringComparer.Ordinal);

                var marker = isCatchAll ? "{**}" : "{?}";
                var constraints = cList.Count > 0 ? ":" + string.Join(":", cList) : "";
                result.Add(marker + constraints);
            }
            else
            {
                result.Add(segment.ToLowerInvariant());
            }
        }

        return string.Join("/", result);
    }
}
