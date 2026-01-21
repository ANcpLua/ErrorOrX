namespace ErrorOrX.Generators.Tests;

public class ErrorOrEndpointAnalyzerTests : AnalyzerTestBase<ErrorOrEndpointAnalyzer>
{
    private const string AttributesSource = """

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

    [Fact]
    public Task NonStaticHandler_ReportsDiagnostic()
    {
        const string Source = """
                              using ErrorOr.Core.ErrorOr;
                              using ErrorOr.Endpoints;

                              public class MyEndpoint
                              {
                                  [Get("/test")]
                                  public ErrorOr<string> {|EOE002:Get|}() => default;
                              }
                              """ + AttributesSource;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task InvalidReturnType_ReportsDiagnostic()
    {
        const string Source = """
                              using ErrorOr.Endpoints;

                              public class MyEndpoint
                              {
                                  [Get("/test")]
                                  public static string {|EOE001:Get|}() => "ok";
                              }
                              """ + AttributesSource;

        return VerifyAsync(Source);
    }
}
