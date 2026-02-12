namespace DiagnosticsDemos.Demos;

// A complex type without TryParse - cannot be used in routes
public class ComplexFilter
{
    public string Name { get; set; } = string.Empty;
    public int Page { get; set; }
}

// A custom type WITH TryParse - CAN be used in routes
public readonly struct CustomId
{
    public int Value { get; }

    public CustomId(int value)
    {
        Value = value;
    }

    // This static TryParse method makes it route-bindable
    public static bool TryParse(string? input, out CustomId result)
    {
        if (int.TryParse(input, out var value))
        {
            result = new CustomId(value);
            return true;
        }

        result = default;
        return false;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}

/// <summary>
/// EOE010: Invalid [FromRoute] type — [FromRoute] parameter type is not a supported primitive and has no TryParse.
/// </summary>
/// <remarks>
/// Route parameters must be parseable from the URL string. This means they must be
/// primitive types (int, string, Guid, etc.) or types with a static TryParse method.
/// </remarks>
public static class EOE010_InvalidFromRouteType
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE010: Complex type in route without TryParse
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Get("/todos/{filter}")]
    // public static ErrorOr<string> GetByFilter([FromRoute] ComplexFilter filter)
    //     => $"Filter: {filter.Name}";

    // -------------------------------------------------------------------------
    // FIXED: Use primitive types for route parameters
    // -------------------------------------------------------------------------
    [Get("/api/eoe010/todos/{id}")]
    public static ErrorOr<string> GetById(int id)
    {
        return $"Todo {id}";
    }

    [Get("/api/eoe010/users/{userId:guid}")]
    public static ErrorOr<string> GetUser(Guid userId)
    {
        return $"User {userId}";
    }

    [Get("/api/eoe010/items/{name}")]
    public static ErrorOr<string> GetByName(string name)
    {
        return $"Item: {name}";
    }

    // -------------------------------------------------------------------------
    // FIXED: Custom type WITH TryParse can be used
    // -------------------------------------------------------------------------
    [Get("/api/eoe010/custom/{id}")]
    public static ErrorOr<string> GetByCustomId(CustomId id)
    {
        return $"Custom ID: {id}";
    }

    // -------------------------------------------------------------------------
    // FIXED: For complex filtering, use query parameters or [AsParameters]
    // -------------------------------------------------------------------------
    [Get("/api/eoe010/search")]
    public static ErrorOr<string> Search(
        [FromQuery] string name,
        [FromQuery] int page = 1)
    {
        return $"Searching for {name} on page {page}";
    }

    // -------------------------------------------------------------------------
    // Supported route parameter types
    // -------------------------------------------------------------------------
    [Get("/api/eoe010/types/{stringVal}/{intVal}/{longVal}/{guidVal}/{boolVal}/{dateVal}")]
    public static ErrorOr<string> AllTypes(
        string stringVal,
        int intVal,
        long longVal,
        Guid guidVal,
        bool boolVal,
        DateTime dateVal)
    {
        return $"string={stringVal}, int={intVal}, long={longVal}, guid={guidVal}, bool={boolVal}, date={dateVal}";
    }
}
