namespace ErrorOrX.Generators.Tests;

public class DuplicateRouteTests : GeneratorTestBase
{
    [Fact]
    public Task Reports_Duplicate_Route_Across_Classes()
    {
        const string Source = """
                              using System;
                              using ErrorOr;
                              

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

        return VerifyAsync(Source);
    }

    [Fact]
    public Task Reports_Duplicate_Route_With_Different_Parameter_Names()
    {
        const string Source = """
                              using System;
                              using ErrorOr;
                              

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

        return VerifyAsync(Source);
    }
}
