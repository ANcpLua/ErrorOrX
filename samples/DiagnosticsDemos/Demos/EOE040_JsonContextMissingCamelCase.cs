// EOE040: JsonSerializerContext missing CamelCase
// ================================================
// When using a custom JsonSerializerContext with ErrorOrX,
// you should configure PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase
// for ASP.NET Core compatibility.
//
// ASP.NET Core defaults to camelCase for JSON property names.
// Mismatched casing can cause serialization/deserialization issues.
//
// See: https://learn.microsoft.com/dotnet/core/deploying/trimming/fixing-warnings

namespace DiagnosticsDemos.Demos;

// -------------------------------------------------------------------------
// TRIGGERS EOE040: JsonSerializerContext without CamelCase
// -------------------------------------------------------------------------
// Uncomment to see the diagnostic:
//
// [JsonSerializable(typeof(Eoe040Response))]
// internal partial class BadJsonContext : JsonSerializerContext { }

// -------------------------------------------------------------------------
// FIXED: JsonSerializerContext with CamelCase policy
// -------------------------------------------------------------------------
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Eoe040Response))]
internal partial class GoodJsonContext : JsonSerializerContext;

public static class EOE040_JsonContextMissingCamelCase
{
    [Get("/api/eoe040/response")]
    public static ErrorOr<Eoe040Response> GetResponse()
    {
        return new Eoe040Response("Hello", 42);
    }
}

public record Eoe040Response(string Message, int Value);

// -------------------------------------------------------------------------
// TIP: Recommended JsonSerializerContext configuration
// -------------------------------------------------------------------------
//
// [JsonSourceGenerationOptions(
//     PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
//     DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
// [JsonSerializable(typeof(YourType))]
// [JsonSerializable(typeof(ProblemDetails))]           // For error responses
// [JsonSerializable(typeof(HttpValidationProblemDetails))]  // For validation errors
// internal partial class AppJsonContext : JsonSerializerContext { }
//
// Then register with ErrorOrX:
// builder.Services.AddErrorOrEndpoints(options => options
//     .UseJsonContext<AppJsonContext>());
