namespace ErrorOrX.Tests.ErrorOr;

public class ErrorOrArgumentValidationTests
{
    private static readonly ErrorOr<int> s_validValue = 5;
    private static readonly ErrorOr<int> s_errorValue = Error.Failure();

    // ---------------------------------------------------------
    // Then / ThenAsync Checks
    // ---------------------------------------------------------
    [Fact]
    public void Then_WhenOnValueIsNull_ShouldThrowArgumentNullException()
    {
        Func<int, ErrorOr<string>> onValue = null!;
        Action act = () => s_validValue.Then(onValue);
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("onValue");
    }

    [Fact]
    public void ThenDo_WhenActionIsNull_ShouldThrowArgumentNullException()
    {
        Action<int> action = null!;
        Action act = () => s_validValue.ThenDo(action);
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("action");
    }

    [Fact]
    public async Task ThenAsync_WhenOnValueIsNull_ShouldThrowArgumentNullException()
    {
        Func<int, Task<ErrorOr<string>>> onValue = null!;
        Func<Task> act = async () => await s_validValue.ThenAsync(onValue);
        await act.Should().ThrowExactlyAsync<ArgumentNullException>().WithParameterName("onValue");
    }

    [Fact]
    public async Task ThenDoAsync_WhenActionIsNull_ShouldThrowArgumentNullException()
    {
        Func<int, Task> action = null!;
        Func<Task> act = async () => await s_validValue.ThenDoAsync(action);
        await act.Should().ThrowExactlyAsync<ArgumentNullException>().WithParameterName("action");
    }

    // ---------------------------------------------------------
    // Else / ElseAsync Checks
    // ---------------------------------------------------------
    [Fact]
    public void Else_WhenOnErrorIsNull_ShouldThrowArgumentNullException()
    {
        Func<IReadOnlyList<Error>, Error> onError = null!;
        Action act = () => s_errorValue.Else(onError);
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("onError");
    }

    [Fact]
    public void ElseDo_WhenOnErrorIsNull_ShouldThrowArgumentNullException()
    {
        Action<IReadOnlyList<Error>> onError = null!;
        Action act = () => s_errorValue.ElseDo(onError);
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("onError");
    }

    [Fact]
    public async Task ElseAsync_WhenOnErrorIsNull_ShouldThrowArgumentNullException()
    {
        Func<IReadOnlyList<Error>, Task<Error>> onError = null!;
        Func<Task> act = async () => await s_errorValue.ElseAsync(onError);
        await act.Should().ThrowExactlyAsync<ArgumentNullException>().WithParameterName("onError");
    }

    [Fact]
    public async Task ElseDoAsync_WhenOnErrorIsNull_ShouldThrowArgumentNullException()
    {
        Func<IReadOnlyList<Error>, Task> onError = null!;
        Func<Task> act = async () => await s_errorValue.ElseDoAsync(onError);
        await act.Should().ThrowExactlyAsync<ArgumentNullException>().WithParameterName("onError");
    }

    // ---------------------------------------------------------
    // Match / MatchAsync Checks
    // ---------------------------------------------------------
    [Fact]
    public void Match_WhenOnValueIsNull_ShouldThrowArgumentNullException()
    {
        Action act = static () => s_validValue.Match(null!, static _ => "error");
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("onValue");
    }

    [Fact]
    public void Match_WhenOnErrorIsNull_ShouldThrowArgumentNullException()
    {
        Action act = static () => s_errorValue.Match(static _ => "value", null!);
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("onError");
    }

    [Fact]
    public async Task MatchAsync_WhenOnValueIsNull_ShouldThrowArgumentNullException()
    {
        Func<Task> act = async static () => await s_validValue.MatchAsync(null!, static _ => Task.FromResult("error"));
        await act.Should().ThrowExactlyAsync<ArgumentNullException>().WithParameterName("onValue");
    }

    [Fact]
    public async Task MatchAsync_WhenOnErrorIsNull_ShouldThrowArgumentNullException()
    {
        Func<Task> act = async static () => await s_errorValue.MatchAsync(static _ => Task.FromResult("value"), null!);
        await act.Should().ThrowExactlyAsync<ArgumentNullException>().WithParameterName("onError");
    }

    [Fact]
    public void MatchFirst_WhenOnValueIsNull_ShouldThrowArgumentNullException()
    {
        Action act = static () => s_validValue.MatchFirst(null!, static _ => "error");
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("onValue");
    }

    [Fact]
    public void MatchFirst_WhenOnFirstErrorIsNull_ShouldThrowArgumentNullException()
    {
        Action act = static () => s_errorValue.MatchFirst(static _ => "value", null!);
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("onFirstError");
    }

    // ---------------------------------------------------------
    // Switch / SwitchAsync Checks
    // ---------------------------------------------------------
    [Fact]
    public void Switch_WhenOnValueIsNull_ShouldThrowArgumentNullException()
    {
        var act = static () => s_validValue.Switch(null!, static _ => { });
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("onValue");
    }

    [Fact]
    public void Switch_WhenOnErrorIsNull_ShouldThrowArgumentNullException()
    {
        var act = static () => s_errorValue.Switch(static _ => { }, null!);
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("onError");
    }

    [Fact]
    public async Task SwitchAsync_WhenOnValueIsNull_ShouldThrowArgumentNullException()
    {
        var act = static () => s_validValue.SwitchAsync(null!, static _ => Task.CompletedTask);
        await act.Should().ThrowExactlyAsync<ArgumentNullException>().WithParameterName("onValue");
    }

    [Fact]
    public async Task SwitchAsync_WhenOnErrorIsNull_ShouldThrowArgumentNullException()
    {
        var act = static () => s_errorValue.SwitchAsync(static _ => Task.CompletedTask, null!);
        await act.Should().ThrowExactlyAsync<ArgumentNullException>().WithParameterName("onError");
    }
}
