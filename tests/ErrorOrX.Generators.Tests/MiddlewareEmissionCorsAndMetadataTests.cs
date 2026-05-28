namespace ErrorOrX.Generators.Tests;

/// <summary>
///     CORS attribute emission (<c>[EnableCors]</c>, <c>[DisableCors]</c>), endpoint metadata
///     (<c>.WithName</c>, <c>.WithTags</c>), and the security-regression tests that pin the
///     "wrapper does not drop <c>[Authorize]</c>" contract — the single most important invariant
///     of the middleware emitter.
/// </summary>
public class MiddlewareEmissionCorsAndMetadataTests : GeneratorTestBase
{
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
        // CorsEndpointConventionBuilderExtensions has no .DisableCors() helper. The framework
        // recognises DisableCorsAttribute as metadata, so the generator emits a WithMetadata call.
        generated.Should()
            .Contain(".WithMetadata(new global::Microsoft.AspNetCore.Cors.DisableCorsAttribute())");
        generated.Should().NotContain(".DisableCors()");
    }

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
}
