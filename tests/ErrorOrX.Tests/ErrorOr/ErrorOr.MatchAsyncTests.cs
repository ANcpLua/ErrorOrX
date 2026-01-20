namespace ErrorOrX.Tests.ErrorOr;

public class MatchAsyncTests
{
    [Fact]
    public async Task CallingMatchAsync_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person("Amichai");

        // Act
        var action = () => errorOrPerson.MatchAsync(
            ThenAction,
            ElsesAction);

        // Assert
        (await action.Should().NotThrowAsync()).Subject.Should().Be("Nice");
        return;

        Task<string> ThenAction(Person person)
        {
            person.Should().BeEquivalentTo(errorOrPerson.Value);
            return Task.FromResult("Nice");
        }

        static Task<string> ElsesAction(IReadOnlyList<Error> _) => Unreachable.Throw<Task<string>>();
    }

    [Fact]
    public async Task CallingMatchAsync_WhenIsError_ShouldExecuteElseAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = (Error[])[Error.Validation(), Error.Conflict()];

        // Act
        var action = () => errorOrPerson.MatchAsync(
            ThenAction,
            ElsesAction);

        // Assert
        (await action.Should().NotThrowAsync()).Subject.Should().Be("Nice");
        return;

        static Task<string> ThenAction(Person _) => Unreachable.Throw<Task<string>>();

        Task<string> ElsesAction(IReadOnlyList<Error> errors)
        {
            errors.Should().BeEquivalentTo(errorOrPerson.Errors);
            return Task.FromResult("Nice");
        }
    }

    [Fact]
    public async Task CallingMatchFirstAsync_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person("Amichai");

        // Act
        var action = () => errorOrPerson.MatchFirstAsync(
            ThenAction,
            OnFirstErrorAction);

        // Assert
        (await action.Should().NotThrowAsync()).Subject.Should().Be("Nice");
        return;

        Task<string> ThenAction(Person person)
        {
            person.Should().BeEquivalentTo(errorOrPerson.Value);
            return Task.FromResult("Nice");
        }

        static Task<string> OnFirstErrorAction(Error _) => Unreachable.Throw<Task<string>>();
    }

    [Fact]
    public async Task CallingMatchFirstAsync_WhenIsError_ShouldExecuteOnFirstErrorAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = (Error[])[Error.Validation(), Error.Conflict()];

        // Act
        var action = () => errorOrPerson.MatchFirstAsync(
            ThenAction,
            OnFirstErrorAction);

        // Assert
        (await action.Should().NotThrowAsync()).Subject.Should().Be("Nice");
        return;

        static Task<string> ThenAction(Person _) => Unreachable.Throw<Task<string>>();

        Task<string> OnFirstErrorAction(Error errors)
        {
            errors.Should().BeEquivalentTo(errorOrPerson.Errors[0])
                .And.BeEquivalentTo(errorOrPerson.FirstError);

            return Task.FromResult("Nice");
        }
    }

    [Fact]
    public async Task CallingMatchFirstAsyncAfterThenAsync_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = (Error[])[Error.Validation(), Error.Conflict()];

        // Act
        var action = () => errorOrPerson
            .ThenAsync(static person => Task.FromResult(person))
            .MatchFirstAsync(ThenAction, OnFirstErrorAction);

        // Assert
        (await action.Should().NotThrowAsync()).Subject.Should().Be("Nice");
        return;

        static Task<string> ThenAction(Person _) => Unreachable.Throw<Task<string>>();

        Task<string> OnFirstErrorAction(Error errors)
        {
            errors.Should().BeEquivalentTo(errorOrPerson.Errors[0])
                .And.BeEquivalentTo(errorOrPerson.FirstError);

            return Task.FromResult("Nice");
        }
    }

    [Fact]
    public async Task CallingMatchAsyncAfterThenAsync_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = (Error[])[Error.Validation(), Error.Conflict()];

        // Act
        var action = () => errorOrPerson
            .ThenAsync(static person => Task.FromResult(person))
            .MatchAsync(ThenAction, ElsesAction);

        // Assert
        (await action.Should().NotThrowAsync()).Subject.Should().Be("Nice");
        return;

        static Task<string> ThenAction(Person _) => Unreachable.Throw<Task<string>>();

        Task<string> ElsesAction(IReadOnlyList<Error> errors)
        {
            errors.Should().BeEquivalentTo(errorOrPerson.Errors);
            return Task.FromResult("Nice");
        }
    }

    private sealed record Person(string Name);
}
