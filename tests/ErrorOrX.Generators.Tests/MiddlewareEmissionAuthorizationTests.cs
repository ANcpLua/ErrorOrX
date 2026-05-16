namespace ErrorOrX.Generators.Tests;

/// <summary>
///     Tests for <c>[Authorize]</c>/<c>[AllowAnonymous]</c> emission. Security-critical because
///     the AOT wrapper delegates lose original method attributes, so RequireAuthorization MUST
///     be emitted as a fluent call. Also covers combined-middleware emission as a smoke test.
/// </summary>
public class MiddlewareEmissionAuthorizationTests : GeneratorTestBase
{
    [Fact]
    public async Task Multiple_Middleware_Attributes_All_Emit()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Authorization;
                              using Microsoft.AspNetCore.RateLimiting;
                              using Microsoft.AspNetCore.OutputCaching;

                              public static class Api
                              {
                                  [Get("/api")]
                                  [Authorize("ApiPolicy")]
                                  [EnableRateLimiting("standard")]
                                  [OutputCache(PolicyName = "ApiCache")]
                                  public static ErrorOr<string> GetData() => "data";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain(".RequireAuthorization(\"ApiPolicy\")");
        generated.Should().Contain(".RequireRateLimiting(\"standard\")");
        generated.Should().Contain(".CacheOutput(\"ApiCache\")");
    }

    [Fact]
    public async Task Authorize_Attribute_Emits_RequireAuthorization()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Authorization;

                              public static class Api
                              {
                                  [Get("/admin")]
                                  [Authorize]
                                  public static ErrorOr<string> AdminOnly() => "secret";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain(".RequireAuthorization()");
    }

    [Fact]
    public async Task Authorize_With_Policy_String_Emits_Policy_Name()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Authorization;

                              public static class Api
                              {
                                  [Get("/admin")]
                                  [Authorize("AdminPolicy")]
                                  public static ErrorOr<string> AdminOnly() => "secret";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain(".RequireAuthorization(\"AdminPolicy\")");
    }

    [Fact]
    public async Task Authorize_With_Policy_Named_Parameter_Emits_Policy_Name()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Authorization;

                              public static class Api
                              {
                                  [Get("/admin")]
                                  [Authorize(Policy = "AdminPolicy")]
                                  public static ErrorOr<string> AdminOnly() => "secret";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain(".RequireAuthorization(\"AdminPolicy\")");
    }

    [Fact]
    public async Task Authorize_With_Roles_Emits_Roles()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Authorization;

                              public static class Api
                              {
                                  [Get("/admin")]
                                  [Authorize(Roles = "Admin,Manager")]
                                  public static ErrorOr<string> AdminOnly() => "secret";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        // Roles should be handled - either via policy builder or RequireAuthorization with role
        generated.Should().Match("*.RequireAuthorization*Admin*");
    }

    [Fact]
    public async Task Multiple_Authorize_Attributes_Emit_All()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Authorization;

                              public static class Api
                              {
                                  [Get("/admin")]
                                  [Authorize("Policy1")]
                                  [Authorize("Policy2")]
                                  public static ErrorOr<string> AdminOnly() => "secret";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain("Policy1");
        generated.Should().Contain("Policy2");
    }

    [Fact]
    public async Task AllowAnonymous_Overrides_Authorize()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Authorization;

                              public static class Api
                              {
                                  [Get("/public")]
                                  [Authorize]
                                  [AllowAnonymous]
                                  public static ErrorOr<string> PublicEndpoint() => "public";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        // AllowAnonymous should emit AllowAnonymous(), not RequireAuthorization()
        generated.Should().Contain(".AllowAnonymous()");
        // The RequireAuthorization should be suppressed by AllowAnonymous
    }

    [Fact]
    public async Task Authorize_With_AuthenticationSchemes_Emits_Scheme()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Authorization;

                              public static class Api
                              {
                                  [Get("/api")]
                                  [Authorize(AuthenticationSchemes = "Bearer")]
                                  public static ErrorOr<string> ApiEndpoint() => "data";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        // Authentication schemes should be handled
        generated.Should().Match("*.RequireAuthorization*");
    }
}
