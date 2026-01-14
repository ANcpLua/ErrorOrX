using ANcpLua.Roslyn.Utilities.Testing;

namespace ErrorOrX.Generators.Tests;

/// <summary>
///     Tests that verify the generator properly caches its outputs between compilations.
/// </summary>
public class GeneratorCachingTests : GeneratorTestBase
{
    [Fact]
    public async Task Generator_Produces_Output_For_Simple_Endpoint()
    {
        const string Source = """
                              using ErrorOr.Core.ErrorOr;
                              using ErrorOr.Endpoints;

                              namespace CachingTest;

                              public static class TestEndpoints
                              {
                                  [Get("/cached")]
                                  public static ErrorOr<string> Get() => "cached";
                              }
                              """;

        using var scope = TestConfiguration.WithAdditionalReferences(RequiredTypes);
        using var result = await Test<ErrorOrEndpointGenerator>.Run(Source, TestContext.Current.CancellationToken);

        // Verify generator produces output
        result.Files.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Generator_Produces_Output_For_Multiple_Endpoints()
    {
        const string Source = """
                              using ErrorOr.Core.ErrorOr;
                              using ErrorOr.Endpoints;

                              namespace CachingTest;

                              public static class MultipleEndpoints
                              {
                                  [Get("/one")]
                                  public static ErrorOr<string> GetOne() => "one";

                                  [Post("/two")]
                                  public static ErrorOr<int> PostTwo() => 42;

                                  [Delete("/three/{id}")]
                                  public static ErrorOr<bool> DeleteThree(int id) => true;
                              }
                              """;

        using var scope = TestConfiguration.WithAdditionalReferences(RequiredTypes);
        using var result = await Test<ErrorOrEndpointGenerator>.Run(Source, TestContext.Current.CancellationToken);

        result.Files.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Generator_Handles_Complex_Parameters()
    {
        const string Source = """
                              using ErrorOr.Core.ErrorOr;
                              using ErrorOr.Endpoints;
                              using Microsoft.AspNetCore.Mvc;

                              namespace CachingTest;

                              public record CreateRequest(string Name, int Value);
                              public record Response(int Id, string Name);

                              public static class ComplexEndpoints
                              {
                                  [Post("/items")]
                                  public static ErrorOr<Response> Create([FromBody] CreateRequest request)
                                      => new Response(1, request.Name);

                                  [Get("/items/{id}")]
                                  public static ErrorOr<Response> GetById(int id)
                                      => new Response(id, "item");
                              }
                              """;

        using var scope = TestConfiguration.WithAdditionalReferences(RequiredTypes);
        using var result = await Test<ErrorOrEndpointGenerator>.Run(Source, TestContext.Current.CancellationToken);

        result.Files.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Generator_Produces_Clean_Output()
    {
        const string Source = """
                              using ErrorOr.Core.ErrorOr;
                              using ErrorOr.Endpoints;

                              namespace CleanTest;

                              public static class CleanEndpoints
                              {
                                  [Get("/clean")]
                                  public static ErrorOr<string> Get() => "clean";
                              }
                              """;

        using var scope = TestConfiguration.WithAdditionalReferences(RequiredTypes);
        using var result = await Test<ErrorOrEndpointGenerator>.Run(Source, TestContext.Current.CancellationToken);

        // Verify no diagnostics are produced for valid code
        result.IsClean();
    }

    [Fact]
    public async Task Generator_Compiles_Without_Errors()
    {
        const string Source = """
                              using ErrorOr.Core.ErrorOr;
                              using ErrorOr.Endpoints;

                              namespace CompileTest;

                              public static class CompilableEndpoints
                              {
                                  [Get("/compile")]
                                  public static ErrorOr<int> Get() => 42;
                              }
                              """;

        using var scope = TestConfiguration.WithAdditionalReferences(RequiredTypes);
        using var result = await Test<ErrorOrEndpointGenerator>.Run(Source, TestContext.Current.CancellationToken);

        // Verify generated code compiles successfully
        result.Compiles();
    }

    [Fact]
    public async Task Generator_Caches_Output_When_Source_Unchanged()
    {
        const string Source = """
                              using ErrorOr.Core.ErrorOr;
                              using ErrorOr.Endpoints;

                              namespace CachingTest;

                              public static class TestEndpoints
                              {
                                  [Get("/cached")]
                                  public static ErrorOr<string> Get() => "cached";
                              }
                              """;

        using var scope = TestConfiguration.WithAdditionalReferences(RequiredTypes);
        using var result = await Test<ErrorOrEndpointGenerator>.Run(Source, TestContext.Current.CancellationToken);

        result.IsCached();
    }
}
