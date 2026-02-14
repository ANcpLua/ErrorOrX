namespace DiagnosticsDemos.Demos;

// Types for this demo
public sealed record ProductResponse(int Id, string Name, decimal Price);

public sealed record CategoryResponse(int Id, string Name);

// -------------------------------------------------------------------------
// JSON Context WITHOUT ProductResponse - TRIGGERS EOE007
// -------------------------------------------------------------------------
// When ProductResponse is used in an endpoint but not in the context,
// EOE007 is reported.
//
// Uncomment and ensure ProductResponse is NOT in the context:
//
// [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
// [JsonSerializable(typeof(CategoryResponse))]  // Only Category, missing Product!
// internal partial class IncompleteJsonContext : JsonSerializerContext { }

// -------------------------------------------------------------------------
// FIXED: JSON Context with all required types
// -------------------------------------------------------------------------
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ProductResponse))]
[JsonSerializable(typeof(CategoryResponse))]
[JsonSerializable(typeof(List<ProductResponse>))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
internal partial class EOE007JsonContext : JsonSerializerContext
{
}

/// <summary>
///     EOE007: Type not AOT-serializable — Type used in endpoint is not registered in JsonSerializerContext for AOT.
/// </summary>
/// <remarks>
///     For Native AOT support, all types that need JSON serialization must be
///     registered in a [JsonSerializable] context. This diagnostic is reported
///     by the generator (not analyzer) because it requires cross-file analysis.
/// </remarks>
public static class EOE007_TypeNotInJsonContext
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE007 if ProductResponse is not in JsonSerializerContext
    // -------------------------------------------------------------------------
    // With a complete context, this works fine:

    [Get("/api/eoe007/products/{id}")]
    public static ErrorOr<ProductResponse> GetProduct(int id)
    {
        return new ProductResponse(id, $"Product {id}", 99.99m);
    }

    [Get("/api/eoe007/products")]
    public static ErrorOr<List<ProductResponse>> GetAllProducts()
    {
        return new List<ProductResponse>
        {
            new(1, "Widget", 9.99m),
            new(2, "Gadget", 19.99m)
        };
    }

    [Get("/api/eoe007/categories/{id}")]
    public static ErrorOr<CategoryResponse> GetCategory(int id)
    {
        return new CategoryResponse(id, $"Category {id}");
    }

    // -------------------------------------------------------------------------
    // NOTE: Always include ProblemDetails types for error responses
    // -------------------------------------------------------------------------
    // The generator produces error responses using ProblemDetails, so these
    // types must also be in your JsonSerializerContext:
    //
    // [JsonSerializable(typeof(ProblemDetails))]
    // [JsonSerializable(typeof(HttpValidationProblemDetails))]
}
