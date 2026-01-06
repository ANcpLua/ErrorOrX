using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using ANcpLua.Roslyn.Utilities.Matching;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RegexMatch = System.Text.RegularExpressions.Match;
using SymbolMatch = ANcpLua.Roslyn.Utilities.Matching.Match;

namespace ErrorOr.Http.Analyzers;

/// <summary>
///     Real-time Roslyn analyzer for ErrorOr.Http endpoints.
///     Provides immediate IDE feedback for common issues.
/// </summary>
/// <remarks>
///     This analyzer handles single-method diagnostics that can run fast.
///     Cross-file diagnostics (EOE004, EOE007, EOE008) remain in the generator.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ErrorOrEndpointAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        Descriptors.InvalidReturnType,
        Descriptors.NonStaticHandler,
        Descriptors.RouteParameterNotBound,
        Descriptors.InvalidRoutePattern,
        Descriptors.MultipleBodySources,
        Descriptors.BodyOnReadOnlyMethod,
        Descriptors.AcceptedOnReadOnlyMethod,
        Descriptors.RouteConstraintTypeMismatch
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol method)
            return;

        // Find ErrorOr endpoint attributes
        var endpointAttributes = GetEndpointAttributes(method);
        if (endpointAttributes.Count == 0)
            return;

        // EOE002: Handler must be static
        if (!method.IsStatic)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.NonStaticHandler,
                method.Locations.FirstOrDefault(),
                method.Name));
            return; // Don't report more diagnostics for invalid handler
        }

        // EOE001: Invalid return type
        if (!IsValidReturnType(method.ReturnType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.InvalidReturnType,
                method.Locations.FirstOrDefault(),
                method.Name));
            return; // Don't report more diagnostics for invalid handler
        }

        // Analyze each endpoint attribute
        foreach (var (httpMethod, pattern, attributeLocation) in endpointAttributes)
        {
            AnalyzeEndpoint(context, method, httpMethod, pattern, attributeLocation);
        }
    }

    private static void AnalyzeEndpoint(
        SymbolAnalysisContext context,
        IMethodSymbol method,
        string httpMethod,
        string pattern,
        Location attributeLocation)
    {
        // EOE005: Invalid route pattern
        var patternDiagnostics = ValidateRoutePattern(pattern);
        foreach (var message in patternDiagnostics)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.InvalidRoutePattern,
                attributeLocation,
                pattern,
                message));
        }

        // If pattern is invalid, skip further route analysis
        if (patternDiagnostics.Count > 0)
            return;

        // Extract route parameters with constraints
        var routeParams = ExtractRouteParametersWithConstraints(pattern);

        // Build method parameter lookup
        var methodParamsByRouteName = BuildMethodParameterLookup(method);

        // EOE003: Route parameter not bound
        foreach (var routeParam in routeParams)
        {
            if (!methodParamsByRouteName.ContainsKey(routeParam.Name))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.RouteParameterNotBound,
                    attributeLocation,
                    pattern,
                    routeParam.Name));
            }
        }

        // EOE023: Route constraint type mismatch
        ValidateConstraintTypes(context, routeParams, methodParamsByRouteName, attributeLocation);

        // EOE006: Multiple body sources
        var bodyCount = CountBodySources(method);
        if (bodyCount > 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.MultipleBodySources,
                method.Locations.FirstOrDefault(),
                method.Name));
        }

        // EOE009: Body on read-only HTTP method
        var hasBody = bodyCount > 0;
        if (hasBody && IsReadOnlyHttpMethod(httpMethod))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.BodyOnReadOnlyMethod,
                attributeLocation,
                method.Name,
                httpMethod.ToUpperInvariant()));
        }

        // EOE010: [AcceptedResponse] on read-only method
        if (HasAcceptedResponseAttribute(method) && IsReadOnlyHttpMethod(httpMethod))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.AcceptedOnReadOnlyMethod,
                attributeLocation,
                method.Name,
                httpMethod.ToUpperInvariant()));
        }
    }

    /// <summary>
    ///     Validates route constraint types match method parameter types (EOE023).
    /// </summary>
    private static void ValidateConstraintTypes(
        SymbolAnalysisContext context,
        List<RouteParameterInfo> routeParams,
        Dictionary<string, MethodParameterInfo> methodParamsByRouteName,
        Location attributeLocation)
    {
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
                if (!IsStringType(mp.TypeFqn))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.RouteConstraintTypeMismatch,
                        attributeLocation,
                        rp.Name,
                        "*",
                        "string",
                        mp.Name,
                        NormalizeTypeName(mp.TypeFqn)));
                }

                continue;
            }

            // Look up expected types for this constraint
            if (!s_constraintToTypes.TryGetValue(rp.Constraint, out var expectedTypes))
                continue; // Unknown constraint (e.g., custom) - skip validation

            // Get the actual type, unwrapping Nullable<T> for optional parameters
            var actualTypeFqn = UnwrapNullableType(mp.TypeFqn, rp.IsOptional || mp.IsNullable);

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
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.RouteConstraintTypeMismatch,
                    attributeLocation,
                    rp.Name,
                    rp.Constraint,
                    expectedTypes[0],
                    mp.Name,
                    NormalizeTypeName(mp.TypeFqn)));
            }
        }
    }

    /// <summary>
    ///     HTTP method constants to avoid magic strings.
    /// </summary>
    private static class HttpMethod
    {
        public const string Get = "GET";
        public const string Post = "POST";
        public const string Put = "PUT";
        public const string Delete = "DELETE";
        public const string Patch = "PATCH";
        public const string Head = "HEAD";
        public const string Options = "OPTIONS";
    }

    // Type matchers for pattern-based validation
    private static readonly TypeMatcher ErrorOrMatcher = SymbolMatch.Type()
        .Named("ErrorOr")
        .InNamespace("ErrorOr")
        .Generic();

    private static readonly TypeMatcher StreamMatcher = SymbolMatch.Type()
        .NameContains("Stream");

    private static readonly TypeMatcher PipeReaderMatcher = SymbolMatch.Type()
        .Named("PipeReader");

    private static readonly TypeMatcher FormFileMatcher = SymbolMatch.Type()
        .Named("IFormFile");

    private static readonly TypeMatcher FormFileCollectionMatcher = SymbolMatch.Type()
        .Named("IFormFileCollection");

    private static readonly TypeMatcher FormCollectionMatcher = SymbolMatch.Type()
        .Named("IFormCollection");

    /// <summary>
    ///     Maps route constraints to their expected CLR types.
    ///     Complete coverage of ASP.NET Core Minimal API route constraints.
    /// </summary>
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

            // String format constraints
            ["alpha"] = ["System.String", "string"],
            ["minlength"] = ["System.String", "string"],
            ["maxlength"] = ["System.String", "string"],
            ["length"] = ["System.String", "string"],
            ["regex"] = ["System.String", "string"],
            ["required"] = ["System.String", "string"],
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Constraints that are format-only and should not trigger type mismatch warnings.
    /// </summary>
    private static readonly FrozenSet<string> s_formatOnlyConstraints =
        new[] { "min", "max", "range", "minlength", "maxlength", "length", "regex", "required", "nonfile" }
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    #region Helpers

    private static List<(string HttpMethod, string Pattern, Location Location)> GetEndpointAttributes(
        IMethodSymbol method)
    {
        var results = new List<(string, string, Location)>();

        foreach (var attr in method.GetAttributes())
        {
            var attrName = attr.AttributeClass?.Name;
            if (attrName is null)
                continue;

            string? httpMethod = null;
            var pattern = "/";

            // Check for specific HTTP method attributes
            if (attrName is "GetAttribute" or "Get")
                httpMethod = HttpMethod.Get;
            else if (attrName is "PostAttribute" or "Post")
                httpMethod = HttpMethod.Post;
            else if (attrName is "PutAttribute" or "Put")
                httpMethod = HttpMethod.Put;
            else if (attrName is "DeleteAttribute" or "Delete")
                httpMethod = HttpMethod.Delete;
            else if (attrName is "PatchAttribute" or "Patch")
                httpMethod = HttpMethod.Patch;
            else if (attrName is "ErrorOrEndpointAttribute" or "ErrorOrEndpoint")
            {
                // Generic endpoint attribute - extract HTTP method from first constructor arg
                if (attr.ConstructorArguments.Length >= 1 &&
                    attr.ConstructorArguments[0].Value is string m)
                {
                    httpMethod = m.ToUpperInvariant();
                }
            }

            if (httpMethod is null)
                continue;

            // Extract pattern from constructor arguments
            var patternIndex = attrName.Contains("ErrorOrEndpoint") ? 1 : 0;
            if (attr.ConstructorArguments.Length > patternIndex &&
                attr.ConstructorArguments[patternIndex].Value is string p)
            {
                pattern = p;
            }

            var location = attr.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                           ?? method.Locations.FirstOrDefault()
                           ?? Location.None;

            results.Add((httpMethod, pattern, location));
        }

        return results;
    }

    private static bool IsValidReturnType(ITypeSymbol returnType)
    {
        if (returnType is not INamedTypeSymbol named)
            return false;

        // Direct ErrorOr<T>
        if (ErrorOrMatcher.Matches(named))
            return true;

        // Task<ErrorOr<T>> or ValueTask<ErrorOr<T>>
        if (named.Name is "Task" or "ValueTask" && named.IsGenericType && named.TypeArguments.Length > 0)
        {
            return ErrorOrMatcher.Matches(named.TypeArguments[0]);
        }

        return false;
    }

    // Matches {paramName} or {paramName:constraint} or {paramName:constraint(arg)} or {*catchAll} or {paramName?}
    private static readonly Regex s_routeParameterWithConstraintRegex = new(
        @"\{(?<star>\*)?(?<n>[a-zA-Z_][a-zA-Z0-9_]*)(?::(?<constraint>[a-zA-Z]+)(?:\([^)]*\))?)?(?<optional>\?)?\}",
        RegexOptions.Compiled);

    /// <summary>
    ///     Extracts route parameters with their constraints from a route pattern.
    /// </summary>
    private static List<RouteParameterInfo> ExtractRouteParametersWithConstraints(string pattern)
    {
        var results = new List<RouteParameterInfo>();
        if (string.IsNullOrWhiteSpace(pattern))
            return results;

        foreach (RegexMatch match in s_routeParameterWithConstraintRegex.Matches(pattern))
        {
            var name = match.Groups["n"].Value;
            var constraint = match.Groups["constraint"].Success ? match.Groups["constraint"].Value : null;
            var isOptional = match.Groups["optional"].Success;
            var isCatchAll = match.Groups["star"].Success;

            results.Add(new RouteParameterInfo(name, constraint, isOptional, isCatchAll));
        }

        return results;
    }

    /// <summary>
    ///     Builds a lookup of method parameters by their bound route name.
    /// </summary>
    private static Dictionary<string, MethodParameterInfo> BuildMethodParameterLookup(IMethodSymbol method)
    {
        var lookup = new Dictionary<string, MethodParameterInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var param in method.Parameters)
        {
            var boundRouteName = GetBoundRouteName(param);
            if (boundRouteName is null)
                continue;

            var typeFqn = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var isNullable = param.Type.NullableAnnotation == NullableAnnotation.Annotated ||
                             IsNullableValueType(param.Type);

            lookup[boundRouteName] = new MethodParameterInfo(param.Name, typeFqn, isNullable);
        }

        return lookup;
    }

    /// <summary>
    ///     Checks if the type is Nullable&lt;T&gt; (e.g., int?).
    /// </summary>
    private static bool IsNullableValueType(ITypeSymbol type)
    {
        return type is INamedTypeSymbol
        {
            IsValueType: true, OriginalDefinition.SpecialType: SpecialType.System_Nullable_T
        };
    }

    private static string? GetBoundRouteName(IParameterSymbol parameter)
    {
        // Check for [FromRoute] attribute with custom name
        foreach (var attr in parameter.GetAttributes())
        {
            var name = attr.AttributeClass?.Name;
            if (name is "FromRouteAttribute" or "FromRoute")
            {
                // Check for Name property
                foreach (var namedArg in attr.NamedArguments)
                {
                    if (namedArg.Key == "Name" && namedArg.Value.Value is string customName)
                        return customName;
                }

                return parameter.Name;
            }
        }

        // Default: parameter name can match route parameter
        return parameter.Name;
    }

    private static int CountBodySources(IMethodSymbol method)
    {
        var count = 0;

        foreach (var param in method.Parameters)
        {
            // Check for body-related attributes
            foreach (var attr in param.GetAttributes())
            {
                var attrName = attr.AttributeClass?.Name;
                if (attrName is "FromBodyAttribute" or "FromBody" or
                    "FromFormAttribute" or "FromForm")
                {
                    count++;
                    goto nextParam;
                }
            }

            // Check for body-related types using pattern matchers
            if (IsBodyType(param.Type))
            {
                count++;
            }

            nextParam: ;
        }

        return count;
    }

    private static bool IsBodyType(ITypeSymbol type) =>
        StreamMatcher.Matches(type) ||
        PipeReaderMatcher.Matches(type) ||
        FormFileMatcher.Matches(type) ||
        FormFileCollectionMatcher.Matches(type) ||
        FormCollectionMatcher.Matches(type);

    private static bool IsReadOnlyHttpMethod(string httpMethod) =>
        httpMethod.ToUpperInvariant() is HttpMethod.Get or HttpMethod.Head or HttpMethod.Delete or HttpMethod.Options;

    private static bool HasAcceptedResponseAttribute(IMethodSymbol method)
    {
        foreach (var attr in method.GetAttributes())
        {
            var name = attr.AttributeClass?.Name;
            if (name is "AcceptedResponseAttribute" or "AcceptedResponse")
                return true;
        }

        return false;
    }

    private static List<string> ValidateRoutePattern(string pattern)
    {
        var issues = new List<string>();

        if (string.IsNullOrWhiteSpace(pattern))
        {
            issues.Add("Route pattern cannot be empty");
            return issues;
        }

        // Check for empty parameter names: {}
        if (pattern.Contains("{}"))
        {
            issues.Add("Route contains empty parameter '{}'. Parameter names are required");
        }

        // Check for unclosed braces
        var openCount = pattern.Count(static c => c == '{');
        var closeCount = pattern.Count(static c => c == '}');
        if (openCount != closeCount)
        {
            issues.Add($"Route has mismatched braces: {openCount} '{{' and {closeCount} '}}'");
        }

        // Check for duplicate parameter names
        var paramNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (RegexMatch match in s_routeParameterWithConstraintRegex.Matches(pattern))
        {
            var name = match.Groups["n"].Value;
            if (!paramNames.Add(name))
            {
                issues.Add($"Route contains duplicate parameter '{{{name}}}'");
            }
        }

        return issues;
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

        // Suffix match
        if (normalizedActual.EndsWith("." + expected, StringComparison.Ordinal))
            return true;

        // Handle keyword aliases
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

    #endregion

    #region Local Types

    /// <summary>
    ///     Information about a route parameter extracted from the route template.
    /// </summary>
    private readonly record struct RouteParameterInfo(
        string Name,
        string? Constraint,
        bool IsOptional,
        bool IsCatchAll);

    /// <summary>
    ///     Information about a method parameter relevant to route binding.
    /// </summary>
    private readonly record struct MethodParameterInfo(
        string Name,
        string TypeFqn,
        bool IsNullable);

    #endregion
}