// EOE033: Handler method name not PascalCase
// ============================================
// Handler method name should follow PascalCase convention.
// The first character should be uppercase, and no underscores should be used.
//
// C# naming conventions use PascalCase for public methods.
// This helps maintain consistency and readability.

namespace DiagnosticsDemos.Demos;

public static class EOE033_MethodNameNotPascalCase
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE033: Method name starts with lowercase
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Get("/items/{id}")]
    // public static ErrorOr<string> getById(int id) => $"Item {id}";

    // -------------------------------------------------------------------------
    // TRIGGERS EOE033: Method name uses underscores
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Get("/items/{id}")]
    // public static ErrorOr<string> Get_By_Id(int id) => $"Item {id}";

    // -------------------------------------------------------------------------
    // TRIGGERS EOE033: Method name is snake_case
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Get("/items/{id}")]
    // public static ErrorOr<string> get_by_id(int id) => $"Item {id}";

    // -------------------------------------------------------------------------
    // TRIGGERS EOE033: Method name is camelCase (not PascalCase)
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic (warning):
    //
    // [Get("/items")]
    // public static ErrorOr<string> getAllItems() => "items";

    // -------------------------------------------------------------------------
    // FIXED: Use PascalCase method names
    // -------------------------------------------------------------------------
    [Get("/api/eoe033/items/{id}")]
    public static ErrorOr<string> GetById(int id) => $"Item {id}";

    [Get("/api/eoe033/items")]
    public static ErrorOr<string> GetAllItems() => "all items";

    [Post("/api/eoe033/items")]
    public static ErrorOr<string> CreateItem([FromBody] string name) => $"Created: {name}";

    [Put("/api/eoe033/items/{id}")]
    public static ErrorOr<string> UpdateItem(int id, [FromBody] string name)
        => $"Updated {id}: {name}";

    [Delete("/api/eoe033/items/{id}")]
    public static ErrorOr<Deleted> DeleteItem(int id) => Result.Deleted;

    // -------------------------------------------------------------------------
    // FIXED: Common naming patterns
    // -------------------------------------------------------------------------
    [Get("/api/eoe033/users/{id}")]
    public static ErrorOr<string> GetUserById(int id) => $"User {id}";

    [Get("/api/eoe033/users")]
    public static ErrorOr<string> ListUsers() => "users";

    [Post("/api/eoe033/users")]
    public static ErrorOr<string> CreateUser([FromBody] string name) => $"Created: {name}";

    [Get("/api/eoe033/search")]
    public static ErrorOr<string> SearchItems([FromQuery] string query) => $"Searching: {query}";

    // -------------------------------------------------------------------------
    // TIP: Method naming conventions for APIs
    // -------------------------------------------------------------------------
    // GET single:    GetById, GetUserById, GetOrderDetails
    // GET list:      GetAll, List, Search, Find
    // POST:          Create, Add, Register
    // PUT:           Update, Replace, Set
    // PATCH:         Patch, PartialUpdate
    // DELETE:        Delete, Remove, Cancel
}
