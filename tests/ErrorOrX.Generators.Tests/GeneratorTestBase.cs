using ANcpLua.Roslyn.Utilities.Testing;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ErrorOrX.Generators.Tests;

public abstract class GeneratorTestBase
{
    private static readonly Type[] RequiredTypes =
    [
        typeof(HttpContext),
        typeof(TypedResults),
        typeof(FromBodyAttribute),
        typeof(IEndpointNameMetadata),
        typeof(IServiceCollection),
        typeof(OpenApiServiceCollectionExtensions),
        typeof(Error),
        // Middleware attributes for auth/rate-limiting/cors/caching tests
        typeof(AuthorizeAttribute),
        typeof(AllowAnonymousAttribute),
        typeof(EnableRateLimitingAttribute),
        typeof(DisableRateLimitingAttribute),
        typeof(OutputCacheAttribute),
        typeof(EnableCorsAttribute),
        typeof(DisableCorsAttribute),
        // API versioning attributes
        typeof(ApiVersionAttribute),
        typeof(ApiVersionNeutralAttribute),
        typeof(MapToApiVersionAttribute)
    ];

    /// <summary>
    ///     Runs the ErrorOrEndpointGenerator on the provided source and returns the result
    ///     for fluent assertions (IsClean, Compiles, IsCached, etc.).
    /// </summary>
    protected static async Task<GeneratorResult> RunAsync(string source)
    {
        using var scope = TestConfiguration.WithAdditionalReferences(RequiredTypes);
        return await Test<ErrorOrEndpointGenerator>.Run(source, TestContext.Current.CancellationToken);
    }

    /// <summary>
    ///     Runs the generator and verifies output using Verify snapshots.
    /// </summary>
    protected static async Task VerifyAsync(string source)
    {
        using var result = await RunAsync(source);

        await Verify(new
        {
            GeneratedSources = result.Files
                .Select(static f => new { f.HintName, Source = f.Content })
                .OrderBy(static s => s.HintName),
            Diagnostics = result.Diagnostics
                .Select(static d => new { d.Id, Severity = d.Severity.ToString(), Message = d.GetMessage() })
                .OrderBy(static d => d.Id)
        }).UseDirectory("Snapshots");
    }
}
