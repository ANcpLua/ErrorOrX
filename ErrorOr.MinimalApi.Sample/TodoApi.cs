using ErrorOr.Http.Bcl.Sample.Domain;
using Microsoft.AspNetCore.Mvc;

namespace ErrorOr.Http.Bcl.Sample;

public static class TodoApi
{
    [Get("/api/todos")]
    public static Task<ErrorOr<List<Todo>>> GetAll(
        [FromServices] ITodoService svc,
        CancellationToken ct)
    {
        return svc.GetAllAsync(ct);
    }

    [Get("/api/todos/{id:guid}")]
    public static Task<ErrorOr<Todo>> GetById(
        [FromRoute] Guid id,
        [FromServices] ITodoService svc,
        CancellationToken ct)
    {
        return svc.GetByIdAsync(id, ct);
    }

    [Post("/api/todos")]
    public static Task<ErrorOr<Todo>> Create(
        [FromBody] CreateTodoRequest request,
        [FromServices] ITodoService svc,
        CancellationToken ct)
    {
        return svc.CreateAsync(request, ct);
    }

    [Put("/api/todos/{id:guid}")]
    public static Task<ErrorOr<Updated>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateTodoRequest request,
        [FromServices] ITodoService svc,
        CancellationToken ct)
    {
        return svc.UpdateAsync(id, request, ct);
    }

    [Delete("/api/todos/{id:guid}")]
    public static Task<ErrorOr<Deleted>> Delete(
        [FromRoute] Guid id,
        [FromServices] ITodoService svc,
        CancellationToken ct)
    {
        return svc.DeleteAsync(id, ct);
    }

    /// <summary>
    /// Demonstrates using Error.Custom() with [ProducesError] - no warning.
    /// </summary>
    [Post("/api/todos/{id:guid}/rate-limit")]
    [ProducesError(429, "RateLimit.Exceeded")]
    public static ErrorOr<Success> RateLimitWithAnnotation([FromRoute] Guid id)
    {
        // No EOE026 warning because [ProducesError] documents this custom error
        return Error.Custom(429, "RateLimit.Exceeded", "Too many requests");
    }

    /// <summary>
    /// Demonstrates using Error.Custom() with [ProducesError] - no warning expected.
    /// </summary>
    [Post("/api/todos/{id:guid}/payment")]
    [ProducesError(402, "Payment.Required")]
    public static ErrorOr<Success> PaymentWithAnnotation([FromRoute] Guid id)
    {
        // This should NOT trigger EOE026 warning because it's documented with [ProducesError]
        return Error.Custom(402, "Payment.Required", "Payment is required to continue");
    }
}