namespace ErrorOrX.Tests.Errors;

public class ErrorTests
{
    private const string ErrorCode = "ErrorCode";
    private const string ErrorDescription = "ErrorDescription";

    private static readonly Dictionary<string, object> Dictionary = new() { { "key1", "value1" }, { "key2", 21 } };

    #region Factory Methods with Custom Parameters

    [Fact]
    public void CreateError_WhenFailureError_ShouldHaveErrorTypeFailure()
    {
        // Act
        var error = Error.Failure(ErrorCode, ErrorDescription, Dictionary);

        // Assert
        ValidateError(error, ErrorType.Failure);
    }

    [Fact]
    public void CreateError_WhenUnexpectedError_ShouldHaveErrorTypeFailure()
    {
        // Act
        var error = Error.Unexpected(ErrorCode, ErrorDescription, Dictionary);

        // Assert
        ValidateError(error, ErrorType.Unexpected);
    }

    [Fact]
    public void CreateError_WhenValidationError_ShouldHaveErrorTypeValidation()
    {
        // Act
        var error = Error.Validation(ErrorCode, ErrorDescription, Dictionary);

        // Assert
        ValidateError(error, ErrorType.Validation);
    }

    [Fact]
    public void CreateError_WhenConflictError_ShouldHaveErrorTypeConflict()
    {
        // Act
        var error = Error.Conflict(ErrorCode, ErrorDescription, Dictionary);

        // Assert
        ValidateError(error, ErrorType.Conflict);
    }

    [Fact]
    public void CreateError_WhenNotFoundError_ShouldHaveErrorTypeNotFound()
    {
        // Act
        var error = Error.NotFound(ErrorCode, ErrorDescription, Dictionary);

        // Assert
        ValidateError(error, ErrorType.NotFound);
    }

    [Fact]
    public void CreateError_WhenNotAuthorizedError_ShouldHaveErrorTypeUnauthorized()
    {
        // Act
        var error = Error.Unauthorized(ErrorCode, ErrorDescription, Dictionary);

        // Assert
        ValidateError(error, ErrorType.Unauthorized);
    }

    [Fact]
    public void CreateError_WhenForbiddenError_ShouldHaveErrorTypeForbidden()
    {
        // Act
        var error = Error.Forbidden(ErrorCode, ErrorDescription, Dictionary);

        // Assert
        ValidateError(error, ErrorType.Forbidden);
    }

    [Fact]
    public void CreateError_WhenCustomType_ShouldHaveCustomErrorType()
    {
        // Act
        var error = Error.Custom(1232, ErrorCode, ErrorDescription, Dictionary);

        // Assert
        ValidateError(error, (ErrorType)1232);
    }

    private static void ValidateError(Error error, ErrorType expectedErrorType)
    {
        error.Code.Should().Be(ErrorCode);
        error.Description.Should().Be(ErrorDescription);
        error.Type.Should().Be(expectedErrorType);
        ((int)error.Type).Should().Be((int)expectedErrorType);
        error.Metadata.Should().BeEquivalentTo(Dictionary);
    }

    #endregion

    #region Factory Methods with Default Parameters

    [Fact]
    public void CreateError_WhenFailureWithDefaults_ShouldHaveDefaultValues()
    {
        // Act
        var error = Error.Failure();

        // Assert
        error.Code.Should().Be("General.Failure");
        error.Description.Should().Be("A failure has occurred.");
        error.Type.Should().Be(ErrorType.Failure);
        ((int)error.Type).Should().Be((int)ErrorType.Failure);
        error.Metadata.Should().BeNull();
    }

    [Fact]
    public void CreateError_WhenUnexpectedWithDefaults_ShouldHaveDefaultValues()
    {
        // Act
        var error = Error.Unexpected();

        // Assert
        error.Code.Should().Be("General.Unexpected");
        error.Description.Should().Be("An unexpected error has occurred.");
        error.Type.Should().Be(ErrorType.Unexpected);
        ((int)error.Type).Should().Be((int)ErrorType.Unexpected);
        error.Metadata.Should().BeNull();
    }

    [Fact]
    public void CreateError_WhenValidationWithDefaults_ShouldHaveDefaultValues()
    {
        // Act
        var error = Error.Validation();

        // Assert
        error.Code.Should().Be("General.Validation");
        error.Description.Should().Be("A validation error has occurred.");
        error.Type.Should().Be(ErrorType.Validation);
        ((int)error.Type).Should().Be((int)ErrorType.Validation);
        error.Metadata.Should().BeNull();
    }

    [Fact]
    public void CreateError_WhenConflictWithDefaults_ShouldHaveDefaultValues()
    {
        // Act
        var error = Error.Conflict();

        // Assert
        error.Code.Should().Be("General.Conflict");
        error.Description.Should().Be("A conflict error has occurred.");
        error.Type.Should().Be(ErrorType.Conflict);
        ((int)error.Type).Should().Be((int)ErrorType.Conflict);
        error.Metadata.Should().BeNull();
    }

    [Fact]
    public void CreateError_WhenNotFoundWithDefaults_ShouldHaveDefaultValues()
    {
        // Act
        var error = Error.NotFound();

        // Assert
        error.Code.Should().Be("General.NotFound");
        error.Description.Should().Be("A 'Not Found' error has occurred.");
        error.Type.Should().Be(ErrorType.NotFound);
        ((int)error.Type).Should().Be((int)ErrorType.NotFound);
        error.Metadata.Should().BeNull();
    }

