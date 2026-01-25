namespace ErrorOrX.Generators.Tests;

public class ErrorMappingSyncTests
{
    [Fact]
    public void ErrorType_Matches_Generator_Expectations() =>
        Enum.GetValues<ErrorType>().Should().BeEquivalentTo(
            [ErrorType.Failure, ErrorType.Unexpected, ErrorType.Validation, ErrorType.Conflict, ErrorType.NotFound, ErrorType.Unauthorized, ErrorType.Forbidden
            ],
            static options => options.WithStrictOrdering());
}
