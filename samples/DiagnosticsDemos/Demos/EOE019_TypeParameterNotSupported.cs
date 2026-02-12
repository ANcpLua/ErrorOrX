namespace DiagnosticsDemos.Demos;

// Supporting types defined first to be visible in the endpoint class
public record Item019(int Id, string Title);

public record User019(int Id, string Name);

public record Product019(int Id, string Name, decimal Price);

public record PagedResult019<T>(List<T> Items, int TotalCount, int Page, int PageSize);

/// <summary>
/// EOE019: Type parameter not supported — Open generic type parameters cannot be used in endpoint return types.
/// </summary>
/// <remarks>
/// ErrorOr endpoints require concrete types so the generator can emit
/// proper JSON serialization and HTTP response code.
/// </remarks>
public static class EOE019_TypeParameterNotSupported
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE019: Generic method with type parameter in return
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Get("/items")]
    // public static ErrorOr<T> GetItem<T>() where T : class => default!;

    // -------------------------------------------------------------------------
    // TRIGGERS EOE019: Generic method with constraint
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Get("/data")]
    // public static ErrorOr<TData> GetData<TData>() where TData : new() => new();

    // -------------------------------------------------------------------------
    // FIXED: Use concrete types
    // -------------------------------------------------------------------------
    [Get("/api/eoe019/string")]
    public static ErrorOr<string> GetString()
    {
        return "hello";
    }

    [Get("/api/eoe019/item/{id}")]
    public static ErrorOr<Item019> GetItem(int id)
    {
        return new Item019(id, $"Item {id}");
    }

    // -------------------------------------------------------------------------
    // FIXED: Use closed generic types (with concrete type arguments)
    // -------------------------------------------------------------------------
    [Get("/api/eoe019/items")]
    public static ErrorOr<List<Item019>> GetItems()
    {
        return new List<Item019>
        {
            new(1, "First"),
            new(2, "Second")
        };
    }

    [Get("/api/eoe019/dict")]
    public static ErrorOr<Dictionary<string, Item019>> GetItemDict()
    {
        return new Dictionary<string, Item019>
        {
            ["first"] = new(1, "First"),
            ["second"] = new(2, "Second")
        };
    }

    // -------------------------------------------------------------------------
    // FIXED: Generic wrapper with concrete type argument
    // -------------------------------------------------------------------------
    [Get("/api/eoe019/paged")]
    public static ErrorOr<PagedResult019<Item019>> GetPagedItems()
    {
        return new PagedResult019<Item019>(
            [new Item019(1, "First")],
            100,
            1,
            10);
    }

    // -------------------------------------------------------------------------
    // TIP: Create specific endpoint methods for each concrete type
    // -------------------------------------------------------------------------
    [Get("/api/eoe019/users")]
    public static ErrorOr<List<User019>> GetUsers()
    {
        return new List<User019> { new(1, "Alice"), new(2, "Bob") };
    }

    [Get("/api/eoe019/products")]
    public static ErrorOr<List<Product019>> GetProducts()
    {
        return new List<Product019> { new(1, "Widget", 9.99m) };
    }
}
