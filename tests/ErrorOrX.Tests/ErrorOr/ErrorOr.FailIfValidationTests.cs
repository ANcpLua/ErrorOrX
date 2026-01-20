namespace ErrorOrX.Tests.ErrorOr;

public class ErrorOrFailIfValidationTests
{
    [Fact]
    public void FailIf_WhenOnValueIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;
        Func<int, bool> onValue = null!;

        // Act
        // This is expected to throw NullReferenceException or ArgumentNullException.
        // If it throws NullReferenceException, it proves the bug (we want ArgumentNullException).
        Action act = () => errorOrInt.FailIf(onValue, Error.Failure());

        // Assert
        act.Should().ThrowExactly<ArgumentNullException>()
            .WithParameterName("onValue");
    }

    [Fact]
    public void FailIf_WhenErrorBuilderIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;
        Func<int, Error> errorBuilder = null!;

        // Act
        Action act = () => errorOrInt.FailIf(_ => true, errorBuilder);

        // Assert
        act.Should().ThrowExactly<ArgumentNullException>()
            .WithParameterName("errorBuilder");
    }

    [Fact]
    public async Task FailIfAsync_WhenOnValueIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;
        Func<int, Task<bool>> onValue = null!;

        // Act
        Func<Task> act = async () => await errorOrInt.FailIfAsync(onValue, Error.Failure());

        // Assert
        await act.Should().ThrowExactlyAsync<ArgumentNullException>()
            .WithParameterName("onValue");
    }
}
