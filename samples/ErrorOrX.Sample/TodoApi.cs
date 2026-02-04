namespace ErrorOrX.Sample;

/// <summary>
///     Todo API demonstrating ErrorOr endpoints with automatic error inference.
/// </summary>
/// <remarks>
///     <para>
///         Smart Binding infers parameter sources automatically based on type and context.
///         No explicit binding attributes are required for common patterns.
///     </para>
///     <list type="table">
///         <listheader>
///             <term>Parameter Type</term>
///             <description>Inferred Source</description>
///         </listheader>
///         <item>
///             <term><c>Guid id</c> (matches route <c>{id}</c>)</term>
///             <description>Route</description>
///         </item>
///         <item>
///             <term><c>int page</c> (primitive, not in route)</term>
///             <description>Query</description>
///         </item>
///         <item>
///             <term><c>CreateTodoRequest</c> (POST/PUT/PATCH)</term>
///             <description>Body</description>
///         </item>
///         <item>
///             <term><c>ITodoService</c> (interface)</term>
///             <description>Service (DI)</description>
///         </item>
///         <item>
///             <term>
///                 <c>CancellationToken</c>
///             </term>
///             <description>Special (from request)</description>
///         </item>
///         <item>
///             <term>
///                 <c>HttpContext</c>
///             </term>
///             <description>Special (current context)</description>
///         </item>
///         <item>
///             <term>
///                 <c>IFormFile</c>
///             </term>
///             <description>Special (uploaded file)</description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <para>Before (explicit attributes):</para>
///     <code>
///     public static Task&lt;ErrorOr&lt;Todo&gt;&gt; GetById(
///         [FromRoute] Guid id,
///         [FromServices] ITodoService svc,
///         CancellationToken ct)
///     </code>
///     <para>After (smart binding - same generated code):</para>
///     <code>
///     public static Task&lt;ErrorOr&lt;Todo&gt;&gt; GetById(
///         Guid id,
///         ITodoService svc,
///         CancellationToken ct)
///     </code>
/// </example>
public static class TodoApi
{
    /// <summary>
    ///     Get all todos.
    /// </summary>
    [Get("/api/todos")]
    public static Task<ErrorOr<List<Todo>>> GetAll(ITodoService svc, CancellationToken ct)
    {
        return svc.GetAllAsync(ct);
    }

    /// <summary>
    ///     Get todo by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the todo item.</param>
    [Get("/api/todos/{id:guid}")]
    public static Task<ErrorOr<Todo>> GetById(Guid id, ITodoService svc, CancellationToken ct)
    {
        return svc.GetByIdAsync(id, ct);
    }

    /// <summary>
    ///     Create a new todo.
    /// </summary>
    /// <param name="request">The todo creation request containing title and description.</param>
    [Post("/api/todos")]
    public static Task<ErrorOr<Todo>> Create(CreateTodoRequest request, ITodoService svc, CancellationToken ct)
    {
        return svc.CreateAsync(request, ct);
    }

    /// <summary>
    ///     Update a todo.
    /// </summary>
    [Put("/api/todos/{id:guid}")]
    public static Task<ErrorOr<Updated>> Update(Guid id, UpdateTodoRequest request, ITodoService svc,
        CancellationToken ct)
    {
        return svc.UpdateAsync(id, request, ct);
    }

    /// <summary>
    ///     Delete a todo.
    /// </summary>
    [Delete("/api/todos/{id:guid}")]
    public static Task<ErrorOr<Deleted>> Delete(Guid id, ITodoService svc, CancellationToken ct)
    {
        return svc.DeleteAsync(id, ct);
    }
}
