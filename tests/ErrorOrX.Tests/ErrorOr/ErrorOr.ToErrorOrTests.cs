namespace ErrorOrX.Tests.ErrorOr;

public class ToErrorOrTests
{
    [Fact]
    public void ValueToErrorOr_WhenAccessingValue_ShouldReturnValue()
    {
        // Arrange
        const int Value = 5;

        // Act
        var result = Value.ToErrorOr();

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Value);
    }

    [Fact]
    public void ErrorToErrorOr_WhenAccessingFirstError_ShouldReturnSameError()
    {
        // Arrange
        var error = Error.Unauthorized();

        // Act
        var result = error.ToErrorOr<int>();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(error);
    }

    [Fact]
    public void ListOfErrorsToErrorOr_WhenAccessingErrors_ShouldReturnSameErrors()
    {
        // Arrange
        List<Error> errors = [Error.Unauthorized(), Error.Validation()];

        // Act
        var result = errors.ToErrorOr<int>();

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void ArrayOfErrorsToErrorOr_WhenAccessingErrors_ShouldReturnSimilarErrors()
    {
        Error[] errors = [Error.Unauthorized(), Error.Validation()];

        var result = errors.ToErrorOr<int>();

        result.IsError.Should().BeTrue();
        result.Errors.Should().Equal(errors);
    }
}
