namespace ErrorOrX.Tests.ErrorOr;

public class OrExtensionsTests
{
    private record Person(string Name);

    #region OrNotFound - Class

    [Fact]
    public void OrNotFound_WhenValueIsNotNull_ShouldReturnValue()
    {
        // Arrange
        var person = new Person("John");

        // Act
        var result = person.OrNotFound();

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(person);
    }

    [Fact]
    public void OrNotFound_WhenValueIsNull_ShouldReturnNotFoundError()
    {
        // Arrange
        Person? person = null;

        // Act
        var result = person.OrNotFound();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        result.FirstError.Code.Should().Be("Person.NotFound");
    }

    [Fact]
    public void OrNotFound_WhenValueIsNullWithDescription_ShouldReturnNotFoundErrorWithDescription()
    {
        // Arrange
        Person? person = null;

        // Act
        var result = person.OrNotFound("Person with ID 123 not found");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        result.FirstError.Code.Should().Be("Person.NotFound");
        result.FirstError.Description.Should().Be("Person with ID 123 not found");
    }

    #endregion

    #region OrNotFound - Struct

    [Fact]
    public void OrNotFound_WhenNullableStructHasValue_ShouldReturnValue()
    {
        // Arrange
        int? value = 42;

        // Act
        var result = value.OrNotFound();

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void OrNotFound_WhenNullableStructIsNull_ShouldReturnNotFoundError()
    {
        // Arrange
        int? value = null;

        // Act
        var result = value.OrNotFound();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        result.FirstError.Code.Should().Be("Int32.NotFound");
    }

    #endregion

    #region OrValidation

    [Fact]
    public void OrValidation_WhenValueIsNotNull_ShouldReturnValue()
    {
        // Arrange
        var person = new Person("John");

        // Act
        var result = person.OrValidation();

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(person);
    }

    [Fact]
    public void OrValidation_WhenValueIsNull_ShouldReturnValidationError()
    {
        // Arrange
        Person? person = null;

        // Act
        var result = person.OrValidation("Name is required");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Be("Person.Invalid");
        result.FirstError.Description.Should().Be("Name is required");
    }

    #endregion

    #region OrUnauthorized

    [Fact]
    public void OrUnauthorized_WhenValueIsNotNull_ShouldReturnValue()
    {
        // Arrange
        var person = new Person("John");

        // Act
        var result = person.OrUnauthorized();

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(person);
    }

    [Fact]
    public void OrUnauthorized_WhenValueIsNull_ShouldReturnUnauthorizedError()
    {
        // Arrange
        Person? person = null;

        // Act
        var result = person.OrUnauthorized();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Unauthorized);
        result.FirstError.Code.Should().Be("Person.Unauthorized");
    }

    #endregion

    #region OrForbidden

    [Fact]
    public void OrForbidden_WhenValueIsNotNull_ShouldReturnValue()
    {
        // Arrange
        var person = new Person("John");

        // Act
        var result = person.OrForbidden();

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(person);
    }

    [Fact]
    public void OrForbidden_WhenValueIsNull_ShouldReturnForbiddenError()
    {
        // Arrange
        Person? person = null;

        // Act
        var result = person.OrForbidden("Access denied");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Forbidden);
        result.FirstError.Code.Should().Be("Person.Forbidden");
        result.FirstError.Description.Should().Be("Access denied");
    }

    #endregion

    #region OrConflict

    [Fact]
    public void OrConflict_WhenValueIsNotNull_ShouldReturnValue()
    {
        // Arrange
        var person = new Person("John");

        // Act
        var result = person.OrConflict();

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(person);
    }

    [Fact]
    public void OrConflict_WhenValueIsNull_ShouldReturnConflictError()
    {
        // Arrange
        Person? person = null;

        // Act
        var result = person.OrConflict("Already exists");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Conflict);
        result.FirstError.Code.Should().Be("Person.Conflict");
        result.FirstError.Description.Should().Be("Already exists");
    }

    #endregion

    #region OrFailure

    [Fact]
    public void OrFailure_WhenValueIsNotNull_ShouldReturnValue()
    {
        // Arrange
        var person = new Person("John");

        // Act
        var result = person.OrFailure();

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(person);
    }

    [Fact]
    public void OrFailure_WhenValueIsNull_ShouldReturnFailureError()
    {
        // Arrange
        Person? person = null;

        // Act
        var result = person.OrFailure("Operation failed");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
        result.FirstError.Code.Should().Be("Person.Failure");
        result.FirstError.Description.Should().Be("Operation failed");
    }

    #endregion

    #region Real-world usage patterns

    [Fact]
    public void OrNotFound_WithListFind_ShouldWorkAsExpected()
    {
        // Arrange
        var people = new List<Person> { new("Alice"), new("Bob") };

        // Act
        var found = people.Find(p => p.Name == "Alice").OrNotFound("Person not found");
        var notFound = people.Find(p => p.Name == "Charlie").OrNotFound("Person not found");

        // Assert
        found.IsError.Should().BeFalse();
        found.Value.Name.Should().Be("Alice");

        notFound.IsError.Should().BeTrue();
        notFound.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void OrValidation_WithNullableStruct_ShouldWorkAsExpected()
    {
        // Arrange
        int? validAge = 25;
        int? invalidAge = null;

        // Act
        var valid = validAge.OrValidation("Age is required");
        var invalid = invalidAge.OrValidation("Age is required");

        // Assert
        valid.IsError.Should().BeFalse();
        valid.Value.Should().Be(25);

        invalid.IsError.Should().BeTrue();
        invalid.FirstError.Description.Should().Be("Age is required");
    }

    #endregion
}
