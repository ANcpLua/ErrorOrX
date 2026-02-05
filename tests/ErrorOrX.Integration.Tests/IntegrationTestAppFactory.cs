using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace ErrorOrX.Integration.Tests;

public sealed class IntegrationTestAppFactory : WebApplicationFactory<IntegrationTestApp>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var appBuilder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });

        appBuilder.WebHost.UseTestServer();
        appBuilder.Services.AddHealthChecks();
        IntegrationTestApp.ConfigureServices(appBuilder.Services);

        var app = appBuilder.Build();
        app.MapHealthChecks("/health");
        IntegrationTestApp.Configure(app);
        app.Start();

        return app;
    }
}
