// EOE024: Undocumented interface call
// =====================================
// Endpoint calls interface/abstract method returning ErrorOr without error documentation.
//
// When an endpoint delegates to an interface method that returns ErrorOr<T>,
// the generator cannot infer what errors might be returned. You must document
// potential errors using [ProducesError] on the endpoint or [ReturnsError] on
// the interface method.

namespace DiagnosticsDemos.Demos;

// Interface that returns ErrorOr - generator can't infer errors
public interface ITodoRepository
{
    ErrorOr<Eoe024TodoItem> GetById(int id);
    ErrorOr<List<Eoe024TodoItem>> GetAll();
    ErrorOr<Eoe024TodoItem> Create(string title);
    ErrorOr<Deleted> Delete(int id);
}

// Interface with documented errors
public interface IDocumentedRepository
{
    [ReturnsError(404, "NotFound")]
    [ReturnsError(400, "Validation")]
    ErrorOr<Eoe024TodoItem> GetById(int id);
}

public record Eoe024TodoItem(int Id, string Title);

public static class EOE024_UndocumentedInterfaceCall
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE024: Calling interface method without error documentation
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Get("/todos/{id}")]
    // public static ErrorOr<Eoe024TodoItem> GetTodo(int id, ITodoRepository repo)
    //     => repo.GetById(id);

    // -------------------------------------------------------------------------
    // TRIGGERS EOE024: Async delegation to undocumented interface
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // [Get("/todos")]
    // public static ErrorOr<List<Eoe024TodoItem>> GetAllTodos(ITodoRepository repo)
    //     => repo.GetAll();

    // -------------------------------------------------------------------------
    // FIXED: Add [ProducesError] to document possible errors
    // -------------------------------------------------------------------------
    // NOTE: Using status codes that don't conflict with ValidationProblem union type
    [Get("/api/eoe024/todos/{id}")]
    [ProducesError(404, "NotFound")]
    public static ErrorOr<Eoe024TodoItem> GetTodoWithDocs(int id, ITodoRepository repo)
    {
        return repo.GetById(id);
    }

    // -------------------------------------------------------------------------
    // FIXED: Document all errors the interface method might return
    // -------------------------------------------------------------------------
    [Post("/api/eoe024/todos")]
    [ProducesError(409, "Conflict")]
    public static ErrorOr<Eoe024TodoItem> CreateTodo([FromBody] string title, ITodoRepository repo)
    {
        return repo.Create(title);
    }

    [Delete("/api/eoe024/todos/{id}")]
    [ProducesError(404, "NotFound")]
    [ProducesError(403, "Forbidden")]
    public static ErrorOr<Deleted> DeleteTodo(int id, ITodoRepository repo)
    {
        return repo.Delete(id);
    }

    // -------------------------------------------------------------------------
    // FIXED: Use interface with [ReturnsError] attributes
    // -------------------------------------------------------------------------
    [Get("/api/eoe024/documented/{id}")]
    public static ErrorOr<Eoe024TodoItem> GetFromDocumentedRepo(int id, IDocumentedRepository repo)
    {
        return repo.GetById(id);
        // No warning - interface method has [ReturnsError]
    }

    // -------------------------------------------------------------------------
    // FIXED: Don't delegate - handle errors inline
    // -------------------------------------------------------------------------
    [Get("/api/eoe024/inline/{id}")]
    public static ErrorOr<Eoe024TodoItem> GetTodoInline(int id)
    {
        // All error paths are visible to the generator
        if (id <= 0)
        {
            return Error.Validation("Todo.InvalidId", "ID must be positive");
        }

        if (id > 1000)
        {
            return Error.NotFound("Todo.NotFound", $"Todo {id} not found");
        }

        return new Eoe024TodoItem(id, $"Todo {id}");
    }

    // -------------------------------------------------------------------------
    // TIP: [ProducesError] accepts status code and error type name
    // -------------------------------------------------------------------------
    // Common patterns:
    // [ProducesError(400, "Validation")]  -> Bad Request
    // [ProducesError(401, "Unauthorized")] -> Unauthorized
    // [ProducesError(403, "Forbidden")]   -> Forbidden
    // [ProducesError(404, "NotFound")]    -> Not Found
    // [ProducesError(409, "Conflict")]    -> Conflict
    // [ProducesError(500, "Failure")]     -> Internal Server Error
}
