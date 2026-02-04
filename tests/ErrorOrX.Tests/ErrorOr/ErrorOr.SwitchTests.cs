namespace ErrorOrX.Tests.ErrorOr;

public class SwitchTests
{
    [Fact]
    public void CallingSwitch_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person("Amichai");

        // Act
        var action = () => errorOrPerson.Switch(
            ThenAction,
            ElsesAction);

        // Assert
        action.Should().NotThrow();
        return;

        void ThenAction(Person person) => person.Should().BeEquivalentTo(errorOrPerson.Value);

        static void ElsesAction(IReadOnlyList<Error> _) => Throw.UnreachableException();
    }

    [Fact]
    public void CallingSwitch_WhenIsError_ShouldExecuteElseAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = (Error[])[Error.Validation(), Error.Conflict()];

        // Act
        var action = () => errorOrPerson.Switch(
            ThenAction,
            ElsesAction);

        // Assert
        action.Should().NotThrow();
        return;

        static void ThenAction(Person _) => Throw.UnreachableException();

        void ElsesAction(IReadOnlyList<Error> errors) => errors.Should().BeEquivalentTo(errorOrPerson.Errors);
    }

    [Fact]
    public void CallingSwitchFirst_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person("Amichai");

        // Act
        var action = () => errorOrPerson.SwitchFirst(
            ThenAction,
            OnFirstErrorAction);

        // Assert
        action.Should().NotThrow();
        return;

        void ThenAction(Person person) => person.Should().BeEquivalentTo(errorOrPerson.Value);

        static void OnFirstErrorAction(Error _) => Throw.UnreachableException();
    }

    [Fact]
    public void CallingSwitchFirst_WhenIsError_ShouldExecuteOnFirstErrorAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = (Error[])[Error.Validation(), Error.Conflict()];

        // Act
        var action = () => errorOrPerson.SwitchFirst(
            ThenAction,
            OnFirstErrorAction);

        // Assert
        action.Should().NotThrow();
        return;

        static void ThenAction(Person _) => Throw.UnreachableException();

        void OnFirstErrorAction(Error errors)
            => errors.Should().BeEquivalentTo(errorOrPerson.Errors[0])
                .And.BeEquivalentTo(errorOrPerson.FirstError);
    }

    [Fact]
    public async Task CallingSwitchFirstAfterThenAsync_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person("Amichai");

        // Act
        var action = () => errorOrPerson
            .ThenAsync(static person => Task.FromResult(person))
            .SwitchFirst(ThenAction, OnFirstErrorAction);

        // Assert
        await action.Should().NotThrowAsync();
        return;

        void ThenAction(Person person) => person.Should().BeEquivalentTo(errorOrPerson.Value);

        static void OnFirstErrorAction(Error _) => Throw.UnreachableException();
    }

    [Fact]
    public async Task CallingSwitchAfterThenAsync_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person("Amichai");

        // Act
        var action = () => errorOrPerson
            .ThenAsync(static person => Task.FromResult(person))
            .Switch(ThenAction, ElsesAction);

        // Assert
        await action.Should().NotThrowAsync();
        return;

        void ThenAction(Person person) => person.Should().BeEquivalentTo(errorOrPerson.Value);

        static void ElsesAction(IReadOnlyList<Error> _) => Throw.UnreachableException();
    }

    private sealed record Person(string Name);
}
