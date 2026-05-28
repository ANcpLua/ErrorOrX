# ASP.NET Core API Support — Research Notes

> Research material informing which extension methods ErrorOrX's source generator should emit for
> `IServiceCollection` and `IApplicationBuilder` / `WebApplication`. Goal: when a consumer imports
> ErrorOrX, the generator produces everything needed for a FastAPI-inspired, AOT-friendly API
> syntax — without forcing the consumer to hand-write BCL wrappers.
>
> Reference implementations of the target pattern:
> - [PaperlessREST/Host/Extensions/ServiceCollectionExtensions.cs](https://github.com/ANcpLua/Paperless/blob/main/PaperlessREST/Host/Extensions/ServiceCollectionExtensions.cs)
> - [PaperlessREST/Host/Extensions/OpenApiMetadataExtensions.cs](https://github.com/ANcpLua/Paperless/blob/main/PaperlessREST/Host/Extensions/OpenApiMetadataExtensions.cs)

## Two Lists, Same Surface

1. **Full list** — everything ASP.NET Core exposes on `WebApplication`, including legacy,
   compatibility, special-case, obsolete, and overload duplicates. Used as the universe to prune from.
2. **Support list for a .NET 10+ web library** — a curated modern subset that ErrorOrX targets.

There is no public Microsoft "raw usage ranking" for these extension methods. Any "X is preferred
over Y" claim is an inference unless backed by docs / templates / source.

What IS confirmed: `WebApplication` implements both `IApplicationBuilder` and `IEndpointRouteBuilder`,
so it naturally exposes both middleware-style `Use*` methods and endpoint-style `Map*` methods.
([Microsoft Learn][1])

## Pruning Rules

| Old / noisy / redundant API | Prefer in .NET 10+ | Reason |
|---|---|---|
| `UseEndpoints(...)` | Direct `app.Map*` calls | Microsoft says apps typically do not need `UseRouting`/`UseEndpoints` with `WebApplicationBuilder`; it wraps middleware added in `Program.cs`. [[2]](#refs) |
| `UseMvc(...)`, `UseMvcWithDefaultRoute()` | `MapControllers()`, `MapControllerRoute()`, `MapAreaControllerRoute()`, `MapRazorPages()` | Current MVC routing docs show endpoint-routing style mapping and call out `MapControllers`/`MapControllerRoute` as the controller setup path. [[2]](#refs) |
| `UseRouter(...)` | Endpoint routing / `Map*` | `UseRouter` is the older `IRouter` middleware path; endpoint routing is the default routing system in current docs. [[2]](#refs) |
| `UseConcurrencyLimiter()` | `AddRateLimiter(...)` + `UseRateLimiter()` | Microsoft marks Concurrency Limiter middleware deprecated and says to use rate-limiting middleware instead. [[3]](#refs) |
| `UseDatabaseErrorPage()` | EF `DatabaseDeveloperPageExceptionFilter` / modern dev exception flow | Microsoft marks it obsolete. [[4]](#refs) |
| `Use(IApplicationBuilder, Func<HttpContext, Func<Task>, Task>)` | `Use(IApplicationBuilder, Func<HttpContext, RequestDelegate, Task>)` | Microsoft explicitly says to prefer the `RequestDelegate` overload for better performance. [[1]](#refs) |
| Generic `MapMethods(...)` for normal verbs | `MapGet`, `MapPost`, `MapPut`, `MapPatch`, `MapDelete` | `MapMethods` is still useful for uncommon / custom verb sets; normal HTTP verbs should use the typed helpers. Microsoft lists the typed helpers directly on `WebApplication`. [[1]](#refs) |
| Static-file kitchen-sink APIs by default | `MapStaticAssets()` plus `UseStaticFiles()` only when needed | .NET 10 static-file docs show `UseStaticFiles` for excluded/uncovered files and `MapStaticAssets` for build-known static assets. [[5]](#refs) |

## Recommended .NET 10+ App-Level Subset

This is the subset to support in a clean web library. Removes overload duplication, obsolete methods,
legacy routing, and niche app-type APIs unless a feature module explicitly needs them.

```csharp
// Core pipeline
Use(Func<HttpContext, RequestDelegate, Task>)
Run(RequestDelegate)
UseMiddleware<TMiddleware>()
Map(path, branch)
MapWhen(predicate, branch)
UseWhen(predicate, branch)

// Modern endpoint mapping
Map(...)
MapGet(...)
MapPost(...)
MapPut(...)
MapPatch(...)
MapDelete(...)
MapMethods(...)            // keep only for custom/multiple verbs
MapGroup(...)
MapFallback(...)
MapShortCircuit(...)

// Static assets
MapStaticAssets(...)
UseStaticFiles(...)

// API / OpenAPI
MapOpenApi(...)

// Controllers / Razor / UI — only if the library supports these app types
MapControllers(...)
MapControllerRoute(...)
MapAreaControllerRoute(...)
MapDefaultControllerRoute(...)
MapRazorPages(...)
MapRazorComponents<TRootComponent>(...)

// Realtime / identity — only if explicitly supported
MapHub<THub>(...)
MapBlazorHub(...)
MapIdentityApi<TUser>()

// Health
MapHealthChecks(...)

// Common production middleware
UseExceptionHandler(...)
UseDeveloperExceptionPage(...)
UseHsts(...)
UseHttpsRedirection(...)
UseForwardedHeaders(...)
UsePathBase(...)
UseRouting(...)            // allowed but not default unless ordering is required
UseCors(...)
UseAuthentication(...)
UseAuthorization(...)
UseAntiforgery(...)
UseRateLimiter(...)
UseRequestTimeouts(...)
UseOutputCache(...)
UseResponseCompression(...)
UseRequestDecompression(...)
UseRequestLocalization(...)
UseCookiePolicy(...)
UseSession(...)
UseWebSockets(...)
UseStatusCodePages(...)
```

## Excluded from the Subset (Still in API Surface)

These still exist in the API surface but should not be exposed by a clean modern web library.

```csharp
// Obsolete / deprecated
UseConcurrencyLimiter(...)
UseDatabaseErrorPage(...)

// Legacy routing / MVC compatibility
UseMvc(...)
UseMvcWithDefaultRoute(...)
UseRouter(...)
UseEndpoints(...)

// Static-file convenience or risky/special static scenarios
UseDefaultFiles(...)
UseDirectoryBrowser(...)
UseFileServer(...)
UseWelcomePage(...)

// SPA / Blazor WASM hosting-specific
UseSpa(...)
UseSpaStaticFiles(...)
UseBlazorFrameworkFiles(...)
UseWebAssemblyDebugging(...)

// Compatibility / infrastructure-specific
UseOwin(...)
UseHttpMethodOverride(...)
UseCertificateForwarding(...)
UseHostFiltering(...)
UseHeaderPropagation(...)
UseRequestCheckpoint(...)
UseRequestLatencyTelemetry(...)
UseW3CLogging(...)
UseHttpLoggingMiddleware(...)   // prefer UseHttpLogging unless you specifically need this package path

// Specialized dynamic routing
MapDynamicControllerRoute<TTransformer>(...)
MapDynamicPageRoute<TTransformer>(...)
MapFallbackToController(...)
MapFallbackToAreaController(...)
MapFallbackToPage(...)
MapFallbackToAreaPage(...)
MapFallbackToFile(...)

// Low-level SignalR/connection plumbing
MapConnectionHandler<TConnectionHandler>(...)
MapConnections(...)
```

## Rate Limiting — Modern Pattern

The modern .NET 10+ subset for rate limiting:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", limiter =>
    {
        limiter.PermitLimit = 100;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 0;
    });
});

app.UseRateLimiter();

app.MapGet("/example", () => "ok")
    .RequireRateLimiting("fixed");
```

`UseRateLimiter` is the current middleware. Endpoint-specific rate limiting requires correct
routing order: Microsoft states `UseRateLimiter` must come **after** `UseRouting` when
endpoint-specific APIs such as attributes or `.RequireRateLimiting(...)` are used. [[6]](#refs)

**ErrorOrX should expose:** `AddRateLimiter`, `UseRateLimiter`, `.RequireRateLimiting`,
`.DisableRateLimiting`, and the `System.Threading.RateLimiting` limiter option types.
**Do NOT expose / support:** `UseConcurrencyLimiter`.

## Complete `WebApplication` Extension-Method List (.NET 10)

Source: Microsoft Learn `WebApplication` → Extension Methods section. List runs from `Map(...)` through `UseWhen(...)`. [[1]](#refs)

### Map / Endpoint Routing

```csharp
Map(IApplicationBuilder, PathString, Action<IApplicationBuilder>)
Map(IApplicationBuilder, PathString, Boolean, Action<IApplicationBuilder>)
Map(IApplicationBuilder, String, Action<IApplicationBuilder>)

Map(IEndpointRouteBuilder, RoutePattern, Delegate)
Map(IEndpointRouteBuilder, RoutePattern, RequestDelegate)
Map(IEndpointRouteBuilder, String, Delegate)
Map(IEndpointRouteBuilder, String, RequestDelegate)

MapAreaControllerRoute(IEndpointRouteBuilder, String, String, String, Object, Object, Object)

MapBlazorHub(IEndpointRouteBuilder, Action<HttpConnectionDispatcherOptions>)
MapBlazorHub(IEndpointRouteBuilder, String, Action<HttpConnectionDispatcherOptions>)
MapBlazorHub(IEndpointRouteBuilder, String)
MapBlazorHub(IEndpointRouteBuilder)

MapConnectionHandler<TConnectionHandler>(IEndpointRouteBuilder, String, Action<HttpConnectionDispatcherOptions>)
MapConnectionHandler<TConnectionHandler>(IEndpointRouteBuilder, String)

MapConnections(IEndpointRouteBuilder, String, Action<IConnectionBuilder>)
MapConnections(IEndpointRouteBuilder, String, HttpConnectionDispatcherOptions, Action<IConnectionBuilder>)

MapControllerRoute(IEndpointRouteBuilder, String, String, Object, Object, Object)
MapControllers(IEndpointRouteBuilder)
MapDefaultControllerRoute(IEndpointRouteBuilder)

MapDelete(IEndpointRouteBuilder, String, Delegate)
MapDelete(IEndpointRouteBuilder, String, RequestDelegate)

MapDynamicControllerRoute<TTransformer>(IEndpointRouteBuilder, String, Object, Int32)
MapDynamicControllerRoute<TTransformer>(IEndpointRouteBuilder, String, Object)
MapDynamicControllerRoute<TTransformer>(IEndpointRouteBuilder, String)

MapDynamicPageRoute<TTransformer>(IEndpointRouteBuilder, String, Object, Int32)
MapDynamicPageRoute<TTransformer>(IEndpointRouteBuilder, String, Object)
MapDynamicPageRoute<TTransformer>(IEndpointRouteBuilder, String)

MapFallback(IEndpointRouteBuilder, Delegate)
MapFallback(IEndpointRouteBuilder, RequestDelegate)
MapFallback(IEndpointRouteBuilder, String, Delegate)
MapFallback(IEndpointRouteBuilder, String, RequestDelegate)

MapFallbackToAreaController(IEndpointRouteBuilder, String, String, String, String)
MapFallbackToAreaController(IEndpointRouteBuilder, String, String, String)

MapFallbackToAreaPage(IEndpointRouteBuilder, String, String, String)
MapFallbackToAreaPage(IEndpointRouteBuilder, String, String)

MapFallbackToController(IEndpointRouteBuilder, String, String, String)
MapFallbackToController(IEndpointRouteBuilder, String, String)

MapFallbackToFile(IEndpointRouteBuilder, String, StaticFileOptions)
MapFallbackToFile(IEndpointRouteBuilder, String, String, StaticFileOptions)
MapFallbackToFile(IEndpointRouteBuilder, String, String)
MapFallbackToFile(IEndpointRouteBuilder, String)

MapFallbackToPage(IEndpointRouteBuilder, String, String)
MapFallbackToPage(IEndpointRouteBuilder, String)

MapGet(IEndpointRouteBuilder, String, Delegate)
MapGet(IEndpointRouteBuilder, String, RequestDelegate)

MapGroup(IEndpointRouteBuilder, RoutePattern)
MapGroup(IEndpointRouteBuilder, String)

MapHealthChecks(IEndpointRouteBuilder, String, HealthCheckOptions)
MapHealthChecks(IEndpointRouteBuilder, String)

MapHub<THub>(IEndpointRouteBuilder, String, Action<HttpConnectionDispatcherOptions>)
MapHub<THub>(IEndpointRouteBuilder, String)

MapIdentityApi<TUser>(IEndpointRouteBuilder)

MapMethods(IEndpointRouteBuilder, String, IEnumerable<String>, Delegate)
MapMethods(IEndpointRouteBuilder, String, IEnumerable<String>, RequestDelegate)

MapOpenApi(IEndpointRouteBuilder, String)

MapPatch(IEndpointRouteBuilder, String, Delegate)
MapPatch(IEndpointRouteBuilder, String, RequestDelegate)

MapPost(IEndpointRouteBuilder, String, Delegate)
MapPost(IEndpointRouteBuilder, String, RequestDelegate)

MapPut(IEndpointRouteBuilder, String, Delegate)
MapPut(IEndpointRouteBuilder, String, RequestDelegate)

MapRazorComponents<TRootComponent>(IEndpointRouteBuilder)
MapRazorPages(IEndpointRouteBuilder)

MapShortCircuit(IEndpointRouteBuilder, Int32, String[])
MapStaticAssets(IEndpointRouteBuilder, String)

MapWhen(IApplicationBuilder, Func<HttpContext, Boolean>, Action<IApplicationBuilder>)
```

### Terminal / Raw Middleware

```csharp
Run(IApplicationBuilder, RequestDelegate)

Use(IApplicationBuilder, Func<HttpContext, Func<Task>, Task>)
Use(IApplicationBuilder, Func<HttpContext, RequestDelegate, Task>)
```

### `Use*` Middleware

```csharp
UseAntiforgery(IApplicationBuilder)
UseAuthentication(IApplicationBuilder)
UseAuthorization(IApplicationBuilder)

UseBlazorFrameworkFiles(IApplicationBuilder, PathString)
UseBlazorFrameworkFiles(IApplicationBuilder)

UseCertificateForwarding(IApplicationBuilder)

UseConcurrencyLimiter(IApplicationBuilder)                        // obsolete

UseCookiePolicy(IApplicationBuilder, CookiePolicyOptions)
UseCookiePolicy(IApplicationBuilder)

UseCors(IApplicationBuilder, Action<CorsPolicyBuilder>)
UseCors(IApplicationBuilder, String)
UseCors(IApplicationBuilder)

UseDatabaseErrorPage(IApplicationBuilder, DatabaseErrorPageOptions)  // obsolete
UseDatabaseErrorPage(IApplicationBuilder)                            // obsolete

UseDefaultFiles(IApplicationBuilder, DefaultFilesOptions)
UseDefaultFiles(IApplicationBuilder, String)
UseDefaultFiles(IApplicationBuilder)

UseDeveloperExceptionPage(IApplicationBuilder, DeveloperExceptionPageOptions)
UseDeveloperExceptionPage(IApplicationBuilder)

UseDirectoryBrowser(IApplicationBuilder, DirectoryBrowserOptions)
UseDirectoryBrowser(IApplicationBuilder, String)
UseDirectoryBrowser(IApplicationBuilder)

UseEndpoints(IApplicationBuilder, Action<IEndpointRouteBuilder>)

UseExceptionHandler(IApplicationBuilder, Action<IApplicationBuilder>)
UseExceptionHandler(IApplicationBuilder, ExceptionHandlerOptions)
UseExceptionHandler(IApplicationBuilder, String, Boolean)
UseExceptionHandler(IApplicationBuilder, String)
UseExceptionHandler(IApplicationBuilder)

UseFileServer(IApplicationBuilder, Boolean)
UseFileServer(IApplicationBuilder, FileServerOptions)
UseFileServer(IApplicationBuilder, String)
UseFileServer(IApplicationBuilder)

UseForwardedHeaders(IApplicationBuilder, ForwardedHeadersOptions)
UseForwardedHeaders(IApplicationBuilder)

UseHeaderPropagation(IApplicationBuilder)

UseHealthChecks(IApplicationBuilder, PathString, HealthCheckOptions)
UseHealthChecks(IApplicationBuilder, PathString, Int32, HealthCheckOptions)
UseHealthChecks(IApplicationBuilder, PathString, Int32)
UseHealthChecks(IApplicationBuilder, PathString, String, HealthCheckOptions)
UseHealthChecks(IApplicationBuilder, PathString, String)
UseHealthChecks(IApplicationBuilder, PathString)

UseHostFiltering(IApplicationBuilder)
UseHsts(IApplicationBuilder)

UseHttpLogging(IApplicationBuilder)
UseHttpLoggingMiddleware(IApplicationBuilder)

UseHttpMethodOverride(IApplicationBuilder, HttpMethodOverrideOptions)
UseHttpMethodOverride(IApplicationBuilder)

UseHttpsRedirection(IApplicationBuilder)

UseMiddleware(IApplicationBuilder, Type, Object[])
UseMiddleware<TMiddleware>(IApplicationBuilder, Object[])

UseMigrationsEndPoint(IApplicationBuilder, MigrationsEndPointOptions)
UseMigrationsEndPoint(IApplicationBuilder)

UseMvc(IApplicationBuilder, Action<IRouteBuilder>)
UseMvc(IApplicationBuilder)
UseMvcWithDefaultRoute(IApplicationBuilder)

UseOutputCache(IApplicationBuilder)

UseOwin(IApplicationBuilder, Action<Action<Func<Func<IDictionary<String, Object>, Task>, Func<IDictionary<String, Object>, Task>>>>)
UseOwin(IApplicationBuilder)

UsePathBase(IApplicationBuilder, PathString)

UseRateLimiter(IApplicationBuilder, RateLimiterOptions)
UseRateLimiter(IApplicationBuilder)

UseRequestCheckpoint(IApplicationBuilder)                         // listed twice by docs
UseRequestDecompression(IApplicationBuilder)

UseRequestLatencyTelemetry(IApplicationBuilder)                   // listed twice by docs

UseRequestLocalization(IApplicationBuilder, Action<RequestLocalizationOptions>)
UseRequestLocalization(IApplicationBuilder, RequestLocalizationOptions)
UseRequestLocalization(IApplicationBuilder, String[])
UseRequestLocalization(IApplicationBuilder)

UseRequestTimeouts(IApplicationBuilder)

UseResponseCaching(IApplicationBuilder)
UseResponseCompression(IApplicationBuilder)

UseRewriter(IApplicationBuilder, RewriteOptions)
UseRewriter(IApplicationBuilder)

UseRouter(IApplicationBuilder, Action<IRouteBuilder>)
UseRouter(IApplicationBuilder, IRouter)

UseRouting(IApplicationBuilder)

UseSession(IApplicationBuilder, SessionOptions)
UseSession(IApplicationBuilder)

UseSpa(IApplicationBuilder, Action<ISpaBuilder>)

UseSpaStaticFiles(IApplicationBuilder, StaticFileOptions)
UseSpaStaticFiles(IApplicationBuilder)

UseStaticFiles(IApplicationBuilder, StaticFileOptions)
UseStaticFiles(IApplicationBuilder, String)
UseStaticFiles(IApplicationBuilder)

UseStatusCodePages(IApplicationBuilder, Action<IApplicationBuilder>)
UseStatusCodePages(IApplicationBuilder, Func<StatusCodeContext, Task>)
UseStatusCodePages(IApplicationBuilder, StatusCodePagesOptions)
UseStatusCodePages(IApplicationBuilder, String, String)
UseStatusCodePages(IApplicationBuilder)

UseStatusCodePagesWithRedirects(IApplicationBuilder, String)

UseStatusCodePagesWithReExecute(IApplicationBuilder, String, String, Boolean)
UseStatusCodePagesWithReExecute(IApplicationBuilder, String, String)

UseW3CLogging(IApplicationBuilder)

UseWebAssemblyDebugging(IApplicationBuilder)

UseWebSockets(IApplicationBuilder, WebSocketOptions)
UseWebSockets(IApplicationBuilder)

UseWelcomePage(IApplicationBuilder, PathString)
UseWelcomePage(IApplicationBuilder, String)
UseWelcomePage(IApplicationBuilder, WelcomePageOptions)
UseWelcomePage(IApplicationBuilder)

UseWhen(IApplicationBuilder, Func<HttpContext, Boolean>, Action<IApplicationBuilder>)
```

## Extra `Microsoft.AspNetCore.Builder` Extensions

These are NOT visible as `app.*` methods because they extend `IServiceCollection`, but they are
still declared in the `Microsoft.AspNetCore.Builder` namespace. Microsoft documents `AddRateLimiter`
in assembly `Microsoft.AspNetCore.RateLimiting.dll` with these overloads: [[2]](#refs)

```csharp
AddRateLimiter(IServiceCollection)
AddRateLimiter(IServiceCollection, Action<RateLimiterOptions>)
```

## References {#refs}

1. Microsoft Learn — `WebApplication` class (extension methods listing)
2. Microsoft Learn — ASP.NET Core routing & MVC
3. Microsoft Learn — Concurrency Limiter middleware (deprecated)
4. Microsoft Learn — Database Error Page middleware (obsolete)
5. Microsoft Learn — Static files in ASP.NET Core (.NET 10)
6. Microsoft Learn — Rate limiting middleware
