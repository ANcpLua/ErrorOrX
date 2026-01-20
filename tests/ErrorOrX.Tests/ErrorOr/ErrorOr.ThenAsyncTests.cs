namespace ErrorOrX.Tests.ErrorOr;

public class ThenAsyncTests
{
    [Fact]
    public async Task CallingThenAsync_WhenIsSuccess_ShouldInvokeNextThen()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";

        // Act
        var result = await errorOrString
            .ThenAsync(Convert.ToIntAsync)
            .ThenAsync(static num => Task.FromResult(num * 2))
            .ThenDoAsync(static _ => Task.CompletedTask)
            .ThenAsync(Convert.ToStringAsync);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEquivalentTo("10");
    }

    [Fact]
    public async Task CallingThenAsync_WhenIsError_ShouldReturnErrors()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        var result = await errorOrString
            .ThenAsync(Convert.ToIntAsync)
            .ThenAsync(static num => Task.FromResult(num * 2))
            .ThenDoAsync(static _ => Task.CompletedTask)
            .ThenAsync(Convert.ToStringAsync);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().BeEquivalentTo(errorOrString.FirstError);
    }
}
