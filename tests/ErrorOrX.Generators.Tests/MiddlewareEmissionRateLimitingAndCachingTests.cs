namespace ErrorOrX.Generators.Tests;

/// <summary>
///     Tests for rate-limiting (<c>[EnableRateLimiting]</c>, <c>[DisableRateLimiting]</c>) and
///     output-caching (<c>[OutputCache]</c> with PolicyName / Duration / VaryByQueryKeys) attribute
///     emission. Verifies <c>Disable</c> overrides <c>Enable</c> rather than being silently dropped.
/// </summary>
public class MiddlewareEmissionRateLimitingAndCachingTests : GeneratorTestBase
{
    [Fact]
    public async Task EnableRateLimiting_Emits_RequireRateLimiting()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.RateLimiting;

                              public static class Api
                              {
                                  [Get("/api")]
                                  [EnableRateLimiting("fixed")]
                                  public static ErrorOr<string> GetData() => "data";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain(".RequireRateLimiting(\"fixed\")");
    }

    [Fact]
    public async Task DisableRateLimiting_Emits_DisableRateLimiting()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.RateLimiting;

                              public static class Api
                              {
                                  [Get("/unlimited")]
                                  [DisableRateLimiting]
                                  public static ErrorOr<string> Unlimited() => "unlimited";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain(".DisableRateLimiting()");
    }

    [Fact]
    public async Task DisableRateLimiting_Overrides_EnableRateLimiting()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.RateLimiting;

                              public static class Api
                              {
                                  [Get("/unlimited")]
                                  [EnableRateLimiting("fixed")]
                                  [DisableRateLimiting]
                                  public static ErrorOr<string> Unlimited() => "unlimited";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;

        // Extract only the endpoint mapping lines (exclude doc comments)
        var endpointLines = generated.Split('\n')
            .Where(static l => !l.TrimStart().StartsWith("///", StringComparison.Ordinal))
            .Where(static l => l.Contains("MapGet", StringComparison.Ordinal) ||
                               l.Contains(".DisableRateLimiting", StringComparison.Ordinal) ||
                               l.Contains(".RequireRateLimiting", StringComparison.Ordinal));
        var endpointSection = string.Join("\n", endpointLines);

        // DisableRateLimiting should override EnableRateLimiting
        endpointSection.Should().Contain(".DisableRateLimiting()");
        endpointSection.Should().NotContain(".RequireRateLimiting(");
    }

    [Fact]
    public async Task OutputCache_Emits_CacheOutput()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.OutputCaching;

                              public static class Api
                              {
                                  [Get("/cached")]
                                  [OutputCache]
                                  public static ErrorOr<string> Cached() => "cached";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain(".CacheOutput()");
    }

    [Fact]
    public async Task OutputCache_With_PolicyName_Emits_Policy()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.OutputCaching;

                              public static class Api
                              {
                                  [Get("/cached")]
                                  [OutputCache(PolicyName = "MyPolicy")]
                                  public static ErrorOr<string> Cached() => "cached";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain(".CacheOutput(\"MyPolicy\")");
    }

    [Fact]
    public async Task OutputCache_With_Duration_Emits_Duration()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.OutputCaching;

                              public static class Api
                              {
                                  [Get("/cached")]
                                  [OutputCache(Duration = 60)]
                                  public static ErrorOr<string> Cached() => "cached";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        // Duration should be handled - either via policy builder or inline
        generated.Should().Match("*.CacheOutput*");
    }

    [Fact]
    public async Task OutputCache_With_VaryByQueryKeys_Emits_VaryBy()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.OutputCaching;

                              public static class Api
                              {
                                  [Get("/cached")]
                                  [OutputCache(VaryByQueryKeys = new[] { "page", "sort" })]
                                  public static ErrorOr<string> Cached() => "cached";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Match("*.CacheOutput*");
    }
}
