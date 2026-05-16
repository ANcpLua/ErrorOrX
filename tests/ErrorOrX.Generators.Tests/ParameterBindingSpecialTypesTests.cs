namespace ErrorOrX.Generators.Tests;

/// <summary>
///     Tests for special parameter types that bypass normal classification:
///     <c>HttpContext</c>, <c>CancellationToken</c>, and <c>Stream</c> for request-body access.
/// </summary>
public class ParameterBindingSpecialTypesTests : GeneratorTestBase
{
    [Fact]
    public async Task HttpContext_Binds_Directly()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Http;

                              public static class Api
                              {
                                  [Get("/info")]
                                  public static ErrorOr<string> GetInfo(HttpContext context) => context.Request.Path;
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        // HttpContext binds directly from ctx parameter (uses p0, p1, etc. naming)
        generated.Should().Contain("= ctx;");
        generated.Should().Contain("global::Api.GetInfo(p0)");
    }

    [Fact]
    public async Task CancellationToken_Binds_From_RequestAborted()
    {
        const string Source = """
                              using ErrorOr;
                              using System.Threading;

                              public static class Api
                              {
                                  [Get("/long-running")]
                                  public static ErrorOr<string> LongRunning(CancellationToken cancellationToken) => "done";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain("ctx.RequestAborted");
    }

    [Fact]
    public async Task Stream_Binds_From_RequestBody()
    {
        const string Source = """
                              using ErrorOr;
                              using System.IO;

                              public static class Api
                              {
                                  [Post("/upload")]
                                  public static ErrorOr<string> Upload(Stream body) => "uploaded";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain("ctx.Request.Body");
    }
}
