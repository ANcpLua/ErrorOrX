// EOE034: Activator.CreateInstance is not AOT-safe
// ==================================================
// Activator.CreateInstance is not AOT-compatible.
// Use factory methods or explicit construction instead.
//
// Native AOT requires all types to be known at compile time.
// Activator.CreateInstance uses reflection to create objects at runtime,
// which won't work in AOT-compiled applications.

namespace DiagnosticsDemos.Demos;

public class DataProcessor
{
    public string Process(string input)
    {
        return $"Processed: {input}";
    }
}

public interface IDataProcessor
{
    string Process(string input);
}

public class DefaultProcessor : IDataProcessor
{
    public string Process(string input)
    {
        return $"Default: {input}";
    }
}

public class JsonProcessor : IDataProcessor
{
    public string Process(string input)
    {
        return $"JSON: {input}";
    }
}

public class XmlProcessor : IDataProcessor
{
    public string Process(string input)
    {
        return $"XML: {input}";
    }
}

public static class EOE034_ActivatorCreateInstance
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE034: Using Activator.CreateInstance<T>()
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Get("/process")]
    // public static ErrorOr<string> ProcessWithActivator([FromQuery] string input)
    // {
    //     var processor = Activator.CreateInstance<DataProcessor>();
    //     return processor.Process(input);
    // }

    // -------------------------------------------------------------------------
    // TRIGGERS EOE034: Using Activator.CreateInstance(Type)
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Get("/dynamic")]
    // public static ErrorOr<string> CreateDynamic([FromQuery] string typeName)
    // {
    //     var type = Type.GetType(typeName);
    //     var instance = Activator.CreateInstance(type!);
    //     return instance?.ToString() ?? "null";
    // }

    // -------------------------------------------------------------------------
    // FIXED: Use explicit construction (new)
    // -------------------------------------------------------------------------
    [Get("/api/eoe034/explicit")]
    public static ErrorOr<string> ProcessExplicit([FromQuery] string input)
    {
        var processor = new DataProcessor();
        return processor.Process(input);
    }

    // -------------------------------------------------------------------------
    // FIXED: Use dependency injection
    // -------------------------------------------------------------------------
    [Get("/api/eoe034/injected")]
    public static ErrorOr<string> ProcessInjected(
        [FromQuery] string input,
        [FromServices] IDataProcessor processor)
    {
        return processor.Process(input);
    }

    // -------------------------------------------------------------------------
    // FIXED: Use factory methods
    // -------------------------------------------------------------------------
    [Get("/api/eoe034/factory")]
    public static ErrorOr<string> ProcessWithFactory([FromQuery] string input)
    {
        var processor = ProcessorFactory.Create();
        return processor.Process(input);
    }
}

// -------------------------------------------------------------------------
// Factory pattern for AOT-safe object creation
// -------------------------------------------------------------------------
public static class ProcessorFactory
{
    public static IDataProcessor Create()
    {
        return new DefaultProcessor();
    }

    public static IDataProcessor Create(string type)
    {
        return type switch
        {
            "json" => new JsonProcessor(),
            "xml" => new XmlProcessor(),
            _ => new DefaultProcessor()
        };
    }
}

// -------------------------------------------------------------------------
// TIP: AOT-safe alternatives to Activator.CreateInstance
// -------------------------------------------------------------------------
//
// 1. Direct construction:
//    var obj = new MyClass();
//
// 2. Factory methods:
//    var obj = MyFactory.Create();
//
// 3. Dependency injection:
//    services.AddSingleton<IMyService, MyService>();
//
// 4. Source generators (for complex scenarios)
