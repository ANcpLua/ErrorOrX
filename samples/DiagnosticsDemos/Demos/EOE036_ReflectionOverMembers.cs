namespace DiagnosticsDemos.Demos;

public class DataModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
///     EOE036: Reflection over members is not AOT-safe — Reflection over type members is not AOT-compatible
///     because members may be trimmed and unavailable at runtime.
/// </summary>
/// <remarks>
///     Native AOT trims unused members to reduce binary size.
///     GetMethods(), GetProperties(), etc. may return incomplete results
///     or fail entirely if the members were trimmed.
/// </remarks>
public static class EOE036_ReflectionOverMembers
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE036: Using GetProperties()
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Get("/properties")]
    // public static ErrorOr<string> GetProperties()
    // {
    //     var props = typeof(DataModel).GetProperties();
    //     return string.Join(", ", props.Select(p => p.Name));
    // }

    // -------------------------------------------------------------------------
    // TRIGGERS EOE036: Using GetMethods()
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Get("/methods")]
    // public static ErrorOr<string> GetMethods()
    // {
    //     var methods = typeof(DataModel).GetMethods();
    //     return $"Method count: {methods.Length}";
    // }

    // -------------------------------------------------------------------------
    // TRIGGERS EOE036: Using GetProperty()
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Get("/property/{name}")]
    // public static ErrorOr<string> GetPropertyValue(string name)
    // {
    //     var model = new DataModel { Id = 1, Name = "Test" };
    //     var prop = typeof(DataModel).GetProperty(name);
    //     var value = prop?.GetValue(model);
    //     return value?.ToString() ?? "null";
    // }

    // -------------------------------------------------------------------------
    // FIXED: Access properties directly
    // -------------------------------------------------------------------------
    [Get("/api/eoe036/model/{id}")]
    public static ErrorOr<DataModel> GetModel(int id)
    {
        return new DataModel { Id = id, Name = $"Item {id}", Description = "Description" };
    }

    // -------------------------------------------------------------------------
    // FIXED: Use explicit property access with a switch
    // -------------------------------------------------------------------------
    [Get("/api/eoe036/property/{name}")]
    public static ErrorOr<string> GetPropertyByName(string name)
    {
        var model = new DataModel { Id = 1, Name = "Test", Description = "Desc" };

        return name.ToLowerInvariant() switch
        {
            "id" => model.Id.ToString(),
            "name" => model.Name,
            "description" => model.Description,
            _ => Error.NotFound("Property.NotFound", $"Property '{name}' not found")
        };
    }

    // -------------------------------------------------------------------------
    // FIXED: Use source-generated serialization
    // -------------------------------------------------------------------------
    [Get("/api/eoe036/serialize/{id}")]
    public static ErrorOr<DataModel> SerializeModel(int id)
    {
        // JsonSerializer with source-generated context handles serialization
        // without reflection
        return new DataModel { Id = id, Name = $"Item {id}", Description = "Desc" };
    }
}

// -------------------------------------------------------------------------
// TIP: AOT-safe alternatives to reflection
// -------------------------------------------------------------------------
//
// 1. Direct property access:
//    var value = obj.PropertyName;
//
// 2. Switch expressions for dynamic access:
//    return name switch { "Id" => obj.Id, "Name" => obj.Name };
//
// 3. Source generators:
//    - System.Text.Json source generation
//    - Custom source generators for type metadata
//
// 4. Compile-time known operations:
//    - nameof(property) for property names
//    - typeof(T) for type information
