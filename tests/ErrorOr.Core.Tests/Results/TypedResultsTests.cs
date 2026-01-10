using ErrorOr.Core.ErrorOr;
using ErrorOr.Core.Errors;
using ErrorOr.Core.Results;

namespace ErrorOr.Core.Tests.Results;

public class ResultTypesTests
{
    #region Integration with ErrorOr<Result> Types

    [Fact]
    public void ErrorOrSuccess_WhenSuccess_ShouldContainSuccessValue()
    {
        // Arrange
        ErrorOr<Success> errorOr = Result.Success;

        // Act & Assert
        errorOr.IsError.Should().BeFalse();
        errorOr.Value.Should().Be(Result.Success);
    }

    [Fact]
    public void ErrorOrSuccess_WhenError_ShouldContainError()
    {
        // Arrange
        ErrorOr<Success> errorOr = Error.Failure("Operation.Failed", "The operation failed");

        // Act & Assert
        errorOr.IsError.Should().BeTrue();
        errorOr.FirstError.Code.Should().Be("Operation.Failed");
    }

    [Fact]
    public void ErrorOrCreated_WhenSuccess_ShouldContainCreatedValue()
    {
        // Arrange
        ErrorOr<Created> errorOr = Result.Created;

        // Act & Assert
        errorOr.IsError.Should().BeFalse();
        errorOr.Value.Should().Be(Result.Created);
    }

    [Fact]
    public void ErrorOrCreated_WhenError_ShouldContainError()
    {
        // Arrange
        ErrorOr<Created> errorOr = Error.Conflict("Resource.AlreadyExists", "Resource already exists");

        // Act & Assert
        errorOr.IsError.Should().BeTrue();
        errorOr.FirstError.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public void ErrorOrDeleted_WhenSuccess_ShouldContainDeletedValue()
    {
        // Arrange
        ErrorOr<Deleted> errorOr = Result.Deleted;

        // Act & Assert
        errorOr.IsError.Should().BeFalse();
        errorOr.Value.Should().Be(Result.Deleted);
    }

    [Fact]
    public void ErrorOrDeleted_WhenError_ShouldContainError()
    {
        // Arrange
        ErrorOr<Deleted> errorOr = Error.NotFound("Resource.NotFound", "Resource not found");

        // Act & Assert
        errorOr.IsError.Should().BeTrue();
        errorOr.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void ErrorOrUpdated_WhenSuccess_ShouldContainUpdatedValue()
    {
        // Arrange
        ErrorOr<Updated> errorOr = Result.Updated;

        // Act & Assert
        errorOr.IsError.Should().BeFalse();
        errorOr.Value.Should().Be(Result.Updated);
    }

    [Fact]
    public void ErrorOrUpdated_WhenError_ShouldContainError()
    {
        // Arrange
        ErrorOr<Updated> errorOr = Error.Validation("Update.Invalid", "Invalid update data");

        // Act & Assert
        errorOr.IsError.Should().BeTrue();
        errorOr.FirstError.Type.Should().Be(ErrorType.Validation);
    }

    #endregion

    #region Result Static Properties Tests

    [Fact]
    public void ResultSuccess_ShouldReturnDefaultSuccessStruct()
    {
        // Act
        var success = Result.Success;

        // Assert
        success.Should().Be(default(Success));
    }

    [Fact]
    public void ResultCreated_ShouldReturnDefaultCreatedStruct()
    {
        // Act
        var created = Result.Created;

        // Assert
        created.Should().Be(default(Created));
    }

    [Fact]
    public void ResultDeleted_ShouldReturnDefaultDeletedStruct()
    {
        // Act
        var deleted = Result.Deleted;

        // Assert
        deleted.Should().Be(default(Deleted));
    }

    [Fact]
    public void ResultUpdated_ShouldReturnDefaultUpdatedStruct()
    {
        // Act
        var updated = Result.Updated;

        // Assert
        updated.Should().Be(default(Updated));
    }

    [Fact]
    public void ResultTypes_ShouldBeEqualWhenCompared()
    {
        // Arrange & Act
        var success1 = Result.Success;
        var success2 = Result.Success;
        var created1 = Result.Created;
        var created2 = Result.Created;

        // Assert
        success1.Should().Be(success2);
        (success1 == success2).Should().BeTrue();
        created1.Should().Be(created2);
        (created1 == created2).Should().BeTrue();
    }

    #endregion

    #region Method Returning ErrorOr<Result> Pattern Tests

    [Fact]
    public void MethodReturningErrorOrSuccess_WhenOperationSucceeds_ShouldReturnSuccess()
    {
        // Arrange & Act
        var result = SimulateSuccessfulOperation();

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Result.Success);
    }

    [Fact]
    public void MethodReturningErrorOrSuccess_WhenOperationFails_ShouldReturnError()
    {
        // Arrange & Act
        var result = SimulateFailedOperation();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Operation.Failed");
    }

    [Fact]
    public void MethodReturningErrorOrCreated_WhenResourceCreated_ShouldReturnCreated()
    {
        // Arrange & Act
        var result = SimulateResourceCreation(true);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Result.Created);
    }

    [Fact]
    public void MethodReturningErrorOrCreated_WhenResourceExists_ShouldReturnConflict()
    {
        // Arrange & Act
        var result = SimulateResourceCreation(false);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Conflict);
    }

    #endregion

    #region Helper Methods

    private static ErrorOr<Success> SimulateSuccessfulOperation() => Result.Success;

    private static ErrorOr<Success> SimulateFailedOperation() =>
        Error.Failure("Operation.Failed", "The operation failed");

    private static ErrorOr<Created> SimulateResourceCreation(bool shouldSucceed)
    {
        if (shouldSucceed)
        {
            return Result.Created;
        }

        return Error.Conflict("Resource.AlreadyExists", "The resource already exists");
    }

    #endregion
}
