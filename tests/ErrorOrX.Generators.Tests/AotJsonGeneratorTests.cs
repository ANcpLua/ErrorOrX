using System.Text.Json.Serialization;
using ANcpLua.Roslyn.Utilities.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ErrorOrX.Generators.Tests;

public sealed class AotJsonGeneratorTests : GeneratorTestBase
{
    /// <summary>
    ///     Common attribute definitions needed for tests to work with ForAttributeWithMetadataName.
    /// </summary>
    private const string RouteAttributesSource = """

                                                 namespace ErrorOr
                                                 {
                                                     [global::System.AttributeUsage(global::System.AttributeTargets.Method)]
                                                     public sealed class GetAttribute : global::System.Attribute
                                                     {
                                                         public GetAttribute(string route) => Route = route;
                                                         public string Route { get; }
                                                     }

                                                     [global::System.AttributeUsage(global::System.AttributeTargets.Method)]
                                                     public sealed class PostAttribute : global::System.Attribute
                                                     {
                                                         public PostAttribute(string route) => Route = route;
                                                         public string Route { get; }
                                                     }

                                                     [global::System.AttributeUsage(global::System.AttributeTargets.Method)]
                                                     public sealed class PutAttribute : global::System.Attribute
                                                     {
                                                         public PutAttribute(string route) => Route = route;
                                                         public string Route { get; }
                                                     }

                                                     [global::System.AttributeUsage(global::System.AttributeTargets.Method)]
                                                     public sealed class DeleteAttribute : global::System.Attribute
                                                     {
                                                         public DeleteAttribute(string route) => Route = route;
                                                         public string Route { get; }
                                                     }

                                                     [global::System.AttributeUsage(global::System.AttributeTargets.Method)]
                                                     public sealed class PatchAttribute : global::System.Attribute
                                                     {
                                                         public PatchAttribute(string route) => Route = route;
                                                         public string Route { get; }
                                                     }

                                                     public readonly struct ErrorOr<T>
                                                     {
                                                         public T Value { get; }
                                                     }
                                                 }
                                                 """;

    private static readonly Type[] AotJsonRequiredTypes =
    [
        typeof(HttpContext),
        typeof(FromBodyAttribute),
        typeof(IServiceCollection),
        typeof(JsonSerializerContext),
        typeof(JsonSerializableAttribute),
        typeof(Error)
    ];

    #region Type Discovery - Request Parameters

    [Fact]
    public async Task Discovers_FromBody_Parameter_Type()
    {
        const string Source = """
                              using System.Text.Json.Serialization;
                              using ErrorOr;
                              using Microsoft.AspNetCore.Mvc;

                              [AotJson]
                              internal partial class AppJsonSerializerContext : JsonSerializerContext;

                              public static class Api
                              {
                                  [Post("/items")]
                                  public static ErrorOr<Item> Create([FromBody] CreateRequest request) => default;
                              }

                              public record Item(int Id);
                              public record CreateRequest(string Name);
                              """ + RouteAttributesSource;

        using var scope = TestConfiguration.WithAdditionalReferences(AotJsonRequiredTypes);
        using var result = await Test<AotJsonGenerator>.Run(Source, TestContext.Current.CancellationToken);

        result
            .Produces("AppJsonSerializerContext.AotJson.g.cs")
            .IsClean();

        var generatedFile = result.Files.FirstOrDefault(f =>
            f.HintName.Contains("AppJsonSerializerContext", StringComparison.Ordinal));
        Assert.NotNull(generatedFile);
        Assert.Contains("global::CreateRequest", generatedFile.Content, StringComparison.Ordinal);
    }

    #endregion

    #region Type Discovery - Return Types

