namespace ErrorOrX.Generators.Tests;

/// <summary>
///     Tests for smart parameter binding inference: HTTP-method + type → source mapping,
///     service detection (interface, abstract, DI naming patterns), and the EOE021
///     ambiguous-parameter diagnostic for bodyless verbs with complex types.
/// </summary>
public class ParameterBindingInferenceTests : GeneratorTestBase
{
    [Theory]
    [InlineData("Post")]
    [InlineData("Put")]
    [InlineData("Patch")]
    public async Task Complex_Type_On_BodyMethod_Infers_Body(string httpMethod)
    {
        var source = $$"""
                       using ErrorOr;
                       using System.Text.Json.Serialization;
                       using Microsoft.AspNetCore.Mvc;
                       using Microsoft.AspNetCore.Http;

                       public record CreateRequest(string Name);
                       public record Response(int Id, string Name);

                       public static class Api
                       {
                           [{{httpMethod}}("/test")]
                           public static ErrorOr<Response> Handler(CreateRequest req) => new Response(1, req.Name);
                       }

                       [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
                       [JsonSerializable(typeof(CreateRequest))]
                       [JsonSerializable(typeof(Response))]
                       [JsonSerializable(typeof(ProblemDetails))]
                       [JsonSerializable(typeof(HttpValidationProblemDetails))]
                       internal partial class TestJsonContext : JsonSerializerContext { }
                       """;

        using var result = await RunAsync(source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain("ReadFromJsonAsync<global::CreateRequest>");
    }

    [Fact]
    public async Task Mixed_Parameter_Sources_Bind_Correctly()
    {
        const string Source = """
                              using ErrorOr;
                              using System.Text.Json.Serialization;
                              using Microsoft.AspNetCore.Mvc;
                              using Microsoft.AspNetCore.Http;

                              public interface ITodoService { string Create(int userId, string title); }
                              public record CreateTodoRequest(string Title);

                              public static class Api
                              {
                                  [Post("/users/{userId}/todos")]
                                  public static ErrorOr<string> Create(
                                      int userId,                  // Route (matches {userId})
                                      CreateTodoRequest req,       // Body (POST + complex type)
                                      ITodoService svc)            // Service (interface)
                                      => svc.Create(userId, req.Title);
                              }

                              [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
                              [JsonSerializable(typeof(CreateTodoRequest))]
                              [JsonSerializable(typeof(ProblemDetails))]
                              [JsonSerializable(typeof(HttpValidationProblemDetails))]
                              internal partial class TestJsonContext : JsonSerializerContext { }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain("TryGetRouteValue(ctx, \"userId\"");
        generated.Should().Contain("ReadFromJsonAsync<global::CreateTodoRequest>");
        generated.Should().Contain("GetRequiredService<global::ITodoService>");
    }

    [Fact]
    public async Task Interface_Type_Infers_Service()
    {
        const string Source = """
                              using ErrorOr;

                              public interface IMyService { string GetValue(); }

                              public static class Api
                              {
                                  [Get("/test")]
                                  public static ErrorOr<string> Handler(IMyService svc) => svc.GetValue();
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain("GetRequiredService<global::IMyService>");
    }

    [Fact]
    public async Task Abstract_Type_Infers_Service()
    {
        const string Source = """
                              using ErrorOr;

                              public abstract class BaseService { public abstract string GetValue(); }

                              public static class Api
                              {
                                  [Get("/test")]
                                  public static ErrorOr<string> Handler(BaseService svc) => svc.GetValue();
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain("GetRequiredService<global::BaseService>");
    }

    [Theory]
    [InlineData("TodoRepository")]
    [InlineData("TodoHandler")]
    [InlineData("TodoManager")]
    [InlineData("ConfigProvider")]
    [InlineData("TodoFactory")]
    [InlineData("HttpClient")]
    public async Task Service_Naming_Pattern_Infers_Service(string typeName)
    {
        var source = $$"""
                       using ErrorOr;

                       public class {{typeName}} { public string GetValue() => "test"; }

                       public static class Api
                       {
                           [Get("/test")]
                           public static ErrorOr<string> Handler({{typeName}} svc) => svc.GetValue();
                       }
                       """;

        using var result = await RunAsync(source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain($"GetRequiredService<global::{typeName}>");
    }

    [Fact]
    public async Task DbContext_Pattern_Infers_Service()
    {
        const string Source = """
                              using ErrorOr;

                              public class AppDbContext { public string Query() => "data"; }

                              public static class Api
                              {
                                  [Get("/test")]
                                  public static ErrorOr<string> Handler(AppDbContext db) => db.Query();
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain("GetRequiredService<global::AppDbContext>");
    }

    [Theory]
    [InlineData("Get")]
    [InlineData("Delete")]
    public async Task Complex_Type_On_BodylessMethod_Emits_EOE021(string httpMethod)
    {
        var source = $$"""
                       using ErrorOr;

                       public record SearchFilter(string Query, int Page);

                       public static class Api
                       {
                           [{{httpMethod}}("/test")]
                           public static ErrorOr<string> Handler(SearchFilter filter) => "result";
                       }
                       """;

        using var result = await RunAsync(source);

        result.Diagnostics.Should().ContainSingle(static d => d.Id == "EOE021");
        var diagnostic = result.Diagnostics.First(static d => d.Id == "EOE021");
        diagnostic.GetMessage().Should().Contain("filter");
        diagnostic.GetMessage().Should().Contain(httpMethod.ToUpperInvariant());
    }

    [Fact]
    public async Task Complex_Type_With_Explicit_FromQuery_NoWarning()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Mvc;

                              public record SearchFilter(string Query, int Page);

                              public static class Api
                              {
                                  [Get("/test")]
                                  public static ErrorOr<string> Handler([FromQuery] SearchFilter filter) => "result";
                              }
                              """;

        using var result = await RunAsync(Source);

        // EOE011: [FromQuery] only supports primitives or collections of primitives
        // This is expected behavior - complex types can't be query bound without [AsParameters]
        result.Diagnostics.Should().ContainSingle(static d => d.Id == "EOE011");
    }

    [Fact]
    public async Task Complex_Type_With_AsParameters_NoWarning()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Http;

                              public record SearchFilter(string Query, int Page);

                              public static class Api
                              {
                                  [Get("/test")]
                                  public static ErrorOr<string> Handler([AsParameters] SearchFilter filter) => "result";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        // AsParameters expands to individual bindings
        generated.Should().Contain("TryGetQueryValue");
    }
}
