using ErrorOr.Core.ErrorOr;
using ErrorOr.Core.Errors;

namespace ErrorOr.Core.Tests.ErrorOr;

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
            .ThenDoAsync(static num => Task.Run(static () => { _ = 5; }))
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
            .ThenDoAsync(static num => Task.Run(static () => { _ = 5; }))
            .ThenAsync(Convert.ToStringAsync);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().BeEquivalentTo(errorOrString.FirstError);
    }
}
