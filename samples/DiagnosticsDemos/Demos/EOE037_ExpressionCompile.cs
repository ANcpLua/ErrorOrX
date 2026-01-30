// EOE037: Expression.Compile is not AOT-safe
// ============================================
// Expression.Compile() generates code at runtime.
// This is not supported in NativeAOT.
//
// Expression trees are a powerful feature for building dynamic queries
// and delegates, but Compile() requires runtime code generation which
// is not available in AOT-compiled applications.

using System.Linq.Expressions;

namespace DiagnosticsDemos.Demos;

public static class EOE037_ExpressionCompile
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE037: Using Expression.Compile()
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Get("/calculate")]
    // public static ErrorOr<int> Calculate([FromQuery] int a, [FromQuery] int b)
    // {
    //     Expression<Func<int, int, int>> expr = (x, y) => x + y;
    //     var compiled = expr.Compile();
    //     return compiled(a, b);
    // }

    // -------------------------------------------------------------------------
    // TRIGGERS EOE037: Building and compiling expressions dynamically
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Get("/dynamic")]
    // public static ErrorOr<string> DynamicFilter([FromQuery] string property, [FromQuery] string value)
    // {
    //     var param = Expression.Parameter(typeof(DataItem), "x");
    //     var prop = Expression.Property(param, property);
    //     var constant = Expression.Constant(value);
    //     var body = Expression.Equal(prop, constant);
    //     var lambda = Expression.Lambda<Func<DataItem, bool>>(body, param);
    //     var compiled = lambda.Compile(); // AOT-unsafe
    //     return "filter created";
    // }

    // -------------------------------------------------------------------------
    // FIXED: Use regular delegates instead of compiled expressions
    // -------------------------------------------------------------------------
    [Get("/api/eoe037/calculate")]
    public static ErrorOr<int> CalculateFixed([FromQuery] int a, [FromQuery] int b)
    {
        // Direct delegate - no compilation needed
        Func<int, int, int> add = (x, y) => x + y;
        return add(a, b);
    }

    // -------------------------------------------------------------------------
    // FIXED: Use predefined filters instead of dynamic expressions
    // -------------------------------------------------------------------------
    [Get("/api/eoe037/filter")]
    public static ErrorOr<List<DataItem>> FilterItems(
        [FromQuery] string? name,
        [FromQuery] int? minValue)
    {
        var items = GetSampleItems();

        if (!string.IsNullOrEmpty(name))
            items = items.Where(x => x.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();

        if (minValue.HasValue)
            items = items.Where(x => x.Value >= minValue.Value).ToList();

        return items;
    }

    // -------------------------------------------------------------------------
    // FIXED: Use specification pattern for complex filters
    // -------------------------------------------------------------------------
    [Get("/api/eoe037/search")]
    public static ErrorOr<List<DataItem>> SearchItems([AsParameters] SearchSpec spec)
    {
        var items = GetSampleItems();
        return spec.Apply(items).ToList();
    }

    private static List<DataItem> GetSampleItems() =>
    [
        new DataItem { Id = 1, Name = "Alpha", Value = 100 },
        new DataItem { Id = 2, Name = "Beta", Value = 200 },
        new DataItem { Id = 3, Name = "Gamma", Value = 300 }
    ];
}

public class DataItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

// -------------------------------------------------------------------------
// Specification pattern - AOT-safe alternative to dynamic expressions
// -------------------------------------------------------------------------
public class SearchSpec
{
    public string? Name { get; set; }
    public int? MinValue { get; set; }
    public int? MaxValue { get; set; }

    public IEnumerable<DataItem> Apply(IEnumerable<DataItem> items)
    {
        if (!string.IsNullOrEmpty(Name))
            items = items.Where(x => x.Name.Contains(Name, StringComparison.OrdinalIgnoreCase));

        if (MinValue.HasValue)
            items = items.Where(x => x.Value >= MinValue.Value);

        if (MaxValue.HasValue)
            items = items.Where(x => x.Value <= MaxValue.Value);

        return items;
    }
}

// -------------------------------------------------------------------------
// TIP: AOT-safe alternatives to Expression.Compile()
// -------------------------------------------------------------------------
//
// 1. Regular delegates:
//    Func<int, int> fn = x => x * 2;
//
// 2. Specification pattern:
//    class Spec { IEnumerable<T> Apply(IEnumerable<T> items) }
//
// 3. Predefined operations with switch:
//    return op switch { "add" => a + b, "sub" => a - b };
//
// 4. Source generators for compile-time code generation
