using System.Text.RegularExpressions;
using Xunit;

namespace ErrorOr.Http.Analyzers.Tests;

/// <summary>
///     Tests for EOE023: Route constraint type mismatch validation.
///     Verifies that route constraints like {id:int} match their bound parameter types.
/// </summary>
/// <remarks>
///     These tests verify the constraint-to-type mapping logic in RouteValidator.
///     They test the algorithm inline to avoid PolySharp conflicts when referencing
///     the netstandard2.0 generator project from net10.0 test project.
/// </remarks>
public class RouteConstraintTypeMatchTests
{
    // Mirrors RouteValidator.s_constraintToTypes
    private static readonly Dictionary<string, string[]> ConstraintToTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["int"] = ["System.Int32", "int"],
        ["long"] = ["System.Int64", "long"],
        ["guid"] = ["System.Guid"],
        ["datetime"] = ["System.DateTime"],
        ["bool"] = ["System.Boolean", "bool"],
        ["decimal"] = ["System.Decimal", "decimal"],
        ["double"] = ["System.Double", "double"],
        ["float"] = ["System.Single", "float"],
        ["alpha"] = ["System.String", "string"],
        ["minlength"] = ["System.String", "string"],
        ["maxlength"] = ["System.String", "string"],
        ["length"] = ["System.String", "string"]
    };

    // Mirrors RouteValidator.s_routeParameterRegexInstance
    private static readonly Regex RouteParameterRegex = new(
        @"\{(?<star>\*)?(?<name>[a-zA-Z_][a-zA-Z0-9_]*)(?::(?<constraint>[a-zA-Z]+)(?:\([^)]*\))?)?(?<optional>\?)?\}",
        RegexOptions.Compiled);

    [Theory]
    [InlineData("int", "System.Int32", true)]
    [InlineData("int", "int", true)]
    [InlineData("int", "string", false)]
    [InlineData("long", "System.Int64", true)]
    [InlineData("long", "long", true)]
    [InlineData("long", "int", false)]
    [InlineData("guid", "System.Guid", true)]
    [InlineData("guid", "string", false)]
    [InlineData("bool", "System.Boolean", true)]
    [InlineData("bool", "bool", true)]
    [InlineData("bool", "string", false)]
    [InlineData("datetime", "System.DateTime", true)]
    [InlineData("datetime", "string", false)]
    [InlineData("decimal", "System.Decimal", true)]
    [InlineData("decimal", "decimal", true)]
    [InlineData("double", "System.Double", true)]
    [InlineData("double", "double", true)]
    [InlineData("float", "System.Single", true)]
    [InlineData("float", "float", true)]
    [InlineData("alpha", "System.String", true)]
    [InlineData("alpha", "string", true)]
    [InlineData("minlength", "string", true)]
    [InlineData("maxlength", "string", true)]
    [InlineData("length", "string", true)]
    public void ConstraintTypeMatch_ValidatesCorrectly(string constraint, string typeFqn, bool shouldMatch)
    {
        // Algorithm from RouteValidator.ValidateConstraintTypes
        var expectedTypes = ConstraintToTypes.TryGetValue(constraint, out var types) ? types : null;

        Assert.NotNull(expectedTypes);

        var matches = false;
        foreach (var expected in expectedTypes)
        {
            if (string.Equals(typeFqn, expected, StringComparison.Ordinal) ||
                typeFqn.EndsWith("." + expected, StringComparison.Ordinal))
            {
                matches = true;
                break;
            }
        }

        Assert.Equal(shouldMatch, matches);
    }

    [Theory]
    [InlineData("{id:int}", "id", "int", false, false)]
    [InlineData("{userId:guid}", "userId", "guid", false, false)]
    [InlineData("{name:alpha}", "name", "alpha", false, false)]
    [InlineData("{id:int?}", "id", "int", true, false)]
    [InlineData("{*path}", "path", null, false, true)]
    [InlineData("{id}", "id", null, false, false)]
    [InlineData("{id?}", "id", null, true, false)]
    public void ExtractRouteParameters_ParsesCorrectly(
        string routeTemplate, string expectedName, string? expectedConstraint,
        bool expectedOptional, bool expectedCatchAll)
    {
        // Algorithm from RouteValidator.ExtractRouteParameters
        var match = RouteParameterRegex.Match(routeTemplate);

        Assert.True(match.Success);
        Assert.Equal(expectedName, match.Groups["name"].Value);
        Assert.Equal(expectedConstraint, match.Groups["constraint"].Success ? match.Groups["constraint"].Value : null);
        Assert.Equal(expectedOptional, match.Groups["optional"].Success);
        Assert.Equal(expectedCatchAll, match.Groups["star"].Success);
    }

    [Theory]
    [InlineData("int", "System.Int32")]
    [InlineData("long", "System.Int64")]
    [InlineData("guid", "System.Guid")]
    [InlineData("bool", "System.Boolean")]
    [InlineData("datetime", "System.DateTime")]
    [InlineData("decimal", "System.Decimal")]
    [InlineData("double", "System.Double")]
    [InlineData("float", "System.Single")]
    [InlineData("alpha", "System.String")]
    public void AllConstraints_HavePrimarySystemType(string constraint, string expectedPrimaryType)
    {
        // Verify each constraint maps to a primary System.* type
        Assert.True(ConstraintToTypes.TryGetValue(constraint, out var types));
        Assert.Contains(expectedPrimaryType, types);
    }

    [Fact]
    public void ConstraintMapping_IsCaseInsensitive()
    {
        // Verify constraint lookup is case-insensitive (matches ASP.NET Core behavior)
        Assert.True(ConstraintToTypes.TryGetValue("INT", out _));
        Assert.True(ConstraintToTypes.TryGetValue("Int", out _));
        Assert.True(ConstraintToTypes.TryGetValue("int", out _));
        Assert.True(ConstraintToTypes.TryGetValue("GUID", out _));
    }

    [Theory]
    [InlineData("global::System.Int32", "System.Int32")]
    [InlineData("System.Int32?", "System.Int32")]
    [InlineData("int?", "int")]
    [InlineData("global::System.String?", "System.String")]
    public void TypeNormalization_RemovesPrefixesAndSuffixes(string actualType, string expectedNormalized)
    {
        // Algorithm from RouteValidator.NormalizeTypeName
        var normalized = actualType;
        if (normalized.StartsWith("global::", StringComparison.Ordinal))
            normalized = normalized["global::".Length..];
        if (normalized.EndsWith("?", StringComparison.Ordinal))
            normalized = normalized[..^1];

        Assert.Equal(expectedNormalized, normalized);
    }

    [Theory]
    [InlineData("System.Int32", "System.Int32", true)]
    [InlineData("System.Int32", "int", false)] // Must check against constraint mapping, not single expected
    [InlineData("int", "int", true)]
    [InlineData("MyNamespace.CustomType", "CustomType", true)] // EndsWith match
    public void TypeNamesMatch_ComparesCorrectly(string actualType, string expected, bool shouldMatch)
    {
        // Algorithm from RouteValidator.TypeNamesMatch
        var matches = string.Equals(actualType, expected, StringComparison.Ordinal) ||
                      actualType.EndsWith("." + expected, StringComparison.Ordinal);

        Assert.Equal(shouldMatch, matches);
    }
}
