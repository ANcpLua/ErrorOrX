namespace Tests;

using ErrorOr;
using FluentAssertions;

public class TypedResultsTests
{
    #region Extensions Property Tests

    [Fact]
    public void Extensions_ShouldNotBeNull()
    {
        // Act
        IResultExtensions extensions = TypedResults.Extensions;

        // Assert
        extensions.Should().NotBeNull();
    }

    [Fact]
    public void Extensions_ShouldBeOfExpectedType()
    {
        // Act
        IResultExtensions extensions = TypedResults.Extensions;

        // Assert
        extensions.Should().BeAssignableTo<IResultExtensions>();
    }

    [Fact]
    public void Extensions_WhenAccessedMultipleTimes_ShouldReturnSameInstance()
    {
        // Act
        IResultExtensions extensions1 = TypedResults.Extensions;
        IResultExtensions extensions2 = TypedResults.Extensions;

        // Assert
        extensions1.Should().BeSameAs(extensions2);
    }

    #endregion

    #region IResultExtensions Extensibility Pattern Tests

    [Fact]
    public void IResultExtensions_ShouldBeExtensibleViaExtensionMethods()
    {
        // Arrange
        IResultExtensions extensions = TypedResults.Extensions;

        // Act - Using the test extension method
        string result = extensions.CustomTestExtension("test input");

        // Assert
        result.Should().Be("Extended: test input");
    }

    [Fact]
    public void IResultExtensions_ShouldSupportChainedExtensionMethods()
    {
        // Arrange
        IResultExtensions extensions = TypedResults.Extensions;

        // Act
        int result = extensions.TransformToInt("42");

        // Assert
        result.Should().Be(42);
    }

    #endregion

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
        Success success = Result.Success;

        // Assert
        success.Should().Be(default(Success));
    }

    [Fact]
    public void ResultCreated_ShouldReturnDefaultCreatedStruct()
    {
        // Act
        Created created = Result.Created;

        // Assert
        created.Should().Be(default(Created));
    }

    [Fact]
    public void ResultDeleted_ShouldReturnDefaultDeletedStruct()
    {
        // Act
        Deleted deleted = Result.Deleted;

        // Assert
        deleted.Should().Be(default(Deleted));
    }

    [Fact]
    public void ResultUpdated_ShouldReturnDefaultUpdatedStruct()
    {
        // Act
        Updated updated = Result.Updated;

        // Assert
        updated.Should().Be(default(Updated));
    }

    [Fact]
    public void ResultTypes_ShouldBeEqualWhenCompared()
    {
        // Arrange & Act
        Success success1 = Result.Success;
        Success success2 = Result.Success;
        Created created1 = Result.Created;
        Created created2 = Result.Created;

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
        ErrorOr<Success> result = SimulateSuccessfulOperation();

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Result.Success);
    }

    [Fact]
    public void MethodReturningErrorOrSuccess_WhenOperationFails_ShouldReturnError()
    {
        // Arrange & Act
        ErrorOr<Success> result = SimulateFailedOperation();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Operation.Failed");
    }

    [Fact]
    public void MethodReturningErrorOrCreated_WhenResourceCreated_ShouldReturnCreated()
    {
        // Arrange & Act
        ErrorOr<Created> result = SimulateResourceCreation(shouldSucceed: true);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Result.Created);
    }

    [Fact]
    public void MethodReturningErrorOrCreated_WhenResourceExists_ShouldReturnConflict()
    {
        // Arrange & Act
        ErrorOr<Created> result = SimulateResourceCreation(shouldSucceed: false);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Conflict);
    }

    #endregion

    #region Helper Methods

    private static ErrorOr<Success> SimulateSuccessfulOperation()
    {
        return Result.Success;
    }

    private static ErrorOr<Success> SimulateFailedOperation()
    {
        return Error.Failure("Operation.Failed", "The operation failed");
    }

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

/// <summary>
/// Extension methods for IResultExtensions to demonstrate extensibility pattern.
/// </summary>
public static class TestResultExtensions
{
    public static string CustomTestExtension(this IResultExtensions _, string input)
    {
        return $"Extended: {input}";
    }

    public static int TransformToInt(this IResultExtensions _, string input)
    {
        return int.Parse(input);
    }
}