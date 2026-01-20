namespace ErrorOrX.Tests.ErrorOr;

public class FailIfTests
{
    [Fact]
    public void CallingFailIf_WhenFailsIf_ShouldReturnError()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        var result = errorOrInt
            .FailIf(static num => num > 3, Error.Failure());

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public async Task CallingFailIfExtensionMethod_WhenFailsIf_ShouldReturnError()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        var result = await errorOrInt
            .ThenAsync(static num => Task.FromResult(num))
            .FailIf(static num => num > 3, Error.Failure());

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public void CallingFailIf_WhenDoesNotFailIf_ShouldReturnValue()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        var result = errorOrInt
            .FailIf(static num => num > 10, Error.Failure());

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(5);
    }

    [Fact]
    public void CallingFailIf_WhenIsError_ShouldNotInvokeFailIfFunc()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        var result = errorOrString
            .FailIf(static str => string.IsNullOrEmpty(str), Error.Failure());

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void CallingFailIfWithErrorBuilder_WhenFailsIf_ShouldReturnError()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        var result = errorOrInt
            .FailIf(static num => num > 3, static num => Error.Failure(description: $"{num} is greater than 3"));

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
        result.FirstError.Description.Should().Be("5 is greater than 3");
    }

    [Fact]
    public async Task CallingFailIfExtensionMethodWithErrorBuilder_WhenFailsIf_ShouldReturnError()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        var result = await errorOrInt
            .ThenAsync(static num => Task.FromResult(num))
            .FailIf(static num => num > 3, static num => Error.Failure(description: $"{num} is greater than 3"));

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
        result.FirstError.Description.Should().Be("5 is greater than 3");
    }

    [Fact]
    public void CallingFailIfWithErrorBuilder_WhenDoesNotFailIf_ShouldReturnValue()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        var result = errorOrInt
            .FailIf(static num => num > 10, static num => Error.Failure(description: $"{num} is greater than 10"));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(5);
    }

    [Fact]
    public void CallingFailIfWithErrorBuilder_WhenIsError_ShouldNotInvokeFailIfFunc()
    {
        // Arrange
        ErrorOr<int> errorOrInt = Error.NotFound();

        // Act
        var result = errorOrInt
            .FailIf(static num => num > 3, static num => Error.Failure(description: $"{num} is greater than 3"));

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }
}
