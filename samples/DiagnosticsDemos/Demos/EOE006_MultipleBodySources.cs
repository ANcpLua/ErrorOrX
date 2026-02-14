namespace DiagnosticsDemos.Demos;

public sealed record CreateRequest(string Name);

/// <summary>
///     EOE006: Multiple body sources — Endpoint has multiple body sources (FromBody, FromForm, Stream, PipeReader);
///     only one is allowed per endpoint.
/// </summary>
/// <remarks>
///     HTTP requests can only have one body, so an endpoint cannot read it multiple ways.
/// </remarks>
public static class EOE006_MultipleBodySources
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE006: Both [FromBody] and [FromForm] on same endpoint
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Post("/upload")]
    // public static ErrorOr<string> UploadWithBody(
    //     [FromBody] CreateRequest body,
    //     [FromForm] IFormFile file) => "uploaded";

    // -------------------------------------------------------------------------
    // TRIGGERS EOE006: Both [FromBody] and Stream
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Post("/data")]
    // public static ErrorOr<string> ProcessData(
    //     [FromBody] CreateRequest body,
    //     Stream data) => "processed";

    // -------------------------------------------------------------------------
    // TRIGGERS EOE006: Multiple [FromBody] parameters
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Post("/multi")]
    // public static ErrorOr<string> MultiBody(
    //     [FromBody] CreateRequest first,
    //     [FromBody] CreateRequest second) => "multi";

    // -------------------------------------------------------------------------
    // FIXED: Use only one body source
    // -------------------------------------------------------------------------
    [Post("/api/eoe006/json")]
    public static ErrorOr<string> CreateFromJson([FromBody] CreateRequest request)
    {
        return $"Created: {request.Name}";
    }

    [Post("/api/eoe006/form")]
    public static ErrorOr<string> CreateFromForm([FromForm] IFormFile file)
    {
        return $"Uploaded: {file.FileName}";
    }

    [Post("/api/eoe006/stream")]
    public static async Task<ErrorOr<string>> ProcessStream(Stream body)
    {
        using var reader = new StreamReader(body);
        var content = await reader.ReadToEndAsync();
        return $"Processed {content.Length} bytes";
    }

    // -------------------------------------------------------------------------
    // FIXED: Combine body with other sources (route, query, header, services)
    // -------------------------------------------------------------------------
    [Post("/api/eoe006/combined/{id}")]
    public static ErrorOr<string> CombinedSources(
        int id,
        [FromBody] CreateRequest body,
        [FromQuery] bool validate,
        [FromHeader(Name = "X-Correlation-Id")]
        string? correlationId)
    {
        return $"Created {body.Name} with id {id}, validated: {validate}, correlation: {correlationId}";
    }
}
