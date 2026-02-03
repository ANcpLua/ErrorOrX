// EOE035: Type.GetType is not AOT-safe (for dynamic patterns)
// =============================================================
// Type.GetType(string) with DYNAMIC type names is not AOT-compatible.
// Types may be trimmed and unavailable at runtime.
//
// SAFE: Type.GetType("System.String") - string literal is analyzable
// UNSAFE: Type.GetType(userInput) - dynamic type name
// UNSAFE: Type.GetType("...", ignoreCase: true) - case-insensitive search
//
// See: https://learn.microsoft.com/dotnet/core/deploying/trimming/trimming-intrinsic

namespace DiagnosticsDemos.Demos;

public static class EOE035_TypeGetType
{
    // -------------------------------------------------------------------------
    // SAFE: String literal Type.GetType (NO WARNING)
    // -------------------------------------------------------------------------
    // Per Microsoft docs, string literals are statically analyzable
    [Get("/api/eoe035/literal")]
    public static ErrorOr<string> GetWithLiteral()
    {
        // ✅ No EOE035 warning - trimmer can see "System.String" at compile time
        var type = Type.GetType("System.String");
        return type?.FullName ?? "Not found";
    }

    // -------------------------------------------------------------------------
    // TRIGGERS EOE035: Dynamic type name from parameter
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Get("/api/eoe035/dynamic")]
    // public static ErrorOr<string> GetDynamic([FromQuery] string typeName)
    // {
    //     // ⚠️ EOE035: Type.GetType with a dynamic type name
    //     var type = Type.GetType(typeName);
    //     return type?.FullName ?? "Type not found";
    // }

    // -------------------------------------------------------------------------
    // TRIGGERS EOE035: Case-insensitive search
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Get("/api/eoe035/ignorecase")]
    // public static ErrorOr<string> GetCaseInsensitive()
    // {
    //     // ⚠️ EOE035: ignoreCase:true breaks trimmer analysis
    //     var type = Type.GetType("system.string", ignoreCase: true, throwOnError: false);
    //     return type?.FullName ?? "Type not found";
    // }

    // -------------------------------------------------------------------------
    // SAFE: Explicit case-sensitive search (NO WARNING)
    // -------------------------------------------------------------------------
    [Get("/api/eoe035/casesensitive")]
    public static ErrorOr<string> GetCaseSensitive()
    {
        // ✅ No warning - string literal + explicit ignoreCase: false
        var type = Type.GetType("System.String", throwOnError: false, ignoreCase: false);
        return type?.FullName ?? "Not found";
    }

    // -------------------------------------------------------------------------
    // FIXED: Use typeof() for known types (BEST APPROACH)
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
