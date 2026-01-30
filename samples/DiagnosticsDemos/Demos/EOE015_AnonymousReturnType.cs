// EOE015: Anonymous return type not supported
// =============================================
// Anonymous types cannot be used as ErrorOr value types.
// They have no stable identity for JSON serialization.

namespace DiagnosticsDemos.Demos;

public record TodoSummary(int Id, string Title);

public static class EOE015_AnonymousReturnType
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE015: Returning anonymous type wrapped in ErrorOr
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Get("/data")]
    // public static ErrorOr<object> GetData() => new { Name = "test", Value = 42 };

    // -------------------------------------------------------------------------
    // FIXED: Use named record types
    // -------------------------------------------------------------------------
    [Get("/api/eoe015/todos/{id}")]
    public static ErrorOr<TodoSummary> GetTodoSummary(int id)
        => new TodoSummary(id, $"Todo {id}");

    // -------------------------------------------------------------------------
    // FIXED: Use named class types
    // -------------------------------------------------------------------------
    public class StatusResponse
    {
        public string Status { get; set; } = "ok";
        public int Code { get; set; } = 200;
    }

    [Get("/api/eoe015/status")]
    public static ErrorOr<StatusResponse> GetStatus()
        => new StatusResponse();

    // -------------------------------------------------------------------------
    // FIXED: Generic wrapper with concrete type argument
    // -------------------------------------------------------------------------
    public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize);

    [Get("/api/eoe015/todos")]
    public static ErrorOr<PagedResult<TodoSummary>> GetTodos(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var todos = Enumerable.Range(1, pageSize)
            .Select(i => new TodoSummary((page - 1) * pageSize + i, $"Todo {i}"))
            .ToList();

        return new PagedResult<TodoSummary>(todos, 100, page, pageSize);
    }

    // -------------------------------------------------------------------------
    // TIP: Records are excellent for API response types
    // -------------------------------------------------------------------------
    public record ApiResponse<T>(T Data, string Message = "Success", bool IsSuccess = true);

    [Get("/api/eoe015/wrapped/{id}")]
    public static ErrorOr<ApiResponse<TodoSummary>> GetWrappedTodo(int id)
        => new ApiResponse<TodoSummary>(new TodoSummary(id, $"Todo {id}"));
}
