using ErrorOr.Core.ErrorOr;
using ErrorOr.Core.Errors;

namespace ErrorOr.Core.Tests.ErrorOr;

public class SwitchTests
{
    [Fact]
    public void CallingSwitch_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person("Amichai");
        void ThenAction(Person person) => person.Should().BeEquivalentTo(errorOrPerson.Value);
        void ElsesAction(IReadOnlyList<Error> _) => throw new Exception("Should not be called");

        // Act
        var action = () => errorOrPerson.Switch(
            ThenAction,
            ElsesAction);

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void CallingSwitch_WhenIsError_ShouldExecuteElseAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new List<Error> { Error.Validation(), Error.Conflict() };
        void ThenAction(Person _) => throw new Exception("Should not be called");
        void ElsesAction(IReadOnlyList<Error> errors) => errors.Should().BeEquivalentTo(errorOrPerson.Errors);

        // Act
        var action = () => errorOrPerson.Switch(
            ThenAction,
            ElsesAction);

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void CallingSwitchFirst_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person("Amichai");
        void ThenAction(Person person) => person.Should().BeEquivalentTo(errorOrPerson.Value);
        void OnFirstErrorAction(Error _) => throw new Exception("Should not be called");

        // Act
        var action = () => errorOrPerson.SwitchFirst(
            ThenAction,
            OnFirstErrorAction);

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void CallingSwitchFirst_WhenIsError_ShouldExecuteOnFirstErrorAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new List<Error> { Error.Validation(), Error.Conflict() };
        void ThenAction(Person _) => throw new Exception("Should not be called");

        void OnFirstErrorAction(Error errors)
            => errors.Should().BeEquivalentTo(errorOrPerson.Errors[0])
                .And.BeEquivalentTo(errorOrPerson.FirstError);

        // Act
        var action = () => errorOrPerson.SwitchFirst(
            ThenAction,
            OnFirstErrorAction);

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public async Task CallingSwitchFirstAfterThenAsync_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person("Amichai");
        void ThenAction(Person person) => person.Should().BeEquivalentTo(errorOrPerson.Value);
        void OnFirstErrorAction(Error _) => throw new Exception("Should not be called");

        // Act
        var action = () => errorOrPerson
            .ThenAsync(static person => Task.FromResult(person))
            .SwitchFirst(ThenAction, OnFirstErrorAction);

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CallingSwitchAfterThenAsync_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person("Amichai");
        void ThenAction(Person person) => person.Should().BeEquivalentTo(errorOrPerson.Value);
        void ElsesAction(IReadOnlyList<Error> _) => throw new Exception("Should not be called");

        // Act
        var action = () => errorOrPerson
            .ThenAsync(static person => Task.FromResult(person))
            .Switch(ThenAction, ElsesAction);

        // Assert
        await action.Should().NotThrowAsync();
    }

    private record Person(string Name);
}
