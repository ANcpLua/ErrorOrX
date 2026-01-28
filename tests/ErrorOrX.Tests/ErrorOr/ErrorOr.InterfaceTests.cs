namespace ErrorOrX.Tests.ErrorOr;

public class ErrorOrInterfaceTests
{
    private sealed record TestPerson(string Name, int Age);

    #region IErrorOr Interface - Success State

    [Fact]
    public void AccessingIErrorOrErrors_WhenNoError_ShouldReturnNull()
    {
        // Arrange
        ErrorOr<int> errorOr = 5;

        // Act
        var errors = ((IErrorOr)errorOr).Errors;

        // Assert
        errors.Should().BeNull();
    }

    [Fact]
    public void AccessingIErrorOrValue_WhenNoError_ShouldReturnValue()
    {
        // Arrange
        ErrorOr<int> errorOr = 5;

        // Act
        var value = errorOr.Value;

        // Assert
        value.Should().Be(5);
    }

    [Fact]
    public void AccessingIErrorOrIsError_WhenNoError_ShouldReturnFalse()
    {
        // Arrange
        ErrorOr<string> errorOr = "success";

        // Act
        var isError = errorOr.IsError;

        // Assert
        isError.Should().BeFalse();
    }

    #endregion

    #region IErrorOr Interface - Error State

    [Fact]
    public void AccessingIErrorOrErrors_WhenIsError_ShouldReturnErrors()
    {
        // Arrange
        var error = Error.Validation("Test.Error", "Test error description");
        ErrorOr<int> errorOr = error;

        // Act
        var errors = ((IErrorOr)errorOr).Errors;

        // Assert
        errors.Should().NotBeNull();
        errors.Should().ContainSingle().Which.Should().Be(error);
    }

    [Fact]
    public void AccessingIErrorOrErrors_WhenIsErrorWithMultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        Error[] expectedErrors =
        [
            Error.Validation("Error.One", "First error"),
            Error.NotFound("Error.Two", "Second error"),
            Error.Conflict("Error.Three", "Third error")
        ];
        ErrorOr<int> errorOr = expectedErrors;

        // Act
        var errors = ((IErrorOr)errorOr).Errors;

        // Assert
        errors.Should().NotBeNull();
        errors.Should().HaveCount(3);
        errors.Should().BeEquivalentTo(expectedErrors);
    }

    [Fact]
    public void AccessingIErrorOrValue_WhenIsError_ShouldThrowInvalidOperationException()
    {
        // Arrange
        ErrorOr<int> errorOr = Error.Failure();

        // Act
        var act = () => errorOr.Value;

        // Assert
        act.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage(
                "The Value property cannot be accessed when errors have been recorded. Check IsError before accessing Value.");
    }

    [Fact]
    public void AccessingIErrorOrIsError_WhenIsError_ShouldReturnTrue()
    {
        // Arrange
        ErrorOr<string> errorOr = Error.NotFound();

        // Act
        var isError = errorOr.IsError;

        // Assert
        isError.Should().BeTrue();
    }

    #endregion

    #region Interface Polymorphism Tests

    [Fact]
    public void IErrorOrGeneric_ShouldInheritFromIErrorOr()
    {
        // Arrange
        ErrorOr<int> errorOr = 42;

        // Assert
        errorOr.Should().BeAssignableTo<IErrorOr<int>>();
        errorOr.Should().BeAssignableTo<IErrorOr>();
    }

    [Fact]
    public void IErrorOr_WhenUsedPolymorphically_ShouldWorkCorrectly()
    {
        // Arrange
        ErrorOr<int> intErrorOr = 5;
        ErrorOr<string> stringErrorOr = Error.Validation();
        List<IErrorOr> errorOrList = [intErrorOr, stringErrorOr];

        // Act & Assert
        errorOrList[0].IsError.Should().BeFalse();
        errorOrList[0].Errors.Should().BeNull();

        errorOrList[1].IsError.Should().BeTrue();
        errorOrList[1].Errors.Should().ContainSingle();
    }

    [Fact]
    public void IErrorOrGeneric_WhenAccessedThroughInterface_ShouldPreserveValue()
    {
        // Arrange
        var expectedPerson = new TestPerson("John", 30);
        ErrorOr<TestPerson> errorOr = expectedPerson;

        // Act
        var actualPerson = errorOr.Value;

        // Assert
        actualPerson.Should().Be(expectedPerson);
    }

    [Fact]
    public void IErrorOr_WhenCastFromDifferentErrorOrTypes_ShouldMaintainErrorState()
    {
        // Arrange
        var sharedError = Error.Unauthorized("Auth.Failed", "Authentication failed");
        ErrorOr<int> intErrorOr = sharedError;
        ErrorOr<string> stringErrorOr = sharedError;

        // Assert
        intErrorOr.IsError.Should().BeTrue();
        stringErrorOr.IsError.Should().BeTrue();
        var intErrors = ((IErrorOr)intErrorOr).Errors;
        Unreachable.ThrowIf(intErrors is null);
        intErrors[0].Should().Be(sharedError);

        var stringErrors = ((IErrorOr)stringErrorOr).Errors;
        Unreachable.ThrowIf(stringErrors is null);
        stringErrors[0].Should().Be(sharedError);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void IErrorOrValue_WhenValueIsReferenceType_ShouldReturnSameReference()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        ErrorOr<List<int>> errorOr = list;

        // Act
        var result = errorOr.Value;

        // Assert
        result.Should().BeSameAs(list);
    }

    [Fact]
    public void IErrorOrErrors_WhenAccessedMultipleTimes_ShouldReturnConsistentResults()
    {
        // Arrange
        var error = Error.Conflict();
        ErrorOr<int> errorOr = error;

        // Act
        var errors1 = ((IErrorOr)errorOr).Errors;
        var errors2 = ((IErrorOr)errorOr).Errors;

        // Assert - ImmutableArray boxing may create different instances, but content should be equivalent
        errors1.Should().BeEquivalentTo(errors2);
        errors1.Should().ContainSingle().Which.Should().Be(error);
    }

    [Fact]
    public void IErrorOr_WhenUsedWithResultTypes_ShouldWorkCorrectly()
    {
        // Arrange
        ErrorOr<Success> successResult = Result.Success;
        ErrorOr<Created> createdResult = Result.Created;
        ErrorOr<Deleted> deletedError = Error.NotFound();
        ErrorOr<Updated> updatedError = Error.Conflict();

        // Act & Assert
        successResult.IsError.Should().BeFalse();
        createdResult.IsError.Should().BeFalse();
        deletedError.IsError.Should().BeTrue();
        updatedError.IsError.Should().BeTrue();
    }

    #endregion
}
