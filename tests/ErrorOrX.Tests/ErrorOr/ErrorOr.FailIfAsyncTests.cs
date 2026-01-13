namespace ErrorOrX.Tests.ErrorOr;

public class FailIfAsyncTests
{
    [Fact]
    public async Task CallingFailIfAsync_WhenFailsIf_ShouldReturnError()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        var result = await errorOrInt
            .FailIfAsync(static num => Task.FromResult(num > 3), Error.Failure());

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public async Task CallingFailIfAsyncExtensionMethod_WhenFailsIf_ShouldReturnError()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        var result = await errorOrInt
            .ThenAsync(static num => Task.FromResult(num))
            .FailIfAsync(static num => Task.FromResult(num > 3), Error.Failure());

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public async Task CallingFailIfAsync_WhenDoesNotFailIf_ShouldReturnValue()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        var result = await errorOrInt
            .FailIfAsync(static num => Task.FromResult(num > 10), Error.Failure());

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(5);
    }

    [Fact]
    public async Task CallingFailIf_WhenIsError_ShouldNotInvokeFailIfFunc()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        var result = await errorOrString
            .FailIfAsync(static str => Task.FromResult(str == string.Empty), Error.Failure());

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task CallingFailIfAsyncWithErrorBuilder_WhenFailsIf_ShouldReturnError()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        var result = await errorOrInt
            .FailIfAsync(static num => Task.FromResult(num > 3),
                static num => Task.FromResult(Error.Failure(description: $"{num} is greater than 3.")));

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
        result.FirstError.Description.Should().Be("5 is greater than 3.");
    }

    [Fact]
    public async Task CallingFailIfAsyncExtensionMethodWithErrorBuilder_WhenFailsIf_ShouldReturnError()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        var result = await errorOrInt
            .ThenAsync(static num => Task.FromResult(num))
            .FailIfAsync(static num => Task.FromResult(num > 3),
                static num => Task.FromResult(Error.Failure(description: $"{num} is greater than 3.")));

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
        result.FirstError.Description.Should().Be("5 is greater than 3.");
    }

    [Fact]
    public async Task CallingFailIfAsyncWithErrorBuilder_WhenDoesNotFailIf_ShouldReturnValue()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        var result = await errorOrInt
            .FailIfAsync(static num => Task.FromResult(num > 10),
                static num => Task.FromResult(Error.Failure(description: $"{num} is greater than 10.")));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(5);
    }

    [Fact]
    public async Task CallingFailIfWithErrorBuilder_WhenIsError_ShouldNotInvokeFailIfFunc()
    {
        // Arrange
        ErrorOr<int> errorOrInt = Error.NotFound();

        // Act
        var result = await errorOrInt
            .FailIfAsync(static num => Task.FromResult(num > 3),
                static num => Task.FromResult(Error.Failure(description: $"{num} is greater than 3.")));

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    private record Person(string Name);
}
