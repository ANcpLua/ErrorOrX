namespace ErrorOrX.Generators.Tests;

/// <summary>
///     Tests that verify the generator properly caches its outputs between compilations.
/// </summary>
public class GeneratorCachingTests : GeneratorTestBase
{
    [Fact]
    public async Task Generator_Emits_Shared_Attributes_File()
    {
        const string Source = """
                              using ErrorOr;

                              namespace AttributeTest;

                              public static class TestEndpoints
                              {
                                  [Get("/test")]
                                  public static ErrorOr<string> Get() => "test";
                              }
                              """;

        using var result = await RunAsync(Source);

        var attributes = result.Files.First(static f => f.HintName == "ErrorOrEndpointAttributes.Mappings.g.cs");
        await Verify(new { attributes.HintName, Source = attributes.Content }).UseDirectory("Snapshots");
    }

    [Fact]
    public async Task Generator_Produces_Clean_Output()
    {
        const string Source = """
                              using ErrorOr;


                              namespace CleanTest;

                              public static class CleanEndpoints
                              {
                                  [Get("/clean")]
                                  public static ErrorOr<string> Get() => "clean";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.IsClean();
    }

    [Fact]
    public async Task Generator_Compiles_Without_Errors()
    {
        const string Source = """
                              using ErrorOr;


                              namespace CompileTest;

                              public static class CompilableEndpoints
                              {
                                  [Get("/compile")]
                                  public static ErrorOr<int> Get() => 42;
                              }
                              """;

        using var result = await RunAsync(Source);

        result.Compiles();
    }

    [Fact]
    public async Task Generator_Caches_Output_When_Source_Unchanged()
    {
        const string Source = """
                              using ErrorOr;


                              namespace CachingTest;

                              public static class TestEndpoints
                              {
                                  [Get("/cached")]
                                  public static ErrorOr<string> Get() => "cached";
                              }
                              """;

        using var result = await RunAsync(Source);

        result.IsCached("EndpointBindingFlow");
    }
}
