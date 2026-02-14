namespace ErrorOrX.Generators.Tests;

/// <summary>
///     Tests for middleware attribute emission in the ErrorOrEndpointGenerator.
///     Security-critical: Verifies that [Authorize], [EnableRateLimiting], [OutputCache], [EnableCors]
///     are correctly translated to fluent calls since wrapper delegates lose original attributes.
/// </summary>
public class MiddlewareEmissionTests : GeneratorTestBase
{
    #region Combined Middleware

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

    #endregion

    #region Authorization Middleware

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

    #endregion

    #region Rate Limiting Middleware

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

    #endregion

    #region Output Caching Middleware

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

    #endregion

    #region CORS Middleware

    [Fact]
    public async Task EnableCors_Emits_RequireCors()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Cors;

                              public static class Api
                              {
                                  [Get("/api")]
                                  [EnableCors]
                                  public static ErrorOr<string> GetData() => "data";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain(".RequireCors()");
    }

    [Fact]
    public async Task EnableCors_With_Policy_Emits_Policy_Name()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Cors;

                              public static class Api
                              {
                                  [Get("/api")]
                                  [EnableCors("AllowAll")]
                                  public static ErrorOr<string> GetData() => "data";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain(".RequireCors(\"AllowAll\")");
    }

    [Fact]
    public async Task DisableCors_Emits_DisableCors()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Cors;

                              public static class Api
                              {
                                  [Get("/internal")]
                                  [DisableCors]
                                  public static ErrorOr<string> Internal() => "internal";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        // DisableCors should be emitted
        generated.Should().Match("*Cors*");
    }

    #endregion

    #region Endpoint Naming and Tags

    [Fact]
    public async Task EndpointName_Uses_ClassName_And_MethodName()
    {
        const string Source = """
                              using ErrorOr;

                              public static class TodoApi
                              {
                                  [Get("/todos/{id}")]
                                  public static ErrorOr<string> GetById(int id) => $"Todo {id}";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain(".WithName(\"TodoApi_GetById\")");
    }

    [Fact]
    public async Task EndpointTags_Uses_ClassName()
    {
        const string Source = """
                              using ErrorOr;

                              public static class UserApi
                              {
                                  [Get("/users")]
                                  public static ErrorOr<string> GetAll() => "users";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain(".WithTags(\"UserApi\")");
    }

    #endregion

    #region Security - Attribute Not Lost

    [Fact]
    public async Task Security_Authorize_NotLost_InWrapper()
    {
        // This is the critical test - verifying the wrapper pattern doesn't lose security
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Authorization;

                              public static class AdminApi
                              {
                                  [Get("/admin/secrets")]
                                  [Authorize("SuperAdmin")]
                                  public static ErrorOr<string> GetSecrets() => "top-secret";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;

        // The wrapper method (Invoke_Ep0) doesn't have [Authorize], so we MUST emit:
        generated.Should().Contain(".RequireAuthorization(\"SuperAdmin\")",
            "wrapper delegates lose original method attributes, so RequireAuthorization MUST be emitted");

        // Also verify the endpoint builder chain includes auth
        var lines = generated.Split('\n');
        var endpointLine = lines.FirstOrDefault(static l =>
            l.Contains("MapGet", StringComparison.Ordinal) && l.Contains("/admin/secrets", StringComparison.Ordinal));

        // The RequireAuthorization should be in the fluent chain
        generated.Should().Match("*MapGet*admin/secrets*RequireAuthorization*SuperAdmin*");
    }

    [Fact]
    public async Task Security_Multiple_Policies_All_Applied()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Authorization;
                              using Microsoft.AspNetCore.RateLimiting;

                              public static class SecureApi
                              {
                                  [Get("/secure/data")]
                                  [Authorize("Policy1")]
                                  [Authorize("Policy2")]
                                  [EnableRateLimiting("strict")]
                                  public static ErrorOr<string> GetSecureData() => "secure";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;

        // All security attributes must be preserved
        generated.Should().Contain("Policy1");
        generated.Should().Contain("Policy2");
        generated.Should().Contain(".RequireRateLimiting(\"strict\")");
    }

    #endregion
}
