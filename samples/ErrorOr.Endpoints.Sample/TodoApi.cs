using ErrorOr.Core.Errors;
using ErrorOr.Endpoints.Sample.Domain;
using Microsoft.AspNetCore.Mvc;

namespace ErrorOr.Endpoints.Sample;

/// <summary>
///     Todo API demonstrating automatic error inference from [ReturnsError] on interfaces.
///     NO [ProducesError] needed - errors are automatically read from ITodoService.
/// </summary>
public static class TodoApi
{
    /// <summary>
    ///     Get all todos. ITodoService.GetAllAsync has no [ReturnsError] so only 500 is inferred.
    /// </summary>
    [Get("/api/todos")]
    public static Task<ErrorOr<List<Todo>>> GetAll([FromServices] ITodoService svc, CancellationToken ct)
    {
        return svc.GetAllAsync(ct);
    }

    /// <summary>
    ///     Get todo by ID. ITodoService.GetByIdAsync has [ReturnsError(NotFound)] so 404 is inferred.
    /// </summary>
    [Get("/api/todos/{id:guid}")]
    public static Task<ErrorOr<Todo>> GetById([FromRoute] Guid id, [FromServices] ITodoService svc,
        CancellationToken ct)
    {
        return svc.GetByIdAsync(id, ct);
    }

    /// <summary>
    ///     Create a new todo. ITodoService.CreateAsync has [ReturnsError(Validation)] so 400 is inferred.
    /// </summary>
    [Post("/api/todos")]
    public static Task<ErrorOr<Todo>> Create([FromBody] CreateTodoRequest request, [FromServices] ITodoService svc,
        CancellationToken ct)
    {
        return svc.CreateAsync(request, ct);
    }

    /// <summary>
    ///     Update a todo. ITodoService.UpdateAsync has [ReturnsError(NotFound)] so 404 is inferred.
    /// </summary>
    [Put("/api/todos/{id:guid}")]
    public static Task<ErrorOr<Updated>> Update([FromRoute] Guid id, [FromBody] UpdateTodoRequest request,
        [FromServices] ITodoService svc, CancellationToken ct)
    {
        return svc.UpdateAsync(id, request, ct);
    }

    /// <summary>
    ///     Delete a todo. ITodoService.DeleteAsync has [ReturnsError(NotFound)] so 404 is inferred.
    /// </summary>
    [Delete("/api/todos/{id:guid}")]
    public static Task<ErrorOr<Deleted>> Delete([FromRoute] Guid id, [FromServices] ITodoService svc,
        CancellationToken ct)
    {
        return svc.DeleteAsync(id, ct);
    }

    /// <summary>
    ///     Demonstrates using Error.Custom() with [ProducesError] - still needed for custom status codes.
    /// </summary>
    [Post("/api/todos/{id:guid}/rate-limit")]
    [ProducesError(429, "RateLimit.Exceeded")]
    public static ErrorOr<Success> RateLimitWithAnnotation([FromRoute] Guid id)
    {
        return Error.Custom(429, "RateLimit.Exceeded", "Too many requests");
    }

    /// <summary>
    ///     Demonstrates using Error.Custom() with [ProducesError] - still needed for custom status codes.
    /// </summary>
    [Post("/api/todos/{id:guid}/payment")]
    [ProducesError(402, "Payment.Required")]
    public static ErrorOr<Success> PaymentWithAnnotation([FromRoute] Guid id)
    {
        return Error.Custom(402, "Payment.Required", "Payment is required to continue");
    }
}