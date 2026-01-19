using System.Threading.Tasks;
using Xunit;

namespace ErrorOrX.Generators.Tests;

public class RouteConstraintTests : GeneratorTestBase
{
    [Fact]
    public async Task Supports_Multiple_Constraints()
    {
        const string Source = """
                              using ErrorOr.Core.ErrorOr;
                              using ErrorOr.Endpoints;

                              namespace MyNamespace;

                              public static class MyEndpoints
                              {
                                  // id has two constraints: int and min(1)
                                  [Get("/test/{id:int:min(1)}")]
                                  public static ErrorOr<string> GetUser(int id) => "user_" + id;
                              }
                              """;

        await VerifyGeneratorAsync(Source);
    }

    [Fact]
    public async Task Routes_With_Different_Constraints_Are_Not_Duplicates()
    {
        const string Source = """
                              using ErrorOr.Core.ErrorOr;
                              using ErrorOr.Endpoints;

                              namespace MyNamespace;

                              public static class MyEndpoints
                              {
                                  [Get("/users/{id:int}")]
                                  public static ErrorOr<string> GetUserById(int id) => "1";

                                  [Get("/users/{id:alpha}")]
                                  public static ErrorOr<string> GetUserByName(string id) => "2";
                              }
                              """;

        // This test will likely FAIL currently because both normalize to /users/{_}
        await VerifyGeneratorAsync(Source);
    }

    [Fact]
    public async Task CatchAll_Routes_Are_Normalized_Correctly()
    {
        const string Source = """
                              using ErrorOr.Core.ErrorOr;
                              using ErrorOr.Endpoints;

                              namespace MyNamespace;

                              public static class MyEndpoints
                              {
                                  [Get("/files/{*path}")]
                                  public static ErrorOr<string> GetFile(string path) => "1";

                                  [Get("/files/{*filePath}")]
                                  public static ErrorOr<string> GetFileAgain(string filePath) => "2";
                              }
                              """;

        // These SHOULD be reported as duplicates
        await VerifyGeneratorAsync(Source);
    }
}
