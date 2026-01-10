using ErrorOr.Core.ErrorOr;
using ErrorOr.Core.Errors;
using ErrorOr.Core.Results;

namespace ErrorOr.Core.Tests.ErrorOr;

public class ErrorOrInterfaceTests
{
    private record TestPerson(string Name, int Age);

    #region IErrorOr Interface - Success State

    [Fact]
    public void AccessingIErrorOrErrors_WhenNoError_ShouldReturnNull()
    {
        // Arrange
        ErrorOr<int> errorOr = 5;
        IErrorOr interfaceErrorOr = errorOr;

        // Act
        var errors = interfaceErrorOr.Errors;

        // Assert
        errors.Should().BeNull();
    }

    [Fact]
    public void AccessingIErrorOrValue_WhenNoError_ShouldReturnValue()
    {
        // Arrange
        ErrorOr<int> errorOr = 5;
        IErrorOr<int> interfaceErrorOr = errorOr;

        // Act
        var value = interfaceErrorOr.Value;

        // Assert
        value.Should().Be(5);
    }

    [Fact]
    public void AccessingIErrorOrIsError_WhenNoError_ShouldReturnFalse()
    {
        // Arrange
        ErrorOr<string> errorOr = "success";
        IErrorOr interfaceErrorOr = errorOr;

        // Act
        var isError = interfaceErrorOr.IsError;

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
        IErrorOr interfaceErrorOr = errorOr;

        // Act
        var errors = interfaceErrorOr.Errors;

        // Assert
        errors.Should().NotBeNull();
        errors.Should().ContainSingle().Which.Should().Be(error);
    }

    [Fact]
    public void AccessingIErrorOrErrors_WhenIsErrorWithMultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        List<Error> expectedErrors =
        [
            Error.Validation("Error.One", "First error"),
            Error.NotFound("Error.Two", "Second error"),
            Error.Conflict("Error.Three", "Third error")
        ];
        ErrorOr<int> errorOr = expectedErrors;
        IErrorOr interfaceErrorOr = errorOr;

        // Act
        var errors = interfaceErrorOr.Errors;

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
        IErrorOr<int> interfaceErrorOr = errorOr;

        // Act
        var act = () => interfaceErrorOr.Value;

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
        IErrorOr interfaceErrorOr = errorOr;

        // Act
        var isError = interfaceErrorOr.IsError;

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

        // Act
        IErrorOr<int> genericInterface = errorOr;
        IErrorOr baseInterface = errorOr;

        // Assert
        genericInterface.Should().BeAssignableTo<IErrorOr>();
        baseInterface.Should().NotBeNull();
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
        IErrorOr<TestPerson> interfaceErrorOr = errorOr;

        // Act
        var actualPerson = interfaceErrorOr.Value;

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

        // Act
        IErrorOr intInterface = intErrorOr;
        IErrorOr stringInterface = stringErrorOr;

        // Assert
        intInterface.IsError.Should().BeTrue();
        stringInterface.IsError.Should().BeTrue();
        intInterface.Errors!.First().Should().Be(sharedError);
        stringInterface.Errors!.First().Should().Be(sharedError);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void IErrorOrValue_WhenValueIsReferenceType_ShouldReturnSameReference()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        ErrorOr<List<int>> errorOr = list;
        IErrorOr<List<int>> interfaceErrorOr = errorOr;

        // Act
        var result = interfaceErrorOr.Value;

        // Assert
        result.Should().BeSameAs(list);
    }

    [Fact]
    public void IErrorOrErrors_WhenAccessedMultipleTimes_ShouldReturnConsistentResults()
    {
        // Arrange
        var error = Error.Conflict();
        ErrorOr<int> errorOr = error;
        IErrorOr interfaceErrorOr = errorOr;

        // Act
        var errors1 = interfaceErrorOr.Errors;
        var errors2 = interfaceErrorOr.Errors;

        // Assert
        errors1.Should().BeSameAs(errors2);
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
