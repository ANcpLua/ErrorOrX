namespace ErrorOrX.Tests.ErrorOr;

public class SwitchAsyncTests
{
    [Fact]
    public async Task CallingSwitchAsync_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person();

        // Act
        var action = () => errorOrPerson.SwitchAsync(
            ThenAction,
            ElsesAction);

        // Assert
        await action.Should().NotThrowAsync();
        return;

        Task ThenAction(Person person) => Task.FromResult(person.Should().BeEquivalentTo(errorOrPerson.Value));

        static Task ElsesAction(IReadOnlyList<Error> _) => Throw.UnreachableException<Task>();
    }

    [Fact]
    public async Task CallingSwitchAsync_WhenIsError_ShouldExecuteElseAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = (Error[])[Error.Validation(), Error.Conflict()];

        // Act
        var action = () => errorOrPerson.SwitchAsync(
            ThenAction,
            ElsesAction);

        // Assert
        await action.Should().NotThrowAsync();
        return;

        static Task ThenAction(Person _) => Throw.UnreachableException<Task>();

        Task ElsesAction(IReadOnlyList<Error> errors) =>
            Task.FromResult(errors.Should().BeEquivalentTo(errorOrPerson.Errors));
    }

    [Fact]
    public async Task CallingSwitchFirstAsync_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person();

        // Act
        var action = () => errorOrPerson.SwitchFirstAsync(
            ThenAction,
            OnFirstErrorAction);

        // Assert
        await action.Should().NotThrowAsync();
        return;

        Task ThenAction(Person person) => Task.FromResult(person.Should().BeEquivalentTo(errorOrPerson.Value));

        static Task OnFirstErrorAction(Error _) => Throw.UnreachableException<Task>();
    }

    [Fact]
    public async Task CallingSwitchFirstAsync_WhenIsError_ShouldExecuteOnFirstErrorAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = (Error[])[Error.Validation(), Error.Conflict()];

        // Act
        var action = () => errorOrPerson.SwitchFirstAsync(
            ThenAction,
            OnFirstErrorAction);

        // Assert
        await action.Should().NotThrowAsync();
        return;

        static Task ThenAction(Person _) => Throw.UnreachableException<Task>();

        Task OnFirstErrorAction(Error errors)
            => Task.FromResult(errors.Should().BeEquivalentTo(errorOrPerson.Errors[0])
                .And.BeEquivalentTo(errorOrPerson.FirstError));
    }

    [Fact]
    public async Task CallingSwitchFirstAsyncAfterThenAsync_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person();

        // Act
        var action = () => errorOrPerson
            .ThenAsync(static person => Task.FromResult(person))
            .SwitchFirstAsync(
                ThenAction,
                OnFirstErrorAction);

        // Assert
        await action.Should().NotThrowAsync();
        return;

        Task ThenAction(Person person) => Task.FromResult(person.Should().BeEquivalentTo(errorOrPerson.Value));

        static Task OnFirstErrorAction(Error _) => Throw.UnreachableException<Task>();
    }

    [Fact]
    public async Task CallingSwitchAsyncAfterThenAsync_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person();

        // Act
        var action = () => errorOrPerson
            .ThenAsync(static person => Task.FromResult(person))
            .SwitchAsync(ThenAction, ElsesAction);

        // Assert
        await action.Should().NotThrowAsync();
        return;

        Task ThenAction(Person person) => Task.FromResult(person.Should().BeEquivalentTo(errorOrPerson.Value));

        static Task ElsesAction(IReadOnlyList<Error> _) => Throw.UnreachableException<Task>();
    }

    private sealed record Person;
}
