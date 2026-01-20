namespace ErrorOrX.Tests.Errors;

public class ErrorNullMetadataTests
{
    [Fact]
    public void Equals_WhenMetadataContainsNullValue_ShouldNotThrow()
    {
        // Arrange
        var metadata = new Dictionary<string, object?>
        {
            ["Key"] = null
        };
        var error1 = Error.Validation("Code", "Description", metadata!);
        var error2 = Error.Validation("Code", "Description", new Dictionary<string, object?>
        {
            ["Key"] = null
        }!);

        // Act
        // This is expected to throw NullReferenceException in the current implementation
        var act = () => error1.Equals(error2);

        // Assert
        // We initially expect this to FAIL (i.e. it WILL throw) to prove the bug.
        // After fix, it should NotThrow and return True.
        act.Should().NotThrow();
        act().Should().BeTrue();
    }

    [Fact]
    public void Equals_WhenOneMetadataValueIsNullAndOtherIsNot_ShouldReturnFalse()
    {
        // Arrange
        var error1 = Error.Validation("Code", "Description", new Dictionary<string, object?>
        {
            ["Key"] = null
        }!);
        var error2 = Error.Validation("Code", "Description", new Dictionary<string, object?>
        {
            ["Key"] = "Value"
        }!);

        // Act
        var result = error1.Equals(error2);

        // Assert
        result.Should().BeFalse();
    }
}
