// EOE041: JsonSerializerContext missing error types
// ==================================================
// When using a custom JsonSerializerContext with ErrorOrX,
// you must include ProblemDetails and HttpValidationProblemDetails
// for error response serialization.
//
// ErrorOrX converts ErrorOr errors to ProblemDetails responses.
// Without these types in your context, error serialization will fail.
//
// See: https://learn.microsoft.com/dotnet/core/deploying/trimming/fixing-warnings

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DiagnosticsDemos.Demos;

// -------------------------------------------------------------------------
// TRIGGERS EOE041: JsonSerializerContext without error types
// -------------------------------------------------------------------------
// Uncomment to see the diagnostic:
//
// [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
// [JsonSerializable(typeof(Eoe041Response))]
// internal partial class IncompleteJsonContext : JsonSerializerContext { }

// -------------------------------------------------------------------------
// FIXED: JsonSerializerContext with error types
// -------------------------------------------------------------------------
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Eoe041Response))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
internal partial class CompleteJsonContext : JsonSerializerContext;

public static class EOE041_JsonContextMissingProblemDetails
{
    [Get("/api/eoe041/item/{id}")]
    public static ErrorOr<Eoe041Response> GetItem(int id)
    {
        if (id <= 0)
            return Error.Validation("Id.Invalid", "Id must be positive");

        if (id == 404)
            return Error.NotFound("Item.NotFound", $"Item {id} not found");

        return new Eoe041Response(id, $"Item {id}");
    }
}

public record Eoe041Response(int Id, string Name);

// -------------------------------------------------------------------------
// TIP: Complete JsonSerializerContext for ErrorOrX
// -------------------------------------------------------------------------
//
// Your JsonSerializerContext should include:
//
// 1. All your request/response types
// 2. ProblemDetails - for standard error responses (404, 500, etc.)
// 3. HttpValidationProblemDetails - for validation error responses (400)
//
// Example:
//
// [JsonSourceGenerationOptions(
//     PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
//     DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
// [JsonSerializable(typeof(MyRequest))]
// [JsonSerializable(typeof(MyResponse))]
// [JsonSerializable(typeof(ProblemDetails))]
// [JsonSerializable(typeof(HttpValidationProblemDetails))]
// internal partial class AppJsonContext : JsonSerializerContext { }
//
// -------------------------------------------------------------------------
// Why ProblemDetails matters
// -------------------------------------------------------------------------
//
// When an ErrorOr<T> handler returns an error:
//
// [Get("/item/{id}")]
// public static ErrorOr<Item> GetItem(int id)
//     => Error.NotFound("Item.NotFound", "Item not found");
//
// ErrorOrX converts this to:
//
// HTTP 404 Not Found
// {
//   "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
//   "title": "Not Found",
//   "status": 404,
//   "detail": "Item not found"
// }
//
// This JSON response REQUIRES ProblemDetails in your JsonSerializerContext!
