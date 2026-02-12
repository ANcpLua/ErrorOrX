namespace DiagnosticsDemos.Demos;

/// <summary>
/// EOE038: 'dynamic' is not AOT-safe — The 'dynamic' keyword uses runtime binding,
/// which is not supported in NativeAOT.
/// </summary>
/// <remarks>
/// The 'dynamic' keyword defers type resolution to runtime using the
/// Dynamic Language Runtime (DLR). This requires runtime code generation
/// and reflection, neither of which are available in AOT-compiled apps.
/// </remarks>
public static class EOE038_DynamicKeyword
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE038: Using dynamic keyword
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Get("/dynamic")]
    // public static ErrorOr<string> ProcessDynamic([FromQuery] string input)
    // {
    //     dynamic value = input;
    //     return value.ToUpper(); // Runtime binding
    // }

    // -------------------------------------------------------------------------
    // TRIGGERS EOE038: Dynamic parameter
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Post("/process")]
    // public static ErrorOr<string> ProcessDynamicObject([FromBody] dynamic data)
    // {
    //     return $"Name: {data.Name}, Value: {data.Value}";
    // }

    // -------------------------------------------------------------------------
    // TRIGGERS EOE038: Dynamic in expressions
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Get("/calculate")]
    // public static ErrorOr<string> CalculateDynamic([FromQuery] int a, [FromQuery] int b)
    // {
    //     dynamic x = a;
    //     dynamic y = b;
    //     return (x + y).ToString(); // Runtime binding for operator
    // }

    // -------------------------------------------------------------------------
    // FIXED: Use strongly-typed parameters
    // -------------------------------------------------------------------------
    [Post("/api/eoe038/process")]
    public static ErrorOr<string> ProcessTyped([FromBody] TypedData data)
    {
        return $"Name: {data.Name}, Value: {data.Value}";
    }

    // -------------------------------------------------------------------------
    // FIXED: Use concrete types instead of dynamic
    // -------------------------------------------------------------------------
    [Get("/api/eoe038/calculate")]
    public static ErrorOr<int> CalculateTyped([FromQuery] int a, [FromQuery] int b)
    {
        return a + b;
    }

    // -------------------------------------------------------------------------
    // FIXED: Use object with type checking when flexibility is needed
    // -------------------------------------------------------------------------
    [Post("/api/eoe038/flexible")]
    public static ErrorOr<string> ProcessFlexible([FromBody] FlexibleData data)
    {
        return data.Value switch
        {
            string s => $"String: {s}",
            int i => $"Integer: {i}",
            double d => $"Double: {d}",
            bool b => $"Boolean: {b}",
            null => "Null",
            _ => $"Unknown type: {data.Value.GetType().Name}"
        };
    }

    // -------------------------------------------------------------------------
    // FIXED: Use generics for type-safe flexible operations
    // -------------------------------------------------------------------------
    [Get("/api/eoe038/string")]
    public static ErrorOr<string> GetProcessedString([FromQuery] string input)
    {
        return ProcessValue(input);
    }

    [Get("/api/eoe038/int")]
    public static ErrorOr<string> GetProcessedInt([FromQuery] int input)
    {
        return ProcessValue(input);
    }

    private static string ProcessValue<T>(T value) where T : notnull
    {
        return $"Processed {typeof(T).Name}: {value}";
    }
}

public record TypedData(string Name, int Value);

public class FlexibleData
{
    public object? Value { get; set; }
}

// -------------------------------------------------------------------------
// TIP: AOT-safe alternatives to 'dynamic'
// -------------------------------------------------------------------------
//
// 1. Strongly-typed classes/records:
//    record MyData(string Name, int Value);
//
// 2. Object with pattern matching:
//    return obj switch { string s => ..., int i => ... };
//
// 3. Generic methods:
//    T Process<T>(T value) where T : notnull;
//
// 4. Interfaces for polymorphism:
//    interface IProcessable { string Process(); }
//
// 5. System.Text.Json for JSON flexibility:
//    JsonElement for unstructured JSON parsing
