// EOE002: Handler must be static
// ================================
// Handler methods must be static for source generation.
//
// The ErrorOrX generator creates static method references at compile time.
// Instance methods cannot be used because the generator has no way to obtain
// or manage instance lifecycles.

namespace DiagnosticsDemos.Demos;

public class EOE002_NonStaticHandler
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE002: Instance method (non-static)
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Get("/instance")]
    // public ErrorOr<string> GetInstance() => "instance method";

    // -------------------------------------------------------------------------
    // TRIGGERS EOE002: Instance async method
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Get("/instance-async")]
    // public Task<ErrorOr<string>> GetInstanceAsync() => Task.FromResult<ErrorOr<string>>("async");

    // -------------------------------------------------------------------------
    // FIXED: Make the method static
    // -------------------------------------------------------------------------
    [Get("/api/eoe002/valid")]
    public static ErrorOr<string> GetValid()
    {
        return "static method";
    }

    // -------------------------------------------------------------------------
    // NOTE: If you need instance state, inject it as a service parameter
    // -------------------------------------------------------------------------
    [Get("/api/eoe002/with-service")]
    public static ErrorOr<string> GetWithService([FromServices] ILogger<EOE002_NonStaticHandler> logger)
    {
        logger.LogInformation("Using injected service instead of instance state");
        return "using dependency injection";
    }
}
