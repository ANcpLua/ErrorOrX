namespace ErrorOrX.Tests.ErrorOr;

public class ErrorOrHashCodeTests
{
    [Fact]
    public void GetHashCode_WhenValueIsNull_ShouldNotThrow()
    {
        // Arrange
        // We use string? as TValue to allow null
        // We use default to bypass the constructor validation that prevents nulls
        ErrorOr<string?> errorOrNull = default;

        // Act
        Action act = () => _ = errorOrNull.GetHashCode();

        // Assert
        // We expect this to fail (Throw NullReferenceException) before the fix
        act.Should().NotThrow();
    }
}
