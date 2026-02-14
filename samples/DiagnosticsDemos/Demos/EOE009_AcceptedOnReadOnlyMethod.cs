namespace DiagnosticsDemos.Demos;

/// <summary>
///     EOE009: [AcceptedResponse] on read-only method — [AcceptedResponse] on GET/DELETE is semantically unusual.
/// </summary>
/// <remarks>
///     HTTP 202 Accepted indicates that a request has been accepted for processing
///     but the processing has not been completed. This is typically used for
///     async POST/PUT operations, not read operations.
/// </remarks>
public static class EOE009_AcceptedOnReadOnlyMethod
{
    [Post("/api/eoe009/import")]
    [AcceptedResponse]
    public static ErrorOr<string> StartImport([FromBody] ImportRequest request)
    {
        // Start async import job and return immediately
        // The job will be processed in the background
        var jobId = Guid.NewGuid().ToString();
        return jobId;
    }

    [Put("/api/eoe009/batch-update")]
    [AcceptedResponse]
    public static ErrorOr<string> StartBatchUpdate([FromBody] BatchUpdateRequest request)
    {
        // Start batch update job
        var jobId = Guid.NewGuid().ToString();
        return jobId;
    }

    // -------------------------------------------------------------------------
    // FIXED: GET should return immediate results, not 202 Accepted
    // -------------------------------------------------------------------------
    [Get("/api/eoe009/items/{id}")]
    public static ErrorOr<string> GetItem(int id)
    {
        return $"Item {id}";
    }

    // -------------------------------------------------------------------------
    // FIXED: For DELETE, consider returning 204 No Content (default) or the deleted item
    // -------------------------------------------------------------------------
    [Delete("/api/eoe009/items/{id}")]
    public static ErrorOr<Deleted> DeleteItem(int id)
    {
        return Result.Deleted;
    }
    // -------------------------------------------------------------------------
    // TRIGGERS EOE009: [AcceptedResponse] with GET
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Get("/status")]
    // [AcceptedResponse]
    // public static ErrorOr<string> GetStatus() => "status";

    // -------------------------------------------------------------------------
    // TRIGGERS EOE009: [AcceptedResponse] with DELETE
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Delete("/items/{id}")]
    // [AcceptedResponse]
    // public static ErrorOr<Deleted> DeleteItem(int id) => Result.Deleted;

    // -------------------------------------------------------------------------
    // FIXED: Use [AcceptedResponse] with POST for async operations
    // -------------------------------------------------------------------------
    public sealed record ImportRequest(string Url);

    // -------------------------------------------------------------------------
    // FIXED: Use [AcceptedResponse] with PUT for long-running updates
    // -------------------------------------------------------------------------
    public sealed record BatchUpdateRequest(List<int> Ids, string NewStatus);
}
