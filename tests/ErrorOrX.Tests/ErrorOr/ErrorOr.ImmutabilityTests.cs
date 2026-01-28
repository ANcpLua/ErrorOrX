namespace ErrorOrX.Tests.ErrorOr;

public class ErrorOrImmutabilityTests
{
    [Fact]
    public void CreateFromErrorList_WhenOriginalListIsModified_ErrorOrShouldNotChange()
    {
        // Arrange
        var initialError = Error.Validation("User.Name", "Name is too short");
        var errors = new List<Error> { initialError };
        var errorOr = errors.ToErrorOr<int>();

        // Act
        errors.Add(Error.NotFound("User.NotFound", "User not found"));

        // Assert
        // If it's vulnerable (current state), this might fail or pass depending on how we check
        // We WANT it to have only 1 error, but if it's broken it will have 2.
        errorOr.Errors.Should().HaveCount(1,
            "because ErrorOr should be immutable and not reflect changes to the original list");
        errorOr.Errors.Should().ContainSingle().Which.Should().Be(initialError);
    }
}
