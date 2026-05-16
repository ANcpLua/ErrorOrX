namespace ErrorOrX.Generators.Tests;

/// <summary>
///     Tests for implicit primitive binding (route by name match, query for unmapped primitives,
///     primitive collections), and custom binding via static TryParse on user-defined types.
/// </summary>
public class ParameterBindingRouteQueryTests : GeneratorTestBase
{
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
}
