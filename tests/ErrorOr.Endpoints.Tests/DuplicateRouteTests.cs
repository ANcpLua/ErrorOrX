using ErrorOr.Endpoints.Generators;

namespace ErrorOr.Endpoints.Tests;

public class DuplicateRouteTests : GeneratorTestBase
{
    [Fact]
    public async Task Reports_Duplicate_Route_Across_Classes()
    {
        var source = """
                     using System;
                     using ErrorOr.Core.ErrorOr;
                     using ErrorOr.Endpoints;

                     namespace ErrorOr.Endpoints
                     {
                         [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
                         public class GetAttribute(string pattern) : Attribute { }
                     }

                     namespace MyNamespace;

                     public static class Endpoints1
                     {
                         [Get("/test")]
                         public static ErrorOr<string> Get1() => "1";
                     }

                     public static class Endpoints2
                     {
                         [Get("/test")]
                         public static ErrorOr<string> Get2() => "2";
                     }
                     """;
        var compilation = CreateCompilation(source);

        await VerifyGeneratorAsync(compilation, new ErrorOrEndpointGenerator());
    }

    [Fact]
    public async Task Reports_Duplicate_Route_With_Different_Parameter_Names()
    {
        var source = """
                     using System;
                     using ErrorOr.Core.ErrorOr;
                     using ErrorOr.Endpoints;

                     namespace ErrorOr.Endpoints
                     {
                         [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
                         public class GetAttribute(string pattern) : Attribute { }
                     }

                     namespace MyNamespace;

                     public static class Endpoints1
                     {
                         [Get("/users/{id}")]
                         public static ErrorOr<string> Get1(int id) => "1";
                     }

                     public static class Endpoints2
                     {
                         [Get("/users/{userId}")]
                         public static ErrorOr<string> Get2(int userId) => "2";
                     }
                     """;
        var compilation = CreateCompilation(source);

        await VerifyGeneratorAsync(compilation, new ErrorOrEndpointGenerator());
    }
}
