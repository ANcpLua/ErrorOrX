using ErrorOr.Core.ErrorOr;
using ErrorOr.Core.Errors;

namespace ErrorOrX.Tests.Runtime;

/// <summary>
///     Unit tests for the ErrorOr&lt;TValue&gt; struct.
/// </summary>
public class ErrorOrTests
{
    #region Error with Metadata Tests

    [Fact]
    public void Error_WithMetadata_ShouldContainMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, object> { { "Field", "Email" }, { "AttemptedValue", "invalid-email" } };

        // Act
        var error = Error.Validation("User.InvalidEmail", "Invalid email format", metadata);

        // Assert
        error.Metadata.Should().NotBeNull();
        error.Metadata.Should().ContainKey("Field");
        error.Metadata["Field"].Should().Be("Email");
    }

    #endregion

    #region IsError and IsSuccess Tests

    [Fact]
    public void IsError_WhenCreatedWithValue_ShouldBeFalse()
    {
        // Arrange
        ErrorOr<int> result = 42;

        // Assert
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public void IsError_WhenCreatedWithError_ShouldBeTrue()
    {
        // Arrange
        ErrorOr<int> result = Error.Failure("Test.Error", "A test error");

        // Assert
        result.IsError.Should().BeTrue();
    }

    [Fact]
    public void IsSuccess_WhenCreatedWithValue_ShouldBeTrue()
    {
        // Arrange
        ErrorOr<int> result = 42;

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void IsSuccess_WhenCreatedWithError_ShouldBeFalse()
    {
        // Arrange
        ErrorOr<int> result = Error.Failure("Test.Error", "A test error");

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region Value Access Tests

    [Fact]
    public void Value_WhenCreatedWithValue_ShouldReturnValue()
    {
        // Arrange
        const int Expected = 42;
        ErrorOr<int> result = Expected;

        // Act
        var actual = result.Value;

        // Assert
        actual.Should().Be(Expected);
    }

    [Fact]
    public void Value_WhenCreatedWithError_ShouldThrowInvalidOperationException()
    {
        // Arrange
        ErrorOr<int> result = Error.Failure("Test.Error", "A test error");

        // Act
        var act = () => result.Value;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot be accessed when errors have been recorded*");
    }

    #endregion

    #region Errors Access Tests

    [Fact]
    public void Errors_WhenCreatedWithError_ShouldReturnErrors()
    {
        // Arrange
        var error = Error.Failure("Test.Error", "A test error");
        ErrorOr<int> result = error;

        // Act
        var errors = result.Errors;

        // Assert
        errors.Should().ContainSingle()
            .Which.Should().Be(error);
    }

    [Fact]
    public void Errors_WhenCreatedWithMultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var errors = new[]
        {
            Error.Failure("Test.Error1", "First error"), Error.Failure("Test.Error2", "Second error")
        };
        ErrorOr<int> result = errors.ToList();

        // Act
        var actualErrors = result.Errors;

        // Assert
        actualErrors.Should().HaveCount(2);
        actualErrors.Should().Contain(errors[0]);
        actualErrors.Should().Contain(errors[1]);
    }

    [Fact]
    public void Errors_WhenCreatedWithValue_ShouldThrowInvalidOperationException()
    {
        // Arrange
        ErrorOr<int> result = 42;

        // Act
        var act = () => result.Errors;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot be accessed when no errors have been recorded*");
    }

    [Fact]
    public void FirstError_WhenCreatedWithError_ShouldReturnFirstError()
    {
        // Arrange
        var firstError = Error.Failure("Test.Error1", "First error");
        var secondError = Error.Failure("Test.Error2", "Second error");
        ErrorOr<int> result = new List<Error> { firstError, secondError };

        // Act
        var error = result.FirstError;

        // Assert
        error.Should().Be(firstError);
    }

    [Fact]
    public void FirstError_WhenCreatedWithValue_ShouldThrowInvalidOperationException()
    {
        // Arrange
        ErrorOr<int> result = 42;

        // Act
        var act = () => result.FirstError;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot be accessed when no errors have been recorded*");
    }

    #endregion

    #region Implicit Conversion Tests

    [Fact]
    public void ImplicitConversion_FromValue_ShouldCreateSuccessResult()
    {
        // Arrange
        const string Value = "test";

        // Act
        ErrorOr<string> result = Value;

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Value);
    }

    [Fact]
    public void ImplicitConversion_FromError_ShouldCreateErrorResult()
    {
        // Arrange
        var error = Error.Validation("Test.Validation", "Invalid input");

        // Act
        ErrorOr<string> result = error;

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(error);
    }

    [Fact]
    public void ImplicitConversion_FromErrorList_ShouldCreateErrorResult()
    {
        // Arrange
        var errors = new List<Error>
        {
            Error.Validation("Test.Validation1", "First validation error"),
            Error.Validation("Test.Validation2", "Second validation error")
        };

        // Act
        ErrorOr<string> result = errors;

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
    }

    #endregion

    #region Error Type Tests

    [Fact]
    public void Error_Failure_ShouldHaveCorrectType()
    {
        // Arrange & Act
        var error = Error.Failure("Test.Failure", "A failure occurred");

        // Assert
        error.Type.Should().Be(ErrorType.Failure);
        error.Code.Should().Be("Test.Failure");
        error.Description.Should().Be("A failure occurred");
    }

    [Fact]
    public void Error_Validation_ShouldHaveCorrectType()
    {
        // Arrange & Act
        var error = Error.Validation("Test.Validation", "Validation failed");

        // Assert
        error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Error_NotFound_ShouldHaveCorrectType()
    {
        // Arrange & Act
        var error = Error.NotFound("Test.NotFound", "Resource not found");

        // Assert
        error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void Error_Conflict_ShouldHaveCorrectType()
    {
        // Arrange & Act
        var error = Error.Conflict("Test.Conflict", "Conflict occurred");

        // Assert
        error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public void Error_Unauthorized_ShouldHaveCorrectType()
    {
        // Arrange & Act
        var error = Error.Unauthorized("Test.Unauthorized", "Unauthorized access");

        // Assert
        error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public void Error_Forbidden_ShouldHaveCorrectType()
    {
        // Arrange & Act
        var error = Error.Forbidden("Test.Forbidden", "Access forbidden");

        // Assert
        error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public void Error_Unexpected_ShouldHaveCorrectType()
    {
        // Arrange & Act
        var error = Error.Unexpected("Test.Unexpected", "Something unexpected happened");

        // Assert
        error.Type.Should().Be(ErrorType.Unexpected);
    }

    #endregion

    #region Match Tests

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

    #endregion

    #region Switch Tests

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

    #endregion

    #region Then/ThenDo Tests

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

    #endregion

    #region Else Tests

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

    #endregion

    #region FailIf Tests

    [Fact]
    public void FailIf_WhenPredicateIsFalse_ShouldReturnOriginalValue()
    {
        // Arrange
        ErrorOr<int> result = 42;
        var error = Error.Validation("Test.Validation", "Value is invalid");

        // Act
        var output = result.FailIf(static value => value < 0, error);

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
        var output = result.FailIf(static value => value < 0, error);

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
        var output = result.FailIf(static _ => true, newError);

        // Assert
        output.IsError.Should().BeTrue();
        output.FirstError.Should().Be(originalError);
    }

    #endregion
}