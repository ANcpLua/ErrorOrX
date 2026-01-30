// EOE035: Type.GetType is not AOT-safe
// ======================================
// Type.GetType(string) is not AOT-compatible.
// Types may be trimmed and unavailable at runtime.
//
// Native AOT trims unused types to reduce binary size.
// Type.GetType("SomeType") may fail at runtime if that type was trimmed.

namespace DiagnosticsDemos.Demos;

public static class EOE035_TypeGetType
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE035: Using Type.GetType with string
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Get("/type")]
    // public static ErrorOr<string> GetTypeInfo([FromQuery] string typeName)
    // {
    //     var type = Type.GetType(typeName);
    //     return type?.FullName ?? "Type not found";
    // }

    // -------------------------------------------------------------------------
    // TRIGGERS EOE035: Using Type.GetType for dynamic loading
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Get("/load")]
    // public static ErrorOr<string> LoadHandler([FromQuery] string handlerType)
    // {
    //     var type = Type.GetType(handlerType);
    //     if (type == null) return Error.NotFound("Type.NotFound", "Handler not found");
    //     return $"Loaded: {type.Name}";
    // }

    // -------------------------------------------------------------------------
    // FIXED: Use typeof() for known types
    // -------------------------------------------------------------------------
    [Get("/api/eoe035/known")]
    public static ErrorOr<string> GetKnownType()
    {
        var type = typeof(string);
        return $"Type: {type.FullName}";
    }

    // -------------------------------------------------------------------------
    // FIXED: Use a type registry/dictionary
    // -------------------------------------------------------------------------
    [Get("/api/eoe035/registry")]
    public static ErrorOr<string> GetFromRegistry([FromQuery] string key)
    {
        if (!TypeRegistry.TryGetType(key, out var type))
            return Error.NotFound("Type.NotFound", $"Type '{key}' not registered");

        return $"Type: {type.FullName}";
    }

    // -------------------------------------------------------------------------
    // FIXED: Use generic type parameters
    // -------------------------------------------------------------------------
    [Get("/api/eoe035/info/string")]
    public static ErrorOr<string> GetStringInfo() => GetTypeInfoInternal<string>();

    [Get("/api/eoe035/info/int")]
    public static ErrorOr<string> GetIntInfo() => GetTypeInfoInternal<int>();

    private static ErrorOr<string> GetTypeInfoInternal<T>()
        => $"Type: {typeof(T).FullName}, Default: {default(T)}";
}

// -------------------------------------------------------------------------
// AOT-safe type registry
// -------------------------------------------------------------------------
public static class TypeRegistry
{
    private static readonly Dictionary<string, Type> _types = new(StringComparer.OrdinalIgnoreCase)
    {
        ["string"] = typeof(string),
        ["int"] = typeof(int),
        ["guid"] = typeof(Guid),
        ["datetime"] = typeof(DateTime)
    };

    public static bool TryGetType(string key, out Type type)
        => _types.TryGetValue(key, out type!);

    public static void Register<T>(string key)
        => _types[key] = typeof(T);
}

// -------------------------------------------------------------------------
// TIP: AOT-safe alternatives to Type.GetType
// -------------------------------------------------------------------------
//
// 1. Use typeof() for compile-time known types:
//    var t = typeof(MyClass);
//
// 2. Use a dictionary registry:
//    var types = new Dictionary<string, Type> { ["key"] = typeof(MyClass) };
//
// 3. Use generic methods:
//    void Process<T>() { var t = typeof(T); }
//
// 4. Use source generators to create type catalogs
