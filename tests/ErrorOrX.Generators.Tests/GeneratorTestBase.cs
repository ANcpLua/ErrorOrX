using ANcpLua.Roslyn.Utilities.Testing;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;

namespace ErrorOrX.Generators.Tests;

public abstract class GeneratorTestBase
{
    private const string AttributesHintName = "ErrorOrEndpointAttributes.Mappings.g.cs";

    private static readonly Type[] s_requiredTypes =
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
        typeof(MapToApiVersionAttribute),
        // DataAnnotations — pulls System.ComponentModel.Annotations.dll into the test compilation
        // so [Required]/[StringLength]/[Range] resolve to real symbols with proper BaseType chains.
        // Without this, EOE034's IsOrInheritsFrom walk has nothing to traverse and silently no-ops.
        typeof(RequiredAttribute)
    ];

    /// <summary>
    ///     Types to include when running without API versioning support.
    ///     Used to test EOE029 (package not referenced).
    /// </summary>
    private static readonly Type[] s_requiredTypesWithoutVersioning =
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
        using var scope = TestConfiguration.WithAdditionalReferences(s_requiredTypes);
        return await Test<ErrorOrEndpointGenerator>.Run(source, TestContext.Current.CancellationToken);
    }

    /// <summary>
    ///     Runs the generator without API versioning package references.
    ///     Used to test EOE029 (versioning package not referenced).
    /// </summary>
    protected static async Task<GeneratorResult> RunWithoutVersioningAsync(string source)
    {
        using var scope = TestConfiguration.WithAdditionalReferences(s_requiredTypesWithoutVersioning);
        return await Test<ErrorOrEndpointGenerator>.Run(source, TestContext.Current.CancellationToken);
    }

    /// <summary>
    ///     Runs the generator and verifies output using Verify snapshots.
    ///     Excludes the shared attributes file (tested once in GeneratorCachingTests).
    ///     Also asserts the generated code COMPILES when the generator didn't report any error-severity
    ///     diagnostics — catches "snapshot matches but emit is syntactically invalid" bugs that would
    ///     otherwise slip past every test in this suite (previously only GeneratorCachingTests called
    ///     <c>.Compiles()</c>; the other ~150 snapshot tests only string-matched the recorded output).
    ///     Tests that exercise diagnostic-error paths intentionally emit invalid sources, so the
    ///     compile-check is gated on "no error diagnostics" — those tests prove the diagnostic instead.
    /// </summary>
    protected static async Task VerifyAsync(string source)
    {
        using var result = await RunAsync(source);

        await Verify(new
        {
            GeneratedSources = result.Files
                .Where(static f => f.HintName != AttributesHintName)
                .Select(static f => new { f.HintName, Source = f.Content })
                .OrderBy(static s => s.HintName),
            Diagnostics = result.Diagnostics
                .Select(static d => new { d.Id, Severity = d.Severity.ToString(), Message = d.GetMessage() })
                .OrderBy(static d => d.Id)
        }).UseDirectory("Snapshots");

        if (!result.Diagnostics.Any(static d => d.Severity == DiagnosticSeverity.Error))
            result.Compiles();
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
                .Where(static f => f.HintName != AttributesHintName)
                .Select(static f => new { f.HintName, Source = f.Content })
                .OrderBy(static s => s.HintName),
            Diagnostics = result.Diagnostics
                .Select(static d => new { d.Id, Severity = d.Severity.ToString(), Message = d.GetMessage() })
                .OrderBy(static d => d.Id)
        }).UseDirectory("Snapshots");
    }
}
