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
    ///     Types to include when running without API versioning support.
    ///     Used to test EOE029 (package not referenced).
    /// </summary>
    private static readonly Type[] RequiredTypesWithoutVersioning =
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
        typeof(DisableCorsAttribute)
        // NOTE: API versioning types intentionally excluded to test EOE029
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
    ///     Runs the generator without API versioning package references.
    ///     Used to test EOE029 (versioning package not referenced).
    /// </summary>
    protected static async Task<GeneratorResult> RunWithoutVersioningAsync(string source)
    {
        using var scope = TestConfiguration.WithAdditionalReferences(RequiredTypesWithoutVersioning);
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

    /// <summary>
    ///     Runs the generator without API versioning references and verifies output.
    ///     Used to test EOE029 (versioning package not referenced).
    /// </summary>
    protected static async Task VerifyWithoutVersioningAsync(string source)
    {
        using var result = await RunWithoutVersioningAsync(source);

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
