using System.Collections.Frozen;
using System.Collections.Immutable;
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
    // Matches {paramName} or {paramName:constraint} or {paramName:constraint(arg)} or {*catchAll} or {paramName?}
    // Support multiple constraints like {id:int:min(1)}
    private static readonly Regex SRouteParameterRegexInstance = new(
        @"(?<!\{)\{(?<star>\*)?(?<n>[a-zA-Z_][a-zA-Z0-9_]*)(?<constraints>(?::[a-zA-Z]+(?:\([^)]*\))?)*)(?<optional>\?)?\}(?!\})",
        RegexOptions.Compiled);

    private static readonly Regex SIndividualConstraintRegex = new(
        @":(?<name>[a-zA-Z]+)(?:\((?<args>[^)]*)\))?",
        RegexOptions.Compiled);

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
            ["required"] = ["System.String", "string"]
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
            return ImmutableArray<RouteParameterInfo>.Empty;

        // Strip escaped braces first to avoid false positives
        var cleanPattern = pattern.Replace("{{", "").Replace("}}", "");

        var matches = SRouteParameterRegexInstance.Matches(cleanPattern);
        if (matches.Count is 0)
            return ImmutableArray<RouteParameterInfo>.Empty;

        var builder = ImmutableArray.CreateBuilder<RouteParameterInfo>(matches.Count);

        foreach (Match match in matches)
        {
            var name = match.Groups["n"].Value;
            var constraintsRaw = match.Groups["constraints"].Value;
            var isOptional = match.Groups["optional"].Success;
            var isCatchAll = match.Groups["star"].Success;

            // Parse individual constraints if present
            var constraintList = new List<string>();
            if (!string.IsNullOrEmpty(constraintsRaw))
            {
                var cMatches = SIndividualConstraintRegex.Matches(constraintsRaw);
                foreach (Match cMatch in cMatches)
                    constraintList.Add(cMatch.Groups["name"].Value);
            }

            // We use the first type-constraining constraint for validation purposes
            // In Minimal APIs, constraints are additive, but usually only one defines the primitive type
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

        // Check for empty pattern
        if (string.IsNullOrWhiteSpace(pattern))
        {
            diagnostics.Add(DiagnosticInfo.Create(
                Descriptors.InvalidRoutePattern,
                location,
                pattern,
                "Route pattern cannot be empty"));
            return diagnostics.ToImmutable();
        }

        // Check for empty parameter names: {}
        // But ignore escaped braces {{}}
        var escapedStripped = pattern.Replace("{{", "").Replace("}}", "");
        if (escapedStripped.Contains("{}"))
            diagnostics.Add(DiagnosticInfo.Create(
                Descriptors.InvalidRoutePattern,
                location,
                pattern,
                "Route contains empty parameter '{}'. Parameter names are required."));

        // Check for unclosed braces, ignoring escaped ones
        var openCount = escapedStripped.Count(static c => c == '{');
        var closeCount = escapedStripped.Count(static c => c == '}');
        if (openCount != closeCount)
            diagnostics.Add(DiagnosticInfo.Create(
                Descriptors.InvalidRoutePattern,
                location,
                pattern,
                $"Route has mismatched braces: {openCount} '{{' and {closeCount} '}}'"));

        // Check for duplicate parameter names
        var routeParams = ExtractRouteParameters(pattern);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var param in routeParams)
            if (!seen.Add(param.Name))
                diagnostics.Add(DiagnosticInfo.Create(
                    Descriptors.InvalidRoutePattern,
                    location,
                    pattern,
                    $"Route contains duplicate parameter '{{{param.Name}}}'"));

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

        // Build lookup of method parameters by their bound route name
        var boundRouteNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mp in methodParams)
            if (mp.BoundRouteName is not null)
                boundRouteNames.Add(mp.BoundRouteName);

        // Check each route parameter is bound
        foreach (var rp in routeParams)
            if (!boundRouteNames.Contains(rp.Name))
                diagnostics.Add(DiagnosticInfo.Create(
                    Descriptors.RouteParameterNotBound,
                    location,
                    pattern,
                    rp.Name));

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

        // Build lookup of method parameters by their bound route name
        var methodParamsByRouteName = BuildRouteParameterLookup(methodParams, requireTypeFqn: true);

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
        var lookup = new Dictionary<string, RouteMethodParameterInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var param in methodParams)
        {
            if (requireTypeFqn && param.TypeFqn is null)
                continue;

            var routeName = param.BoundRouteName ?? param.Name;
            lookup[routeName] = param;
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
            return;

        // Skip format-only constraints (min, max, range, etc.) - they don't constrain CLR type
        if (FormatOnlyConstraints.Contains(constraint))
            return;

        // Catch-all parameters must be string
        if (routeParam.IsCatchAll)
        {
            AddCatchAllMismatch(routeParam, methodParam, typeFqn, location, diagnostics);
            return;
        }

        if (!ConstraintToTypes.TryGetValue(constraint, out var expectedTypes))
            return; // Unknown constraint - skip validation (could be custom)

        var actualTypeFqn = TypeNameHelper.UnwrapNullable(typeFqn, routeParam.IsOptional || methodParam.IsNullable);
        if (MatchesExpectedType(actualTypeFqn, expectedTypes))
            return;

        diagnostics.Add(DiagnosticInfo.Create(
            Descriptors.RouteConstraintTypeMismatch,
            location,
            routeParam.Name,
            constraint,
            expectedTypes[0],
            methodParam.Name,
            TypeNameHelper.Normalize(typeFqn)));
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
            return false;

        if (!methodParamsByRouteName.TryGetValue(routeParam.Name, out var mp))
            return false;

        if (mp.TypeFqn is null)
            return false;

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
        if (TypeNameHelper.IsStringType(typeFqn))
            return;

        diagnostics.Add(DiagnosticInfo.Create(
            Descriptors.RouteConstraintTypeMismatch,
            location,
            routeParam.Name,
            "*",
            "string",
            methodParam.Name,
            TypeNameHelper.Normalize(typeFqn)));
    }

    private static bool MatchesExpectedType(string actualTypeFqn, IEnumerable<string> expectedTypes)
    {
        var normalizedActual = TypeNameHelper.Normalize(actualTypeFqn);

        foreach (var expected in expectedTypes)
        {
            // Direct match
            if (string.Equals(normalizedActual, expected, StringComparison.Ordinal))
                return true;

            // Suffix match (e.g., "System.Int32" ends with ".Int32" for expected "Int32")
            if (normalizedActual.EndsWith("." + expected, StringComparison.Ordinal))
                return true;

            // Handle keyword aliases (int vs Int32, etc.)
            var aliasedActual = TypeNameHelper.GetKeywordAlias(normalizedActual);
            if (aliasedActual is not null && string.Equals(aliasedActual, expected, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Detects duplicate routes across all registered endpoints.
    /// </summary>
    public static ImmutableArray<Diagnostic> DetectDuplicateRoutes(ImmutableArray<EndpointDescriptor> endpoints)
    {
        if (endpoints.IsDefaultOrEmpty || endpoints.Length < 2)
            return ImmutableArray<Diagnostic>.Empty;

        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var routeMap = new Dictionary<string, EndpointDescriptor>(StringComparer.OrdinalIgnoreCase);

        foreach (var ep in endpoints)
        {
            var normalizedKey = CanonicalizeRoute(ep.HttpMethod, ep.Pattern);

            if (routeMap.TryGetValue(normalizedKey, out var existing))
                diagnostics.Add(Diagnostic.Create(
                    Descriptors.DuplicateRoute,
                    Location.None,
                    ep.HttpMethod.ToUpperInvariant(),
                    ep.Pattern,
                    TypeNameHelper.ExtractShortName(existing.HandlerContainingTypeFqn),
                    existing.HandlerMethodName));
            else
                routeMap[normalizedKey] = ep;
        }

        return diagnostics.ToImmutable();
    }

    /// <summary>
    ///     Canonicalizes a route for duplicate detection.
    ///     Matches ASP.NET Core logic: method + segment structure + constraint types + catch-all.
    /// </summary>
    private static string CanonicalizeRoute(string httpMethod, string pattern)
    {
        // 1. Normalize Method
        var method = httpMethod.ToUpperInvariant();

        // 2. Normalize Pattern
        var p = pattern.Trim();
        if (!p.StartsWith("/")) p = "/" + p;
        if (p.Length > 1 && p.EndsWith("/")) p = p[..^1];

        // 3. Process segments
        var segments = p.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string>(segments.Length + 1)
        {
            method
        };

        foreach (var segment in segments)
        {
            // Ignore escaped braces for segment matching
            var cleanSegment = segment.Replace("{{", "").Replace("}}", "");

            var match = SRouteParameterRegexInstance.Match(cleanSegment);
            if (match.Success)
            {
                var isCatchAll = match.Groups["star"].Success;
                var constraintsRaw = match.Groups["constraints"].Value;

                // Canonicalize constraints
                var cList = new List<string>();
                if (!string.IsNullOrEmpty(constraintsRaw))
                {
                    var cMatches = SIndividualConstraintRegex.Matches(constraintsRaw);
                    foreach (Match cm in cMatches)
                    {
                        var cName = cm.Groups["name"].Value.ToLowerInvariant();
                        // Only type-impacting constraints matter for uniqueness in simple routing
                        if (ConstraintToTypes.ContainsKey(cName))
                            cList.Add(cName);
                    }
                }

                cList.Sort(StringComparer.Ordinal);

                var marker = isCatchAll ? "{**}" : "{?}";
                var constraints = cList.Count > 0 ? ":" + string.Join(":", cList) : "";
                result.Add(marker + constraints);
            }
            else
                // Literal segment
                result.Add(segment.ToLowerInvariant());
        }

        return string.Join("/", result);
    }
}
