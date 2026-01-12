namespace ErrorOrX.Generators.Tests;

/// <summary>
///     Tests to ensure the expected ErrorType enum values exist.
///     This documents the ErrorType enum members that the generator's ErrorMapping constants
///     must stay in sync with. If these tests fail, update ErrorMapping.cs accordingly.
/// </summary>
public class ErrorMappingSyncTests
{
    /// <summary>
    ///     The canonical set of ErrorType names that the generator's ErrorMapping constants must match.
    ///     SYNC REQUIREMENT: If you add or remove values here, update ErrorMapping.cs too!
    /// </summary>
    private static readonly HashSet<string> ExpectedErrorTypeNames = new(StringComparer.Ordinal)
    {
        "Failure",
        "Unexpected",
        "Validation",
        "Conflict",
        "NotFound",
        "Unauthorized",
        "Forbidden"
    };

    [Fact]
    public void RuntimeErrorType_HasExpectedMembers()
    {
        // Get all names from the runtime ErrorType enum
        var runtimeNames = Enum.GetNames<ErrorOr.ErrorType>().ToHashSet(StringComparer.Ordinal);

        // Verify they match the expected set
        runtimeNames.Should().BeEquivalentTo(ExpectedErrorTypeNames,
            "Runtime ErrorType enum must match expected values. " +
            "If you changed ErrorType, update both this test AND ErrorMapping.cs in the generator");
    }

    [Fact]
    public void RuntimeErrorType_HasCorrectOrdinalValues()
    {
        // Document the expected ordinal values that MapEnumValueToName in the generator depends on
        // This ensures the generator's MapEnumValueToName stays correct
        ((int)ErrorOr.ErrorType.Failure).Should().Be(0);
        ((int)ErrorOr.ErrorType.Unexpected).Should().Be(1);
        ((int)ErrorOr.ErrorType.Validation).Should().Be(2);
        ((int)ErrorOr.ErrorType.Conflict).Should().Be(3);
        ((int)ErrorOr.ErrorType.NotFound).Should().Be(4);
        ((int)ErrorOr.ErrorType.Unauthorized).Should().Be(5);
        ((int)ErrorOr.ErrorType.Forbidden).Should().Be(6);
    }

    [Fact]
    public void RuntimeErrorType_Count_MatchesExpected()
    {
        var runtimeCount = Enum.GetValues<ErrorOr.ErrorType>().Length;

        runtimeCount.Should().Be(ExpectedErrorTypeNames.Count,
            "If ErrorType enum count changed, update ErrorMapping.cs and this test");
    }
}
