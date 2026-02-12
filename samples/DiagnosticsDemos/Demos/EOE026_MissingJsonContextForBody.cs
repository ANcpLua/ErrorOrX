namespace DiagnosticsDemos.Demos.Eoe026;

public record OrderRequest(string ProductId, int Quantity);

public record OrderResponse(string OrderId, string Status);

// -------------------------------------------------------------------------
// TRIGGERS EOE026: When no JsonSerializerContext exists for body types
// -------------------------------------------------------------------------
// This diagnostic appears when:
// 1. An endpoint has a [FromBody] parameter
// 2. No JsonSerializerContext with [JsonSerializable(typeof(ThatType))] exists
//
// Without the context below, endpoints using OrderRequest/OrderResponse
// would trigger EOE026.

// -------------------------------------------------------------------------
// FIXED: Define JsonSerializerContext with all body types
// -------------------------------------------------------------------------
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(OrderRequest))]
[JsonSerializable(typeof(OrderResponse))]
[JsonSerializable(typeof(List<OrderResponse>))]
// Don't forget ProblemDetails for error responses
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
internal partial class EOE026JsonContext : JsonSerializerContext
{
}

/// <summary>
/// EOE026: Missing JsonSerializerContext for AOT — No JsonSerializerContext found but endpoint uses request body,
/// so AOT serialization will fail.
/// </summary>
/// <remarks>
/// For Native AOT support, you must define a JsonSerializerContext
/// with [JsonSerializable] attributes for all request/response types.
/// </remarks>
public static class EOE026_MissingJsonContextForBody
{
    // -------------------------------------------------------------------------
    // These endpoints need OrderRequest/OrderResponse in JsonSerializerContext
    // -------------------------------------------------------------------------
    [Post("/api/eoe026/orders")]
    public static ErrorOr<OrderResponse> CreateOrder([FromBody] OrderRequest request)
    {
        if (request.Quantity <= 0)
        {
            return Error.Validation("Order.InvalidQuantity", "Quantity must be positive");
        }

        return new OrderResponse(Guid.NewGuid().ToString(), "Created");
    }

    [Get("/api/eoe026/orders/{id}")]
    public static ErrorOr<OrderResponse> GetOrder(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return Error.Validation("Order.InvalidId", "Order ID is required");
        }

        return new OrderResponse(id, "Completed");
    }

    // -------------------------------------------------------------------------
    // TIP: Minimal setup for AOT-compatible endpoints
    // -------------------------------------------------------------------------
    // 1. Create a JsonSerializerContext:
    //
    //    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    //    [JsonSerializable(typeof(MyRequest))]
    //    [JsonSerializable(typeof(MyResponse))]
    //    [JsonSerializable(typeof(ProblemDetails))]
    //    [JsonSerializable(typeof(HttpValidationProblemDetails))]
    //    internal partial class AppJsonContext : JsonSerializerContext { }
    //
    // 2. Register in Program.cs:
    //
    //    builder.Services.AddErrorOrEndpoints(options => options
    //        .UseJsonContext<AppJsonContext>()
    //        .WithCamelCase()
    //        .WithIgnoreNulls());
}

// -------------------------------------------------------------------------
// Complete JsonSerializerContext checklist:
// -------------------------------------------------------------------------
// [ ] Add [JsonSerializable] for each request body type
// [ ] Add [JsonSerializable] for each response type
// [ ] Add [JsonSerializable] for List<T> if returning collections
// [ ] Add [JsonSerializable] for ProblemDetails (error responses)
// [ ] Add [JsonSerializable] for HttpValidationProblemDetails (validation errors)
// [ ] Add PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase
// [ ] Optionally add DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
