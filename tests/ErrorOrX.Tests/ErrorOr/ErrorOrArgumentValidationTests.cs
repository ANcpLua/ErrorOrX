namespace ErrorOrX.Tests.ErrorOr;

public class ErrorOrArgumentValidationTests
{
    private static readonly ErrorOr<int> ValidValue = 5;
    private static readonly ErrorOr<int> ErrorValue = Error.Failure();

    // ---------------------------------------------------------
    // Then / ThenAsync Checks
    // ---------------------------------------------------------
    [Fact]
    public void Then_WhenOnValueIsNull_ShouldThrowArgumentNullException()
    {
        Func<int, ErrorOr<string>> onValue = null!;
        Action act = () => ValidValue.Then(onValue);
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("onValue");
    }

    [Fact]
    public void ThenDo_WhenActionIsNull_ShouldThrowArgumentNullException()
    {
        Action<int> action = null!;
        Action act = () => ValidValue.ThenDo(action);
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("action");
    }

    [Fact]
    public async Task ThenAsync_WhenOnValueIsNull_ShouldThrowArgumentNullException()
    {
        Func<int, Task<ErrorOr<string>>> onValue = null!;
        Func<Task> act = async () => await ValidValue.ThenAsync(onValue);
        await act.Should().ThrowExactlyAsync<ArgumentNullException>().WithParameterName("onValue");
    }

    [Fact]
    public async Task ThenDoAsync_WhenActionIsNull_ShouldThrowArgumentNullException()
    {
        Func<int, Task> action = null!;
        Func<Task> act = async () => await ValidValue.ThenDoAsync(action);
        await act.Should().ThrowExactlyAsync<ArgumentNullException>().WithParameterName("action");
    }

    // ---------------------------------------------------------
    // Else / ElseAsync Checks
    // ---------------------------------------------------------
    [Fact]
    public void Else_WhenOnErrorIsNull_ShouldThrowArgumentNullException()
    {
        Func<IReadOnlyList<Error>, Error> onError = null!;
        Action act = () => ErrorValue.Else(onError);
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("onError");
    }

    [Fact]
    public void ElseDo_WhenOnErrorIsNull_ShouldThrowArgumentNullException()
    {
        Action<IReadOnlyList<Error>> onError = null!;
        Action act = () => ErrorValue.ElseDo(onError);
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("onError");
    }

    [Fact]
    public async Task ElseAsync_WhenOnErrorIsNull_ShouldThrowArgumentNullException()
    {
        Func<IReadOnlyList<Error>, Task<Error>> onError = null!;
        Func<Task> act = async () => await ErrorValue.ElseAsync(onError);
        await act.Should().ThrowExactlyAsync<ArgumentNullException>().WithParameterName("onError");
    }

    [Fact]
    public async Task ElseDoAsync_WhenOnErrorIsNull_ShouldThrowArgumentNullException()
    {
        Func<IReadOnlyList<Error>, Task> onError = null!;
        Func<Task> act = async () => await ErrorValue.ElseDoAsync(onError);
        await act.Should().ThrowExactlyAsync<ArgumentNullException>().WithParameterName("onError");
    }

    // ---------------------------------------------------------
    // Match / MatchAsync Checks
    // ---------------------------------------------------------
    [Fact]
    public void Match_WhenOnValueIsNull_ShouldThrowArgumentNullException()
    {
        Action act = static () => ValidValue.Match(null!, static _ => "error");
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("onValue");
    }

    [Fact]
    public void Match_WhenOnErrorIsNull_ShouldThrowArgumentNullException()
    {
        Action act = static () => ErrorValue.Match(static _ => "value", null!);
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("onError");
    }

    [Fact]
    public async Task MatchAsync_WhenOnValueIsNull_ShouldThrowArgumentNullException()
    {
        Func<Task> act = async static () => await ValidValue.MatchAsync(null!, static _ => Task.FromResult("error"));
        await act.Should().ThrowExactlyAsync<ArgumentNullException>().WithParameterName("onValue");
    }

    [Fact]
    public async Task MatchAsync_WhenOnErrorIsNull_ShouldThrowArgumentNullException()
    {
        Func<Task> act = async static () => await ErrorValue.MatchAsync(static _ => Task.FromResult("value"), null!);
        await act.Should().ThrowExactlyAsync<ArgumentNullException>().WithParameterName("onError");
    }

    [Fact]
    public void MatchFirst_WhenOnValueIsNull_ShouldThrowArgumentNullException()
    {
        Action act = static () => ValidValue.MatchFirst(null!, static _ => "error");
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("onValue");
    }

    [Fact]
    public void MatchFirst_WhenOnFirstErrorIsNull_ShouldThrowArgumentNullException()
    {
        Action act = static () => ErrorValue.MatchFirst(static _ => "value", null!);
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("onFirstError");
    }

    // ---------------------------------------------------------
    // Switch / SwitchAsync Checks
    // ---------------------------------------------------------
    [Fact]
    public void Switch_WhenOnValueIsNull_ShouldThrowArgumentNullException()
    {
        var act = static () => ValidValue.Switch(null!, static _ => { });
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("onValue");
    }

    [Fact]
    public void Switch_WhenOnErrorIsNull_ShouldThrowArgumentNullException()
    {
        var act = static () => ErrorValue.Switch(static _ => { }, null!);
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("onError");
    }

    [Fact]
    public async Task SwitchAsync_WhenOnValueIsNull_ShouldThrowArgumentNullException()
    {
        var act = static () => ValidValue.SwitchAsync(null!, static _ => Task.CompletedTask);
        await act.Should().ThrowExactlyAsync<ArgumentNullException>().WithParameterName("onValue");
    }

    [Fact]
    public async Task SwitchAsync_WhenOnErrorIsNull_ShouldThrowArgumentNullException()
    {
        var act = static () => ErrorValue.SwitchAsync(static _ => Task.CompletedTask, null!);
        await act.Should().ThrowExactlyAsync<ArgumentNullException>().WithParameterName("onError");
    }
}
