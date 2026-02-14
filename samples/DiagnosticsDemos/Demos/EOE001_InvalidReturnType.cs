namespace DiagnosticsDemos.Demos;

/// <summary>
///     EOE001: Invalid return type — Handler method must return ErrorOr&lt;T&gt;, Task&lt;ErrorOr&lt;T&gt;&gt;,
///     or ValueTask&lt;ErrorOr&lt;T&gt;&gt;.
/// </summary>
/// <remarks>
///     The ErrorOrX generator requires endpoints to return one of these types so it can
///     properly generate the error handling and result mapping code.
/// </remarks>
public static class EOE001_InvalidReturnType
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE001: Method returns string instead of ErrorOr<string>
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Get("/invalid")]
    // public static string GetInvalid() => "not wrapped in ErrorOr";

    // -------------------------------------------------------------------------
    // TRIGGERS EOE001: Method returns Task<string> instead of Task<ErrorOr<string>>
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Get("/invalid-async")]
    // public static Task<string> GetInvalidAsync() => Task.FromResult("not wrapped");

    // -------------------------------------------------------------------------
    // FIXED: Return ErrorOr<T> for synchronous methods
    // -------------------------------------------------------------------------
    [Get("/api/eoe001/sync")]
    public static ErrorOr<string> GetValidSync()
    {
        return "valid response";
    }

    // -------------------------------------------------------------------------
    // FIXED: Return Task<ErrorOr<T>> for asynchronous methods
    // -------------------------------------------------------------------------
    [Get("/api/eoe001/async")]
    public static Task<ErrorOr<string>> GetValidAsync()
    {
        return Task.FromResult<ErrorOr<string>>("valid async response");
    }

    // -------------------------------------------------------------------------
    // FIXED: Return ValueTask<ErrorOr<T>> for high-performance async paths
    // -------------------------------------------------------------------------
    [Get("/api/eoe001/valuetask")]
    public static ValueTask<ErrorOr<string>> GetValidValueTask()
    {
        return ValueTask.FromResult<ErrorOr<string>>("valid valuetask response");
    }
}