    [Fact]
    public void CreateError_WhenUnauthorizedWithDefaults_ShouldHaveDefaultValues()
    {
        // Act
        var error = Error.Unauthorized();

        // Assert
        error.Code.Should().Be("General.Unauthorized");
        error.Description.Should().Be("An 'Unauthorized' error has occurred.");
        error.Type.Should().Be(ErrorType.Unauthorized);
        ((int)error.Type).Should().Be((int)ErrorType.Unauthorized);
        error.Metadata.Should().BeNull();
    }

    [Fact]
    public void CreateError_WhenForbiddenWithDefaults_ShouldHaveDefaultValues()
    {
        // Act
        var error = Error.Forbidden();

        // Assert
        error.Code.Should().Be("General.Forbidden");
        error.Description.Should().Be("A 'Forbidden' error has occurred.");
        error.Type.Should().Be(ErrorType.Forbidden);
        ((int)error.Type).Should().Be((int)ErrorType.Forbidden);
        error.Metadata.Should().BeNull();
    }

    #endregion

    #region Custom Error Type Tests

    [Fact]
    public void CreateError_WhenCustomWithNegativeType_ShouldHandleNegativeType()
    {
        // Arrange
        const int NegativeType = -1;
        const string Code = "Custom.Negative";
        const string Description = "Negative type error";

        // Act
        var error = Error.Custom(NegativeType, Code, Description);

        // Assert
        ((int)error.Type).Should().Be(NegativeType);
        error.Type.Should().Be((ErrorType)NegativeType);
        error.Code.Should().Be(Code);
        error.Description.Should().Be(Description);
        error.Metadata.Should().BeNull();
    }

    [Fact]
    public void CreateError_WhenCustomWithExistingEnumValue_ShouldMapToCorrectErrorType()
    {
        // Arrange - using the numeric value of ErrorType.Validation
        const int ValidationTypeValue = (int)ErrorType.Validation;
        const string Code = "Custom.MappedToValidation";
        const string Description = "Custom error that maps to validation";

        // Act
        var error = Error.Custom(ValidationTypeValue, Code, Description);

        // Assert
        error.Type.Should().Be(ErrorType.Validation);
        ((int)error.Type).Should().Be(ValidationTypeValue);
    }

    [Fact]
    public void CreateError_WhenCustomWithoutMetadata_ShouldHaveNullMetadata()
    {
        // Act
        var error = Error.Custom(999, "Custom.Code", "Custom description");

        // Assert
        error.Metadata.Should().BeNull();
    }

    #endregion

    #region Properties Tests

    [Theory]
    [InlineData(ErrorType.Failure)]
    [InlineData(ErrorType.Unexpected)]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.Conflict)]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Unauthorized)]
    [InlineData(ErrorType.Forbidden)]
    public void AllErrorTypes_ShouldHaveMatchingTypeValue(ErrorType expectedType)
    {
        // Arrange
        var error = expectedType switch
        {
            ErrorType.Failure => Error.Failure(),
            ErrorType.Unexpected => Error.Unexpected(),
            ErrorType.Validation => Error.Validation(),
            ErrorType.Conflict => Error.Conflict(),
            ErrorType.NotFound => Error.NotFound(),
            ErrorType.Unauthorized => Error.Unauthorized(),
            ErrorType.Forbidden => Error.Forbidden(),
            _ => throw new ArgumentOutOfRangeException(nameof(expectedType))
        };

        // Act & Assert
        error.Type.Should().Be(expectedType);
        ((int)error.Type).Should().Be((int)expectedType);
    }

    [Fact]
    public void Metadata_WhenAccessedMultipleTimes_ShouldReturnSameReference()
    {
        // Arrange
        var metadata = new Dictionary<string, object> { { "key", "value" } };
        var error = Error.Failure(metadata: metadata);

        // Act
        var firstAccess = error.Metadata;
        var secondAccess = error.Metadata;

        // Assert - FrozenDictionary is immutable so same instance is returned
        firstAccess.Should().BeSameAs(secondAccess);
        // Note: Original dictionary is copied to FrozenDictionary, so content equals but not same reference
        firstAccess.Should().BeEquivalentTo(metadata);
    }

    [Fact]
    public void Metadata_WhenContainsComplexObjects_ShouldPreserveValues()
    {
        // Arrange
        var complexMetadata = new Dictionary<string, object>
        {
            { "stringValue", "test" },
            { "intValue", 42 },
            { "boolValue", true },
            { "doubleValue", 3.14 },
            { "arrayValue", new[] { 1, 2, 3 } },
            { "nestedDict", new Dictionary<string, object> { { "nested", "value" } } }
        };

        // Act
        var error = Error.Validation(metadata: complexMetadata);

        // Assert
        error.Metadata.Should().BeEquivalentTo(complexMetadata);
        error.Metadata["stringValue"].Should().Be("test");
        error.Metadata["intValue"].Should().Be(42);
        error.Metadata["boolValue"].Should().Be(true);
    }

    #endregion
}
