using ErrorOr.Endpoints.Analyzers;

namespace ErrorOr.Endpoints.Tests;

public class ErrorOrEndpointCodeFixTests : CodeFixTestBase<ErrorOrEndpointAnalyzer, ErrorOrEndpointCodeFixProvider>
{
    [Fact]
    public async Task BlockBody_ConvertsToExpressionBody()
    {
        var source = """
                     using ErrorOr.Core.ErrorOr;
                     using ErrorOr.Endpoints;

                     public class MyEndpoint
                     {
                         [Get("/test")]
                         public static ErrorOr<string> {|EOE025:Get|}()
                         {
                             return default;
                         }
                     }

                     namespace ErrorOr.Endpoints
                     {
                         [System.AttributeUsage(System.AttributeTargets.Method)]
                         public class GetAttribute : System.Attribute { public GetAttribute(string route) {} }
                     }

                     namespace ErrorOr.Core.ErrorOr
                     {
                         public struct ErrorOr<TValue> {}
                     }
                     """;

        var fixedSource = """
                          using ErrorOr.Core.ErrorOr;
                          using ErrorOr.Endpoints;

                          public class MyEndpoint
                          {
                              [Get("/test")]
                              public static ErrorOr<string> Get() => default;
                          }

                          namespace ErrorOr.Endpoints
                          {
                              [System.AttributeUsage(System.AttributeTargets.Method)]
                              public class GetAttribute : System.Attribute { public GetAttribute(string route) {} }
                          }

                          namespace ErrorOr.Core.ErrorOr
                          {
                              public struct ErrorOr<TValue> {}
                          }
                          """;

        await VerifyCodeFixAsync(source, fixedSource);
    }
}
