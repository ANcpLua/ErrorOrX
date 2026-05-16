namespace ErrorOrX.Generators.Tests;

/// <summary>
///     Tests for explicit attribute bindings — <c>[FromBody]</c>, <c>[FromServices]</c>,
///     <c>[FromKeyedServices]</c>, <c>[FromRoute]</c>, <c>[FromQuery]</c>, <c>[FromHeader]</c>.
///     Verifies that explicit attributes override inference and that <c>Name = "..."</c>
///     overrides the parameter name on the bound source.
/// </summary>
public class ParameterBindingExplicitAttributeTests : GeneratorTestBase
{
    [Fact]
    public async Task FromKeyedServices_Binds_With_Key()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.Extensions.DependencyInjection;

                              public interface ICache { string Get(string key); }

                              public static class Api
                              {
                                  [Get("/cached")]
                                  public static ErrorOr<string> Handler([FromKeyedServices("redis")] ICache cache)
                                      => cache.Get("key");
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain("GetRequiredKeyedService<global::ICache>(\"redis\")");
    }

    [Fact]
    public async Task FromBody_Attribute_Forces_Body_Binding()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Mvc;
                              using System.Text.Json.Serialization;
                              using Microsoft.AspNetCore.Http;

                              public record Payload(string Data);

                              public static class Api
                              {
                                  [Get("/test")]
                                  public static ErrorOr<string> Handler([FromBody] Payload payload) => payload.Data;
                              }

                              [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
                              [JsonSerializable(typeof(Payload))]
                              [JsonSerializable(typeof(ProblemDetails))]
                              [JsonSerializable(typeof(HttpValidationProblemDetails))]
                              internal partial class TestJsonContext : JsonSerializerContext { }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain("ReadFromJsonAsync<global::Payload>");
    }

    [Fact]
    public async Task FromServices_Attribute_Forces_Service_Binding()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Mvc;

                              public class MyHelper { public string Help() => "help"; }

                              public static class Api
                              {
                                  [Get("/test")]
                                  public static ErrorOr<string> Handler([FromServices] MyHelper helper) => helper.Help();
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain("GetRequiredService<global::MyHelper>");
    }

    [Fact]
    public async Task FromRoute_Attribute_Forces_Route_Binding()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Mvc;

                              public static class Api
                              {
                                  [Get("/items/{itemId}")]
                                  public static ErrorOr<string> Handler([FromRoute(Name = "itemId")] int id) => $"Item {id}";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain("TryGetRouteValue(ctx, \"itemId\"");
    }

    [Fact]
    public async Task FromQuery_Attribute_With_Name_Uses_Custom_Key()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Mvc;

                              public static class Api
                              {
                                  [Get("/search")]
                                  public static ErrorOr<string> Handler([FromQuery(Name = "q")] string searchTerm) => searchTerm;
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain("TryGetQueryValue(ctx, \"q\"");
    }

    [Fact]
    public async Task FromHeader_Binds_From_Headers()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Mvc;

                              public static class Api
                              {
                                  [Get("/test")]
                                  public static ErrorOr<string> Handler([FromHeader(Name = "X-Api-Key")] string apiKey) => apiKey;
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain("ctx.Request.Headers.TryGetValue(\"X-Api-Key\"");
    }
}
