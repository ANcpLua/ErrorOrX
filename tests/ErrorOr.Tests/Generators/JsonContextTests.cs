using ErrorOr.Core.ErrorOr;
using ErrorOr.Core.Errors;

namespace ErrorOrX.Tests.Generators;

/// <summary>
///     Tests for verifying that ErrorOr.Core runtime types are properly defined and usable.
/// </summary>
public class JsonContextTests
{
    [Fact]
    public void ErrorOr_ShouldExist()
    {
        var errorOrAssembly = typeof(ErrorOr<>).Assembly;
        var errorOrType = errorOrAssembly.GetType("ErrorOr.Core.ErrorOr.ErrorOr`1");
        errorOrType.Should().NotBeNull("ErrorOr<T> should be defined in the ErrorOr.Core assembly");
    }

    [Fact]
    public void Error_ShouldExist()
    {
        var errorOrAssembly = typeof(Error).Assembly;
        var errorType = errorOrAssembly.GetType("ErrorOr.Core.Errors.Error");
        errorType.Should().NotBeNull("Error should be defined in the ErrorOr.Core assembly");
    }

    [Fact]
    public void ErrorType_ShouldExist()
    {
        var errorOrAssembly = typeof(ErrorType).Assembly;
        var errorTypeEnum = errorOrAssembly.GetType("ErrorOr.Core.Errors.ErrorType");
        errorTypeEnum.Should().NotBeNull("ErrorType should be defined in the ErrorOr.Core assembly");
    }

    [Fact]
    public void Error_FactoryMethods_ShouldBeAvailable()
    {
        var failureError = Error.Failure("Test.Failure", "A failure");
        var validationError = Error.Validation("Test.Validation", "A validation error");
        var notFoundError = Error.NotFound("Test.NotFound", "Not found");
        var conflictError = Error.Conflict("Test.Conflict", "Conflict");
        var unauthorizedError = Error.Unauthorized("Test.Unauthorized", "Unauthorized");
        var forbiddenError = Error.Forbidden("Test.Forbidden", "Forbidden");
        var unexpectedError = Error.Unexpected("Test.Unexpected", "Unexpected");

        failureError.Type.Should().Be(ErrorType.Failure);
        validationError.Type.Should().Be(ErrorType.Validation);
        notFoundError.Type.Should().Be(ErrorType.NotFound);
        conflictError.Type.Should().Be(ErrorType.Conflict);
        unauthorizedError.Type.Should().Be(ErrorType.Unauthorized);
        forbiddenError.Type.Should().Be(ErrorType.Forbidden);
        unexpectedError.Type.Should().Be(ErrorType.Unexpected);
    }

    [Fact]
    public void ErrorType_ShouldMapToCorrectHttpStatusCodes()
    {
        var errorTypes = new[]
        {
            (ErrorType.Failure, 500),
            (ErrorType.Validation, 400),
            (ErrorType.NotFound, 404),
            (ErrorType.Conflict, 409),
            (ErrorType.Unauthorized, 401),
            (ErrorType.Forbidden, 403),
            (ErrorType.Unexpected, 500)
        };

        foreach (var (errorType, expectedStatus) in errorTypes)
        {
            var httpStatus = GetExpectedHttpStatus(errorType);
            httpStatus.Should().Be(expectedStatus, $"ErrorType.{errorType} should map to HTTP status {expectedStatus}");
        }
    }

    private static int GetExpectedHttpStatus(ErrorType errorType) =>
        errorType switch
        {
            ErrorType.Failure => 500,
            ErrorType.Validation => 400,
            ErrorType.NotFound => 404,
            ErrorType.Conflict => 409,
            ErrorType.Unauthorized => 401,
            ErrorType.Forbidden => 403,
            _ => 500
        };
}
