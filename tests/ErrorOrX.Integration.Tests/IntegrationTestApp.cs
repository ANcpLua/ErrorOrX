using System.Security.Claims;
using ErrorOr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ErrorOrX.Integration.Tests;

public sealed class IntegrationTestApp
{
    public static void Configure(WebApplication app)
    {
        app.MapErrorOrEndpoints();
        app.UseAuthentication();
        app.UseAuthorization();
    }

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(static o =>
            {
                o.LoginPath = "/login";
                o.AccessDeniedPath = "/denied";
                o.Events.OnRedirectToLogin = static context =>
                {
                    if (context.Request.Path.StartsWithSegments("/parity", StringComparison.Ordinal))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }

                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
            });
        services.AddAuthorization();

        services.AddErrorOrEndpoints();
    }
}

public static class TestEndpoints
{
    [Get("/parity/query-required")]
    public static ErrorOr<string> QueryRequired(string q) => q;

    [Post("/parity/body-json")]
    public static ErrorOr<string> BodyJson(TestDto dto) => dto.Name;

    [Get("/parity/query-int")]
    public static ErrorOr<int> QueryInt(int id) => id;

    [Post("/parity/form")]
    public static ErrorOr<string> FormValue([FromForm] string name) => name;

    [Get("/parity/return-t")]
    public static ErrorOr<string> ReturnT() => "success";

    [Post("/parity/return-t-post")]
    public static ErrorOr<string> ReturnTPost() => "success";

    [Post("/parity/created")]
    public static ErrorOr<Created> ReturnCreated() => Result.Created;

    [Get("/parity/auth/protected")]
    [Authorize]
    public static ErrorOr<string> Protected() => "secure";
}

public record TestDto(string Name);
