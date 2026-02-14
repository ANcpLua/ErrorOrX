namespace DiagnosticsDemos.Demos;

/// <summary>
///     EOE020: Route constraint type mismatch — Route constraint type does not match parameter type
///     (e.g. {id:int} requires an int parameter, not string).
/// </summary>
/// <remarks>
///     Route constraints like :int, :guid, :bool validate the URL format.
///     The method parameter type should match the constraint.
/// </remarks>
public static class EOE020_RouteConstraintTypeMismatch
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE020: {id:int} constraint with string parameter
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Get("/todos/{id:int}")]
    // public static ErrorOr<string> GetByIdString(string id) => $"Todo {id}";

    // -------------------------------------------------------------------------
    // TRIGGERS EOE020: {id:guid} constraint with int parameter
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Get("/items/{id:guid}")]
    // public static ErrorOr<string> GetByGuidAsInt(int id) => $"Item {id}";

    // -------------------------------------------------------------------------
    // TRIGGERS EOE020: {active:bool} constraint with string parameter
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Get("/filter/{active:bool}")]
    // public static ErrorOr<string> FilterByBool(string active) => $"Active: {active}";

    // -------------------------------------------------------------------------
    // FIXED: Match parameter type to constraint
    // -------------------------------------------------------------------------
    [Get("/api/eoe020/todos/{id:int}")]
    public static ErrorOr<string> GetById(int id)
    {
        return $"Todo {id}";
    }

    [Get("/api/eoe020/items/{id:guid}")]
    public static ErrorOr<string> GetByGuid(Guid id)
    {
        return $"Item {id}";
    }

    [Get("/api/eoe020/filter/{active:bool}")]
    public static ErrorOr<string> FilterByBool(bool active)
    {
        return $"Active: {active}";
    }

    // -------------------------------------------------------------------------
    // FIXED: Common constraint patterns
    // -------------------------------------------------------------------------
    [Get("/api/eoe020/users/{userId:long}")]
    public static ErrorOr<string> GetUser(long userId)
    {
        return $"User {userId}";
    }

    [Get("/api/eoe020/products/{price:decimal}")]
    public static ErrorOr<string> GetByPrice(decimal price)
    {
        return $"Price: {price}";
    }

    [Get("/api/eoe020/orders/{date:datetime}")]
    public static ErrorOr<string> GetByDate(DateTime date)
    {
        return $"Date: {date:yyyy-MM-dd}";
    }

    // -------------------------------------------------------------------------
    // FIXED: Constraints can be combined
    // -------------------------------------------------------------------------
    [Get("/api/eoe020/paged/{page:int:min(1)}/{size:int:range(1,100)}")]
    public static ErrorOr<string> GetPaged(int page, int size)
    {
        return $"Page {page}, Size {size}";
    }

    // -------------------------------------------------------------------------
    // FIXED: String parameters don't need constraints
    // -------------------------------------------------------------------------
    [Get("/api/eoe020/search/{query}")]
    public static ErrorOr<string> Search(string query)
    {
        return $"Searching: {query}";
    }

    // -------------------------------------------------------------------------
    // Available route constraints reference:
    // -------------------------------------------------------------------------
    // :int       -> int
    // :long      -> long
    // :guid      -> Guid
    // :bool      -> bool
    // :decimal   -> decimal
    // :double    -> double
    // :float     -> float
    // :datetime  -> DateTime
    // :alpha     -> string (letters only)
    // :minlength(n) -> string
    // :maxlength(n) -> string
    // :length(n)    -> string
    // :min(n)       -> int/long
    // :max(n)       -> int/long
    // :range(n,m)   -> int/long
    // :regex(pattern) -> string
}
