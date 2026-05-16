namespace ErrorOrX.Tests.ErrorOr;

/// <summary>
///     Tests for the <c>Match</c> (transformation) and <c>Switch</c> (side-effect) handlers
///     on <c>ErrorOr&lt;TValue&gt;</c>.
/// </summary>
public class ErrorOrMatchAndSwitchTests
{
    [Fact]
    public void Match_WhenSuccess_ShouldInvokeOnValue()
    {
        // Arrange
        ErrorOr<int> result = 42;
        var onValueInvoked = false;
        var onErrorInvoked = false;

        // Act
        var output = result.Match(
            value =>
            {
                onValueInvoked = true;
                return value * 2;
            },
            _ =>
            {
                onErrorInvoked = true;
                return -1;
            });

        // Assert
        onValueInvoked.Should().BeTrue();
        onErrorInvoked.Should().BeFalse();
        output.Should().Be(84);
    }

    [Fact]
    public void Match_WhenError_ShouldInvokeOnError()
    {
        // Arrange
        ErrorOr<int> result = Error.Failure("Test.Error", "A test error");
        var onValueInvoked = false;
        var onErrorInvoked = false;

        // Act
        var output = result.Match(
            value =>
            {
                onValueInvoked = true;
                return value * 2;
            },
            _ =>
            {
                onErrorInvoked = true;
                return -1;
            });

        // Assert
        onValueInvoked.Should().BeFalse();
        onErrorInvoked.Should().BeTrue();
        output.Should().Be(-1);
    }

    [Fact]
    public void Switch_WhenSuccess_ShouldInvokeOnValue()
    {
        // Arrange
        ErrorOr<int> result = 42;
        var onValueInvoked = false;
        var onErrorInvoked = false;

        // Act
        result.Switch(
            _ => onValueInvoked = true,
            _ => onErrorInvoked = true);

        // Assert
        onValueInvoked.Should().BeTrue();
        onErrorInvoked.Should().BeFalse();
    }

    [Fact]
    public void Switch_WhenError_ShouldInvokeOnError()
    {
        // Arrange
        ErrorOr<int> result = Error.Failure("Test.Error", "A test error");
        var onValueInvoked = false;
        var onErrorInvoked = false;

        // Act
        result.Switch(
            _ => onValueInvoked = true,
            _ => onErrorInvoked = true);

        // Assert
        onValueInvoked.Should().BeFalse();
        onErrorInvoked.Should().BeTrue();
    }
}
