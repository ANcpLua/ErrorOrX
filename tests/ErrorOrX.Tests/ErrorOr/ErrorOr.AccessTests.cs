namespace ErrorOrX.Tests.ErrorOr;

/// <summary>
///     Tests for <c>ErrorOr&lt;TValue&gt;</c> state, value/error access, implicit conversions,
///     and Error factory ErrorType assignment.
/// </summary>
public class ErrorOrAccessTests
{
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
        ErrorOr<int> result = errors;

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
        ErrorOr<int> result = (Error[])[firstError, secondError];

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
        // Arrange & Act
        ErrorOr<string> result = (Error[])
        [
            Error.Validation("Test.Validation1", "First validation error"),
            Error.Validation("Test.Validation2", "Second validation error")
        ];

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
    }

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
}
