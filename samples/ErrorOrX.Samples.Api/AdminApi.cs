using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace ErrorOrX.Samples.Api;

/// <summary>
///     Demonstrates ASP.NET Core middleware attributes that the generator translates
///     into fluent Minimal API calls:
///     <list type="bullet">
///         <item><c>[Authorize]</c>           → <c>.RequireAuthorization(...)</c></item>
///         <item><c>[EnableRateLimiting]</c>  → <c>.RequireRateLimiting(...)</c></item>
///         <item><c>[OutputCache]</c>         → <c>.CacheOutput(p => p.Expire(...))</c></item>
///         <item><c>[EnableCors]</c>          → <c>.RequireCors(...)</c></item>
///     </list>
///     Required services are wired in <c>Program.cs</c>. No authentication scheme is
///     registered in the sample, so <c>[Authorize]</c> endpoints return 401 at runtime —
///     wire your own scheme to actually exercise them.
/// </summary>
public static class AdminApi
{
    [Get("/api/admin/todos")]
    [Authorize("Admin")]
    public static Task<ErrorOr<List<Todo>>> AdminListAll(ITodoService svc, CancellationToken ct)
        => svc.GetAllAsync(ct);

    [Post("/api/admin/todos/{id:guid}/promote")]
    [Authorize("Admin")]
    [EnableRateLimiting("fixed")]
    public static Task<ErrorOr<Todo>> Promote(Guid id, ITodoService svc, CancellationToken ct)
        => svc.GetByIdAsync(id, ct);

    [Get("/api/admin/todos/report")]
    [Authorize("Admin")]
    [OutputCache(Duration = 60)]
    public static Task<ErrorOr<List<Todo>>> ExpensiveReport(ITodoService svc, CancellationToken ct)
        => svc.GetAllAsync(ct);

    [Get("/api/public/health")]
    [EnableCors("Public")]
    public static ErrorOr<string> Health() => "ok";
}
