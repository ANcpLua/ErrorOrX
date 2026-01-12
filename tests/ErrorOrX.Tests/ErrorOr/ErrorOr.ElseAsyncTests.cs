
namespace ErrorOrX.Tests.ErrorOr;

public class ElseAsyncTests
{
    [Fact]
    public async Task CallingElseAsyncWithValueFunc_WhenIsSuccess_ShouldNotInvokeElseFunc()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";

        // Act
        var result = await errorOrString
            .ThenAsync(Convert.ToIntAsync)
            .ThenAsync(Convert.ToStringAsync)
            .ElseAsync(static errors => Task.FromResult($"Error count: {errors.Count}"));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEquivalentTo(errorOrString.Value);
    }

    [Fact]
    public async Task CallingElseAsyncWithValueFunc_WhenIsError_ShouldInvokeElseFunc()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        var result = await errorOrString
            .ThenAsync(Convert.ToIntAsync)
            .ThenAsync(Convert.ToStringAsync)
            .ElseAsync(static errors => Task.FromResult($"Error count: {errors.Count}"));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEquivalentTo("Error count: 1");
    }

    [Fact]
    public async Task CallingElseAsyncWithValue_WhenIsSuccess_ShouldNotReturnElseValue()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";

        // Act
        var result = await errorOrString
            .ThenAsync(Convert.ToIntAsync)
            .ThenAsync(Convert.ToStringAsync)
            .ElseAsync(Task.FromResult("oh no"));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEquivalentTo(errorOrString.Value);
    }

    [Fact]
    public async Task CallingElseAsyncWithValue_WhenIsError_ShouldInvokeElseFunc()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        var result = await errorOrString
            .ThenAsync(Convert.ToIntAsync)
            .ThenAsync(Convert.ToStringAsync)
            .ElseAsync(Task.FromResult("oh no"));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEquivalentTo("oh no");
    }

    [Fact]
    public async Task CallingElseAsyncWithError_WhenIsError_ShouldReturnElseError()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        var result = await errorOrString
            .ThenAsync(Convert.ToIntAsync)
            .ThenAsync(Convert.ToStringAsync)
            .ElseAsync(Task.FromResult(Error.Unexpected()));

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Unexpected);
    }

    [Fact]
    public async Task CallingElseAsyncWithError_WhenIsSuccess_ShouldNotReturnElseError()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";

        // Act
        var result = await errorOrString
            .ThenAsync(Convert.ToIntAsync)
            .ThenAsync(Convert.ToStringAsync)
            .ElseAsync(Task.FromResult(Error.Unexpected()));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(errorOrString.Value);
    }

    [Fact]
    public async Task CallingElseAsyncWithErrorFunc_WhenIsError_ShouldReturnElseError()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        var result = await errorOrString
            .ThenAsync(Convert.ToIntAsync)
            .ThenAsync(Convert.ToStringAsync)
            .ElseAsync(static errors => Task.FromResult(Error.Unexpected()));

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Unexpected);
    }

    [Fact]
    public async Task CallingElseAsyncWithErrorFunc_WhenIsSuccess_ShouldNotReturnElseError()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";

        // Act
        var result = await errorOrString
            .ThenAsync(Convert.ToIntAsync)
            .ThenAsync(Convert.ToStringAsync)
            .ElseAsync(static errors => Task.FromResult(Error.Unexpected()));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(errorOrString.Value);
    }

    [Fact]
    public async Task CallingElseAsyncWithErrorFunc_WhenIsError_ShouldReturnElseErrors()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        var result = await errorOrString
            .ThenAsync(Convert.ToIntAsync)
            .ThenAsync(Convert.ToStringAsync)
            .ElseAsync(static errors => Task.FromResult<IReadOnlyList<Error>>(new List<Error> { Error.Unexpected() }));

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Unexpected);
    }

    [Fact]
    public async Task CallingElseAsyncWithErrorFunc_WhenIsSuccess_ShouldNotReturnElseErrors()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";

        // Act
        var result = await errorOrString
            .ThenAsync(Convert.ToIntAsync)
            .ThenAsync(Convert.ToStringAsync)
            .ElseAsync(static errors => Task.FromResult<IReadOnlyList<Error>>(new List<Error> { Error.Unexpected() }));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(errorOrString.Value);
    }

    #region ElseDoAsync Tests

    [Fact]
    public async Task CallingElseDoAsync_WhenIsError_ShouldExecuteAction()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();
        var actionExecuted = false;

        // Act
        var result = await errorOrString
            .ElseDoAsync(async errors =>
            {
                await Task.Delay(1);
                actionExecuted = true;
            });

        // Assert
        result.IsError.Should().BeTrue();
        actionExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task CallingElseDoAsync_WhenIsSuccess_ShouldNotExecuteAction()
    {
        // Arrange
        ErrorOr<string> errorOrString = "success";
        var actionExecuted = false;

        // Act
        var result = await errorOrString
            .ElseDoAsync(async errors =>
            {
                await Task.Delay(1);
                actionExecuted = true;
            });

        // Assert
        result.IsError.Should().BeFalse();
        actionExecuted.Should().BeFalse();
        result.Value.Should().Be("success");
    }

    [Fact]
    public async Task CallingElseDoAsync_WhenIsError_ShouldReturnOriginalErrorOr()
    {
        // Arrange
        var originalError = Error.Validation("Original.Error", "Original error description");
        ErrorOr<string> errorOrString = originalError;

        // Act
        var result = await errorOrString
            .ElseDoAsync(async _ => await Task.CompletedTask);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(originalError);
    }

    [Fact]
    public async Task CallingElseDoAsync_WhenIsError_ShouldReceiveAllErrors()
    {
        // Arrange
        List<Error> expectedErrors =
        [
            Error.Validation("Error.One", "First"),
            Error.Conflict("Error.Two", "Second")
        ];
        ErrorOr<string> errorOrString = expectedErrors;
        IReadOnlyList<Error>? receivedErrors = null;

        // Act
        await errorOrString.ElseDoAsync(async errors =>
        {
            await Task.CompletedTask;
            receivedErrors = errors;
        });

        // Assert
        receivedErrors.Should().NotBeNull();
        receivedErrors.Should().HaveCount(2);
        receivedErrors.Should().BeEquivalentTo(expectedErrors);
    }

    #endregion

    #region Direct ElseAsync Instance Method Tests

    [Fact]
    public async Task CallingElseAsync_WithTaskValue_WhenIsError_ShouldReturnTaskValue()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        var result = await errorOrString.ElseAsync(Task.FromResult("fallback value"));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be("fallback value");
    }

    [Fact]
    public async Task CallingElseAsync_WithTaskValue_WhenIsSuccess_ShouldReturnOriginalValue()
    {
        // Arrange
        ErrorOr<string> errorOrString = "original";

        // Act
        var result = await errorOrString.ElseAsync(Task.FromResult("fallback value"));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be("original");
    }

    [Fact]
    public async Task CallingElseAsync_WithTaskError_WhenIsError_ShouldReturnTaskError()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();
        var expectedError = Error.Unexpected("Custom.Error", "Custom description");

        // Act
        var result = await errorOrString.ElseAsync(Task.FromResult(expectedError));

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expectedError);
    }

    [Fact]
    public async Task CallingElseAsync_WithTaskError_WhenIsSuccess_ShouldReturnOriginalValue()
    {
        // Arrange
        ErrorOr<string> errorOrString = "original";

        // Act
        var result = await errorOrString.ElseAsync(Task.FromResult(Error.Unexpected()));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be("original");
    }

    [Fact]
    public async Task CallingElseAsync_WithErrorListFunc_WhenIsError_ShouldReturnErrors()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        var result = await errorOrString.ElseAsync(static _ =>
            Task.FromResult<IReadOnlyList<Error>>(new List<Error>
            {
                Error.Validation("New.Error.One", "First"), Error.Validation("New.Error.Two", "Second")
            }));

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public async Task CallingElseAsync_WithErrorListFunc_WhenIsSuccess_ShouldReturnOriginalValue()
    {
        // Arrange
        ErrorOr<string> errorOrString = "original";

        // Act
        var result = await errorOrString.ElseAsync(static _ =>
            Task.FromResult<IReadOnlyList<Error>>(new List<Error> { Error.Validation() }));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be("original");
    }

    #endregion
}
