namespace ErrorOrX.Tests.ErrorOr;

public class MatchTests
{
    [Fact]
    public void CallingMatch_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person("Amichai");

        // Act
        var action = () => errorOrPerson.Match(
            ThenAction,
            ElsesAction);

        // Assert
        action.Should().NotThrow().Subject.Should().Be("Nice");
        return;

        string ThenAction(Person person)
        {
            person.Should().BeEquivalentTo(errorOrPerson.Value);
            return "Nice";
        }

        static string ElsesAction(IReadOnlyList<Error> _) => Unreachable.Throw<string>();
    }

    [Fact]
    public void CallingMatch_WhenIsError_ShouldExecuteElseAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = (Error[])[Error.Validation(), Error.Conflict()];

        // Act
        var action = () => errorOrPerson.Match(
            ThenAction,
            ElsesAction);

        // Assert
        action.Should().NotThrow().Subject.Should().Be("Nice");
        return;

        static string ThenAction(Person _) => Unreachable.Throw<string>();

        string ElsesAction(IReadOnlyList<Error> errors)
        {
            errors.Should().BeEquivalentTo(errorOrPerson.Errors);
            return "Nice";
        }
    }

    [Fact]
    public void CallingMatchFirst_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person("Amichai");

        // Act
        var action = () => errorOrPerson.MatchFirst(
            ThenAction,
            OnFirstErrorAction);

        // Assert
        action.Should().NotThrow().Subject.Should().Be("Nice");
        return;

        string ThenAction(Person person)
        {
            person.Should().BeEquivalentTo(errorOrPerson.Value);
            return "Nice";
        }

        static string OnFirstErrorAction(Error _) => Unreachable.Throw<string>();
    }

    [Fact]
    public void CallingMatchFirst_WhenIsError_ShouldExecuteOnFirstErrorAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = (Error[])[Error.Validation(), Error.Conflict()];

        // Act
        var action = () => errorOrPerson.MatchFirst(
            ThenAction,
            OnFirstErrorAction);

        // Assert
        action.Should().NotThrow().Subject.Should().Be("Nice");
        return;

        static string ThenAction(Person _) => Unreachable.Throw<string>();

        string OnFirstErrorAction(Error errors)
        {
            errors.Should().BeEquivalentTo(errorOrPerson.Errors[0])
                .And.BeEquivalentTo(errorOrPerson.FirstError);

            return "Nice";
        }
    }

    [Fact]
    public async Task CallingMatchFirstAfterThenAsync_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person("Amichai");

        // Act
        var action = () => errorOrPerson
            .ThenAsync(static person => Task.FromResult(person))
            .MatchFirst(ThenAction, OnFirstErrorAction);

        // Assert
        (await action.Should().NotThrowAsync()).Subject.Should().Be("Nice");
        return;

        string ThenAction(Person person)
        {
            person.Should().BeEquivalentTo(errorOrPerson.Value);
            return "Nice";
        }

        static string OnFirstErrorAction(Error _) => Unreachable.Throw<string>();
    }

    [Fact]
    public async Task CallingMatchAfterThenAsync_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person("Amichai");

        // Act
        var action = () => errorOrPerson
            .ThenAsync(static person => Task.FromResult(person))
            .Match(ThenAction, ElsesAction);

        // Assert
        (await action.Should().NotThrowAsync()).Subject.Should().Be("Nice");
        return;

        string ThenAction(Person person)
        {
            person.Should().BeEquivalentTo(errorOrPerson.Value);
            return "Nice";
        }

        static string ElsesAction(IReadOnlyList<Error> _) => Unreachable.Throw<string>();
    }

    private sealed record Person(string Name);
}
