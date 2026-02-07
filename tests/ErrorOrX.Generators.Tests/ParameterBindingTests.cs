namespace ErrorOrX.Generators.Tests;

/// <summary>
///     Tests for smart parameter binding inference in the ErrorOrEndpointGenerator.
///     Covers: service detection, body inference, route/query binding, special types, and diagnostics.
/// </summary>
public class ParameterBindingTests : GeneratorTestBase
{
    #region HTTP Method + Complex Type → Body Inference

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

    #endregion

    #region Keyed Services

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

    #endregion

    #region Combined Parameters

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

    #endregion

    #region Custom Binding (TryParse)

    [Fact]
    public async Task Type_With_TryParse_Uses_Custom_Binding()
    {
        const string Source = """
                              using ErrorOr;

                              public readonly struct CustomId
                              {
                                  public int Value { get; }
                                  private CustomId(int value) => Value = value;
                                  public static bool TryParse(string? s, out CustomId result)
                                  {
                                      if (int.TryParse(s, out var value))
                                      {
                                          result = new CustomId(value);
                                          return true;
                                      }
                                      result = default;
                                      return false;
                                  }
                              }

                              public static class Api
                              {
                                  [Get("/items/{id}")]
                                  public static ErrorOr<string> GetById(CustomId id) => $"Item {id.Value}";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain("CustomId.TryParse");
    }

    #endregion

    #region Interface and Abstract Type → Service Inference

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

    #endregion

    #region Service Naming Patterns

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

    #endregion

    #region GET/DELETE + Complex Type → EOE021 Warning

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

    #endregion

    #region Route Parameter Binding

    [Fact]
    public async Task Route_Parameter_Name_Match_Binds_From_Route()
    {
        const string Source = """
                              using ErrorOr;

                              public static class Api
                              {
                                  [Get("/todos/{id}")]
                                  public static ErrorOr<string> GetById(int id) => $"Todo {id}";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain("TryGetRouteValue(ctx, \"id\"");
    }

    [Fact]
    public async Task Multiple_Route_Parameters_Bind_Correctly()
    {
        const string Source = """
                              using ErrorOr;

                              public static class Api
                              {
                                  [Get("/users/{userId}/posts/{postId}")]
                                  public static ErrorOr<string> GetPost(int userId, int postId)
                                      => $"User {userId} Post {postId}";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain("TryGetRouteValue(ctx, \"userId\"");
        generated.Should().Contain("TryGetRouteValue(ctx, \"postId\"");
    }

    [Fact]
    public async Task Guid_Route_Parameter_Uses_TryParse()
    {
        const string Source = """
                              using ErrorOr;
                              using System;

                              public static class Api
                              {
                                  [Get("/items/{id}")]
                                  public static ErrorOr<string> GetById(Guid id) => id.ToString();
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain("Guid.TryParse");
    }

    #endregion

    #region Query Parameter Binding

    [Fact]
    public async Task Primitive_NotInRoute_Infers_Query()
    {
        const string Source = """
                              using ErrorOr;

                              public static class Api
                              {
                                  [Get("/search")]
                                  public static ErrorOr<string> Search(string query, int page) => $"Query: {query}, Page: {page}";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain("TryGetQueryValue(ctx, \"query\"");
        generated.Should().Contain("TryGetQueryValue(ctx, \"page\"");
    }

    [Fact]
    public async Task Primitive_Array_Binds_As_Query_Collection()
    {
        const string Source = """
                              using ErrorOr;

                              public static class Api
                              {
                                  [Get("/filter")]
                                  public static ErrorOr<string> Filter(int[] ids) => $"Count: {ids.Length}";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain("ctx.Request.Query[\"ids\"]");
        generated.Should().Contain("ToArray()");
    }

    [Fact]
    public async Task Nullable_Query_Parameter_Allows_Missing()
    {
        const string Source = """
                              using ErrorOr;

                              public static class Api
                              {
                                  [Get("/search")]
                                  public static ErrorOr<string> Search(string? query) => query ?? "all";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        // Nullable query parameters use default when missing, not BindFail
        generated.Should().Contain("= default");
    }

    #endregion

    #region Special Types

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

    #endregion

    #region Explicit Attribute Bindings

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

    #endregion

}
