namespace ErrorOrX.Generators.Tests;

public class ErrorOrEndpointAnalyzerTests : AnalyzerTestBase<ErrorOrEndpointAnalyzer>
{
    private const string AttributesSource = """

                                            namespace ErrorOr
                                            {
                                                [System.AttributeUsage(System.AttributeTargets.Method)]
                                                public class GetAttribute : System.Attribute { public GetAttribute(string route) {} }

                                                public struct ErrorOr<TValue> {}
                                            }
                                            """;

    [Fact]
    public Task NonStaticHandler_ReportsDiagnostic()
    {
        const string Source = """
                              using ErrorOr;

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
                              using ErrorOr;

                              public class MyEndpoint
                              {
                                  [Get("/test")]
                                  public static string {|EOE001:Get|}() => "ok";
                              }
                              """ + AttributesSource;

        return VerifyAsync(Source);
    }
}