    [Fact]
    public async Task Discovers_Simple_Return_Type()
    {
        const string Source = """
                              using System.Text.Json.Serialization;
                              using ErrorOr;

                              [AotJson]
                              internal partial class AppJsonSerializerContext : JsonSerializerContext;

                              public static class Api
                              {
                                  [Get("/item")]
                                  public static ErrorOr<Item> Get() => default;
                              }

                              public record Item(int Id);
                              """ + RouteAttributesSource;

        using var scope = TestConfiguration.WithAdditionalReferences(AotJsonRequiredTypes);
        using var result = await Test<AotJsonGenerator>.Run(Source, TestContext.Current.CancellationToken);

        result
            .Produces("AppJsonSerializerContext.AotJson.g.cs")
            .IsClean();

        var generatedFile = result.Files.FirstOrDefault(f =>
            f.HintName.Contains("AppJsonSerializerContext", StringComparison.Ordinal));
        Assert.NotNull(generatedFile);
        Assert.Contains("JsonSerializable(typeof(global::Item))", generatedFile.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Discovers_List_Return_Type()
    {
        const string Source = """
                              using System.Text.Json.Serialization;
                              using System.Collections.Generic;
                              using ErrorOr;

                              [AotJson]
                              internal partial class AppJsonSerializerContext : JsonSerializerContext;

                              public static class Api
                              {
                                  [Get("/items")]
                                  public static ErrorOr<List<Item>> GetAll() => default;
                              }

                              public record Item(int Id);
                              """ + RouteAttributesSource;

        using var scope = TestConfiguration.WithAdditionalReferences(AotJsonRequiredTypes);
        using var result = await Test<AotJsonGenerator>.Run(Source, TestContext.Current.CancellationToken);

        result
            .Produces("AppJsonSerializerContext.AotJson.g.cs")
            .IsClean();

        var generatedFile = result.Files.FirstOrDefault(f =>
            f.HintName.Contains("AppJsonSerializerContext", StringComparison.Ordinal));
        Assert.NotNull(generatedFile);
        Assert.Contains("List<global::Item>", generatedFile.Content, StringComparison.Ordinal);
    }

    #endregion

    #region Collection Generation

    [Fact]
    public async Task Generates_List_Variant_When_Enabled()
    {
        const string Source = """
                              using System.Text.Json.Serialization;
                              using ErrorOr;

                              [AotJson(GenerateCollections = CollectionKind.List)]
                              internal partial class AppJsonSerializerContext : JsonSerializerContext;

                              public static class Api
                              {
                                  [Get("/item")]
                                  public static ErrorOr<Item> GetItem() => default;
                              }

                              public record Item(string Name);
                              """ + RouteAttributesSource;

        using var scope = TestConfiguration.WithAdditionalReferences(AotJsonRequiredTypes);
        using var result = await Test<AotJsonGenerator>.Run(Source, TestContext.Current.CancellationToken);

        result.Produces("AppJsonSerializerContext.AotJson.g.cs");

        var generatedFile = result.Files.FirstOrDefault(f =>
            f.HintName.Contains("AppJsonSerializerContext", StringComparison.Ordinal));
        Assert.NotNull(generatedFile);
        Assert.Contains("List<global::Item>", generatedFile.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Generates_Array_Variant_When_Enabled()
    {
        const string Source = """
                              using System.Text.Json.Serialization;
                              using ErrorOr;

                              [AotJson(GenerateCollections = CollectionKind.Array)]
                              internal partial class AppJsonSerializerContext : JsonSerializerContext;

                              public static class Api
                              {
                                  [Get("/item")]
                                  public static ErrorOr<Item> GetItem() => default;
                              }

                              public record Item(string Name);
                              """ + RouteAttributesSource;

        using var scope = TestConfiguration.WithAdditionalReferences(AotJsonRequiredTypes);
        using var result = await Test<AotJsonGenerator>.Run(Source, TestContext.Current.CancellationToken);

        result.Produces("AppJsonSerializerContext.AotJson.g.cs");

        var generatedFile = result.Files.FirstOrDefault(f =>
            f.HintName.Contains("AppJsonSerializerContext", StringComparison.Ordinal));
        Assert.NotNull(generatedFile);
        Assert.Contains("global::Item[]", generatedFile.Content, StringComparison.Ordinal);
    }

    #endregion

    #region ProblemDetails

    [Fact]
    public async Task Includes_ProblemDetails_By_Default()
    {
        const string Source = """
                              using System.Text.Json.Serialization;
                              using ErrorOr;

                              [AotJson]
                              internal partial class AppJsonSerializerContext : JsonSerializerContext;

                              public static class Api
                              {
                                  [Get("/test")]
                                  public static ErrorOr<string> Get() => default;
                              }
                              """ + RouteAttributesSource;

        using var scope = TestConfiguration.WithAdditionalReferences(AotJsonRequiredTypes);
        using var result = await Test<AotJsonGenerator>.Run(Source, TestContext.Current.CancellationToken);

        result.Produces("AppJsonSerializerContext.AotJson.g.cs");

        var generatedFile = result.Files.FirstOrDefault(f =>
            f.HintName.Contains("AppJsonSerializerContext", StringComparison.Ordinal));
        Assert.NotNull(generatedFile);
        Assert.Contains("ProblemDetails", generatedFile.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Includes_HttpValidationProblemDetails_By_Default()
    {
        const string Source = """
                              using System.Text.Json.Serialization;
                              using ErrorOr;

                              [AotJson]
                              internal partial class AppJsonSerializerContext : JsonSerializerContext;

                              public static class Api
                              {
                                  [Get("/test")]
                                  public static ErrorOr<string> Get() => default;
                              }
                              """ + RouteAttributesSource;

        using var scope = TestConfiguration.WithAdditionalReferences(AotJsonRequiredTypes);
        using var result = await Test<AotJsonGenerator>.Run(Source, TestContext.Current.CancellationToken);

        result.Produces("AppJsonSerializerContext.AotJson.g.cs");

        var generatedFile = result.Files.FirstOrDefault(f =>
            f.HintName.Contains("AppJsonSerializerContext", StringComparison.Ordinal));
        Assert.NotNull(generatedFile);
        Assert.Contains("HttpValidationProblemDetails", generatedFile.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Excludes_ProblemDetails_When_Disabled()
    {
        const string Source = """
                              using System.Text.Json.Serialization;
                              using ErrorOr;

                              [AotJson(IncludeProblemDetails = false)]
                              internal partial class AppJsonSerializerContext : JsonSerializerContext;

                              public static class Api
                              {
                                  [Get("/test")]
                                  public static ErrorOr<TestDto> Get() => default;
                              }

                              public record TestDto();
                              """ + RouteAttributesSource;

        using var scope = TestConfiguration.WithAdditionalReferences(AotJsonRequiredTypes);
        using var result = await Test<AotJsonGenerator>.Run(Source, TestContext.Current.CancellationToken);

        result.Produces("AppJsonSerializerContext.AotJson.g.cs");

        var generatedFile = result.Files.FirstOrDefault(f =>
            f.HintName.Contains("AppJsonSerializerContext", StringComparison.Ordinal));
        Assert.NotNull(generatedFile);
        Assert.DoesNotContain("typeof(global::Microsoft.AspNetCore.Mvc.ProblemDetails)", generatedFile.Content,
            StringComparison.Ordinal);
    }

    #endregion

    #region Async Unwrapping

    [Fact]
    public async Task Unwraps_Task_ErrorOr_Return_Type()
    {
        const string Source = """
                              using System.Text.Json.Serialization;
                              using System.Threading.Tasks;
                              using ErrorOr;

                              [AotJson]
                              internal partial class AppJsonSerializerContext : JsonSerializerContext;

                              public static class Api
                              {
                                  [Get("/async")]
                                  public static Task<ErrorOr<AsyncResult>> GetAsync() =>
                                      Task.FromResult<ErrorOr<AsyncResult>>(default);
                              }

                              public record AsyncResult(string Status);
                              """ + RouteAttributesSource;

        using var scope = TestConfiguration.WithAdditionalReferences(AotJsonRequiredTypes);
        using var result = await Test<AotJsonGenerator>.Run(Source, TestContext.Current.CancellationToken);

        result.Produces("AppJsonSerializerContext.AotJson.g.cs");

        var generatedFile = result.Files.FirstOrDefault(f =>
            f.HintName.Contains("AppJsonSerializerContext", StringComparison.Ordinal));
        Assert.NotNull(generatedFile);
        Assert.Contains("global::AsyncResult", generatedFile.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unwraps_Task_ErrorOr_List_Return_Type()
    {
        const string Source = """
                              using System.Text.Json.Serialization;
                              using System.Collections.Generic;
                              using System.Threading.Tasks;
                              using ErrorOr;

                              [AotJson]
                              internal partial class AppJsonSerializerContext : JsonSerializerContext;

                              public static class Api
                              {
                                  [Get("/nested")]
                                  public static Task<ErrorOr<List<NestedItem>>> GetNested() =>
                                      Task.FromResult<ErrorOr<List<NestedItem>>>(default);
                              }

                              public record NestedItem(int Id);
                              """ + RouteAttributesSource;

        using var scope = TestConfiguration.WithAdditionalReferences(AotJsonRequiredTypes);
        using var result = await Test<AotJsonGenerator>.Run(Source, TestContext.Current.CancellationToken);

        result.Produces("AppJsonSerializerContext.AotJson.g.cs");

        var generatedFile = result.Files.FirstOrDefault(f =>
            f.HintName.Contains("AppJsonSerializerContext", StringComparison.Ordinal));
        Assert.NotNull(generatedFile);
        Assert.Contains("global::NestedItem", generatedFile.Content, StringComparison.Ordinal);
    }

    #endregion

    #region Property Type Traversal

    [Fact]
    public async Task Discovers_Nested_Property_Types()
    {
        const string Source = """
                              using System.Text.Json.Serialization;
                              using ErrorOr;

                              [AotJson]
                              internal partial class AppJsonSerializerContext : JsonSerializerContext;

                              public static class Api
                              {
                                  [Get("/order")]
                                  public static ErrorOr<Order> Get() => default;
                              }

                              public record Order(int Id, Customer Customer);
                              public record Customer(string Name, Address Address);
                              public record Address(string City, string Country);
                              """ + RouteAttributesSource;

        using var scope = TestConfiguration.WithAdditionalReferences(AotJsonRequiredTypes);
        using var result = await Test<AotJsonGenerator>.Run(Source, TestContext.Current.CancellationToken);

        result
            .Produces("AppJsonSerializerContext.AotJson.g.cs")
            .IsClean();

        var generatedFile = result.Files.FirstOrDefault(f =>
            f.HintName.Contains("AppJsonSerializerContext", StringComparison.Ordinal));
        Assert.NotNull(generatedFile);
        Assert.Contains("global::Order", generatedFile.Content, StringComparison.Ordinal);
        Assert.Contains("global::Customer", generatedFile.Content, StringComparison.Ordinal);
        Assert.Contains("global::Address", generatedFile.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handles_Cyclic_Type_References()
    {
        const string Source = """
                              using System.Text.Json.Serialization;
                              using ErrorOr;

                              [AotJson]
                              internal partial class AppJsonSerializerContext : JsonSerializerContext;

                              public static class Api
                              {
                                  [Get("/node")]
                                  public static ErrorOr<TreeNode> Get() => default;
                              }

                              public class TreeNode
                              {
                                  public string Value { get; set; } = "";
                                  public TreeNode? Parent { get; set; }
                                  public TreeNode[]? Children { get; set; }
                              }
                              """ + RouteAttributesSource;

        using var scope = TestConfiguration.WithAdditionalReferences(AotJsonRequiredTypes);
        using var result = await Test<AotJsonGenerator>.Run(Source, TestContext.Current.CancellationToken);

        result
            .Produces("AppJsonSerializerContext.AotJson.g.cs")
            .IsClean();

        var generatedFile = result.Files.FirstOrDefault(f =>
            f.HintName.Contains("AppJsonSerializerContext", StringComparison.Ordinal));
        Assert.NotNull(generatedFile);
        Assert.Contains("global::TreeNode", generatedFile.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Respects_TraversePropertyTypes_False()
    {
        const string Source = """
                              using System.Text.Json.Serialization;
                              using ErrorOr;

                              [AotJson(TraversePropertyTypes = false)]
                              internal partial class AppJsonSerializerContext : JsonSerializerContext;

                              public static class Api
                              {
                                  [Get("/order")]
                                  public static ErrorOr<Order> Get() => default;
                              }

                              public record Order(int Id, Customer Customer);
                              public record Customer(string Name);
                              """ + RouteAttributesSource;

        using var scope = TestConfiguration.WithAdditionalReferences(AotJsonRequiredTypes);
        using var result = await Test<AotJsonGenerator>.Run(Source, TestContext.Current.CancellationToken);

        result.Produces("AppJsonSerializerContext.AotJson.g.cs");

        var generatedFile = result.Files.FirstOrDefault(f =>
            f.HintName.Contains("AppJsonSerializerContext", StringComparison.Ordinal));
        Assert.NotNull(generatedFile);
        Assert.Contains("global::Order", generatedFile.Content, StringComparison.Ordinal);
        // Customer should NOT be discovered when TraversePropertyTypes = false
        Assert.DoesNotContain("global::Customer", generatedFile.Content, StringComparison.Ordinal);
    }

    #endregion

    #region Collection Variants

    [Fact]
    public async Task Generates_IEnumerable_Variant_When_Enabled()
    {
        const string Source = """
                              using System.Text.Json.Serialization;
                              using ErrorOr;

                              [AotJson(GenerateCollections = CollectionKind.IEnumerable)]
                              internal partial class AppJsonSerializerContext : JsonSerializerContext;

                              public static class Api
                              {
                                  [Get("/item")]
                                  public static ErrorOr<Item> GetItem() => default;
                              }

                              public record Item(string Name);
                              """ + RouteAttributesSource;

        using var scope = TestConfiguration.WithAdditionalReferences(AotJsonRequiredTypes);
        using var result = await Test<AotJsonGenerator>.Run(Source, TestContext.Current.CancellationToken);

        result.Produces("AppJsonSerializerContext.AotJson.g.cs");

        var generatedFile = result.Files.FirstOrDefault(f =>
            f.HintName.Contains("AppJsonSerializerContext", StringComparison.Ordinal));
        Assert.NotNull(generatedFile);
        Assert.Contains("IEnumerable<global::Item>", generatedFile.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Generates_IReadOnlyList_Variant_When_Enabled()
    {
        const string Source = """
                              using System.Text.Json.Serialization;
                              using ErrorOr;

                              [AotJson(GenerateCollections = CollectionKind.IReadOnlyList)]
                              internal partial class AppJsonSerializerContext : JsonSerializerContext;

                              public static class Api
                              {
                                  [Get("/item")]
                                  public static ErrorOr<Item> GetItem() => default;
                              }

                              public record Item(string Name);
                              """ + RouteAttributesSource;

        using var scope = TestConfiguration.WithAdditionalReferences(AotJsonRequiredTypes);
        using var result = await Test<AotJsonGenerator>.Run(Source, TestContext.Current.CancellationToken);

        result.Produces("AppJsonSerializerContext.AotJson.g.cs");

        var generatedFile = result.Files.FirstOrDefault(f =>
            f.HintName.Contains("AppJsonSerializerContext", StringComparison.Ordinal));
        Assert.NotNull(generatedFile);
        Assert.Contains("IReadOnlyList<global::Item>", generatedFile.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Generates_All_Collection_Variants()
    {
        const string Source = """
                              using System.Text.Json.Serialization;
                              using ErrorOr;

                              [AotJson(GenerateCollections = CollectionKind.All)]
                              internal partial class AppJsonSerializerContext : JsonSerializerContext;

                              public static class Api
                              {
                                  [Get("/item")]
                                  public static ErrorOr<Item> GetItem() => default;
                              }

                              public record Item(string Name);
                              """ + RouteAttributesSource;

        using var scope = TestConfiguration.WithAdditionalReferences(AotJsonRequiredTypes);
        using var result = await Test<AotJsonGenerator>.Run(Source, TestContext.Current.CancellationToken);

        result.Produces("AppJsonSerializerContext.AotJson.g.cs");

        var generatedFile = result.Files.FirstOrDefault(f =>
            f.HintName.Contains("AppJsonSerializerContext", StringComparison.Ordinal));
        Assert.NotNull(generatedFile);
        Assert.Contains("List<global::Item>", generatedFile.Content, StringComparison.Ordinal);
        Assert.Contains("global::Item[]", generatedFile.Content, StringComparison.Ordinal);
        Assert.Contains("IEnumerable<global::Item>", generatedFile.Content, StringComparison.Ordinal);
        Assert.Contains("IReadOnlyList<global::Item>", generatedFile.Content, StringComparison.Ordinal);
    }

    #endregion

    #region Assembly-Level Attribute

    [Fact]
    public async Task AotJsonAssembly_Generates_Context_When_No_AotJson_Exists()
    {
        const string Source = """
                              using System.Text.Json.Serialization;
                              using ErrorOr;

                              [assembly: AotJsonAssembly(ContextTypeName = "GeneratedJsonContext")]

                              public static class Api
                              {
                                  [Get("/item")]
                                  public static ErrorOr<Item> GetItem() => default;
                              }

                              public record Item(string Name);
                              """ + RouteAttributesSource;

        using var scope = TestConfiguration.WithAdditionalReferences(AotJsonRequiredTypes);
        using var result = await Test<AotJsonGenerator>.Run(Source, TestContext.Current.CancellationToken);

        result.Produces("GeneratedJsonContext.g.cs");

        var generatedFile = result.Files.FirstOrDefault(f =>
            f.HintName.Contains("GeneratedJsonContext", StringComparison.Ordinal));
        Assert.NotNull(generatedFile);
        Assert.Contains("partial class GeneratedJsonContext", generatedFile.Content, StringComparison.Ordinal);
        Assert.Contains("JsonSerializerContext", generatedFile.Content, StringComparison.Ordinal);
        Assert.Contains("global::Item", generatedFile.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AotJsonAssembly_Does_Not_Generate_When_AotJson_Exists()
    {
        const string Source = """
                              using System.Text.Json.Serialization;
                              using ErrorOr;

                              [assembly: AotJsonAssembly(ContextTypeName = "ShouldNotBeGenerated")]

                              [AotJson]
                              internal partial class ExistingContext : JsonSerializerContext;

                              public static class Api
                              {
                                  [Get("/item")]
                                  public static ErrorOr<Item> GetItem() => default;
                              }

                              public record Item(string Name);
                              """ + RouteAttributesSource;

        using var scope = TestConfiguration.WithAdditionalReferences(AotJsonRequiredTypes);
        using var result = await Test<AotJsonGenerator>.Run(Source, TestContext.Current.CancellationToken);

        // Should generate for ExistingContext, NOT for ShouldNotBeGenerated
        Assert.Contains(result.Files, f => f.HintName.Contains("ExistingContext", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Files, f => f.HintName.Contains("ShouldNotBeGenerated", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AotJsonAssembly_Generates_With_Custom_Namespace()
    {
        const string Source = """
                              using System.Text.Json.Serialization;
                              using ErrorOr;

                              [assembly: AotJsonAssembly(ContextNamespace = "MyApp.Serialization", ContextTypeName = "AppContext")]

                              public static class Api
                              {
                                  [Get("/item")]
                                  public static ErrorOr<Item> GetItem() => default;
                              }

                              public record Item(string Name);
                              """ + RouteAttributesSource;

        using var scope = TestConfiguration.WithAdditionalReferences(AotJsonRequiredTypes);
        using var result = await Test<AotJsonGenerator>.Run(Source, TestContext.Current.CancellationToken);

        result.Produces("AppContext.g.cs");

        var generatedFile = result.Files.FirstOrDefault(f =>
            f.HintName.Contains("AppContext", StringComparison.Ordinal));
        Assert.NotNull(generatedFile);
        Assert.Contains("namespace MyApp.Serialization;", generatedFile.Content, StringComparison.Ordinal);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Ignores_Non_JsonSerializerContext_Classes()
    {
        const string Source = """
                              using System.Text.Json.Serialization;
                              using ErrorOr;

                              [AotJson]
                              internal partial class NotAContext;

                              public static class Api
                              {
                                  [Get("/test")]
                                  public static ErrorOr<string> Get() => default;
                              }
                              """ + RouteAttributesSource;

        using var scope = TestConfiguration.WithAdditionalReferences(AotJsonRequiredTypes);
        using var result = await Test<AotJsonGenerator>.Run(Source, TestContext.Current.CancellationToken);

        Assert.DoesNotContain(result.Files, f => f.HintName.Contains("NotAContext.AotJson", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Generator_Caches_Correctly()
    {
        const string Source = """
                              using System.Text.Json.Serialization;
                              using ErrorOr;

                              [AotJson]
                              internal partial class AppJsonSerializerContext : JsonSerializerContext;

                              public static class Api
                              {
                                  [Get("/test")]
                                  public static ErrorOr<CacheTestItem> Get() => default;
                              }

                              public record CacheTestItem(int Id);
                              """ + RouteAttributesSource;

        using var scope = TestConfiguration.WithAdditionalReferences(AotJsonRequiredTypes);
        using var result = await Test<AotJsonGenerator>.Run(Source, TestContext.Current.CancellationToken);

        // Verify generator output and caching of our custom steps
        // (Internal Roslyn steps like Compilation and result_ForAttributeWithMetadataName
        // inherently cache semantic types - we only verify our named steps)
        result
            .Produces("AppJsonSerializerContext.AotJson.g.cs")
            .IsCached("AotJson_Contexts", "AotJson_EndpointTypes");
    }

    #endregion
}