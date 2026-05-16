namespace ErrorOrX.Tests.ErrorOr;

/// <summary>
///     Tests for the railway-oriented chaining operators on <c>ErrorOr&lt;TValue&gt;</c>:
///     <c>Then</c> / <c>ThenDo</c> (continue on success), <c>Else</c> (fallback on error),
///     and <c>FailIf</c> (conditional failure). The deeper async variants live in
///     <c>ErrorOr.ElseAsyncTests.cs</c>; the basic synchronous core lives here.
/// </summary>
public class ErrorOrChainingTests
{
    [Fact]
    public void Then_WhenSuccess_ShouldTransformValue()
    {
        // Arrange
        ErrorOr<int> result = 42;

        // Act
        var transformed = result.Then(static value => value.ToString());

        // Assert
        transformed.IsError.Should().BeFalse();
        transformed.Value.Should().Be("42");
    }

    [Fact]
    public void Then_WhenError_ShouldPropagateError()
    {
        // Arrange
        var error = Error.Failure("Test.Error", "A test error");
        ErrorOr<int> result = error;

        // Act
        var transformed = result.Then(static value => value.ToString());

        // Assert
        transformed.IsError.Should().BeTrue();
        transformed.FirstError.Should().Be(error);
    }

    [Fact]
    public void ThenDo_WhenSuccess_ShouldExecuteAction()
    {
        // Arrange
        ErrorOr<int> result = 42;
        var actionExecuted = false;

        // Act
        var output = result.ThenDo(_ => actionExecuted = true);

        // Assert
        actionExecuted.Should().BeTrue();
        output.IsError.Should().BeFalse();
        output.Value.Should().Be(42);
    }

    [Fact]
    public void ThenDo_WhenError_ShouldNotExecuteAction()
    {
        // Arrange
        var error = Error.Failure("Test.Error", "A test error");
        ErrorOr<int> result = error;
        var actionExecuted = false;

        // Act
        var output = result.ThenDo(_ => actionExecuted = true);

        // Assert
        actionExecuted.Should().BeFalse();
        output.IsError.Should().BeTrue();
    }

    [Fact]
    public void Else_WhenSuccess_ShouldReturnOriginalValue()
    {
        // Arrange
        ErrorOr<int> result = 42;

        // Act - Else returns ErrorOr<TValue>, so extract .Value
        var output = result.Else(static _ => -1);

        // Assert
        output.IsError.Should().BeFalse();
        output.Value.Should().Be(42);
    }

    [Fact]
    public void Else_WhenError_ShouldReturnFallbackValue()
    {
        // Arrange
        ErrorOr<int> result = Error.Failure("Test.Error", "A test error");

        // Act - Else returns ErrorOr<TValue> with the fallback value
        var output = result.Else(static _ => -1);

        // Assert
        output.IsError.Should().BeFalse();
        output.Value.Should().Be(-1);
    }

    [Fact]
    public void ElseWithValue_WhenError_ShouldReturnFallbackValue()
    {
        // Arrange
        ErrorOr<int> result = Error.Failure("Test.Error", "A test error");

        // Act - Else returns ErrorOr<TValue> with the fallback value
        var output = result.Else(-1);

        // Assert
        output.IsError.Should().BeFalse();
        output.Value.Should().Be(-1);
    }

    [Fact]
    public void FailIf_WhenPredicateIsFalse_ShouldReturnOriginalValue()
    {
        // Arrange
        ErrorOr<int> result = 42;
        var error = Error.Validation("Test.Validation", "Value is invalid");

        // Act
        var output = result.FailIf(static value => value < 0, in error);

        // Assert
        output.IsError.Should().BeFalse();
        output.Value.Should().Be(42);
    }

    [Fact]
    public void FailIf_WhenPredicateIsTrue_ShouldReturnError()
    {
        // Arrange
        ErrorOr<int> result = -5;
        var error = Error.Validation("Test.Validation", "Value must be positive");

        // Act
        var output = result.FailIf(static value => value < 0, in error);

        // Assert
        output.IsError.Should().BeTrue();
        output.FirstError.Should().Be(error);
    }

    [Fact]
    public void FailIf_WhenAlreadyError_ShouldReturnOriginalError()
    {
        // Arrange
        var originalError = Error.Failure("Original.Error", "Original error");
        ErrorOr<int> result = originalError;
        var newError = Error.Validation("Test.Validation", "Value is invalid");

        // Act
        var output = result.FailIf(static _ => true, in newError);

        // Assert
        output.IsError.Should().BeTrue();
        output.FirstError.Should().Be(originalError);
    }
}
