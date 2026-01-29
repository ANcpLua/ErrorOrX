namespace ErrorOrX.Generators.Tests;

/// <summary>
///     Tests for API versioning diagnostics (EOE050-EOE054).
///     Verifies that invalid versioning configurations are detected and reported.
/// </summary>
public class ApiVersioningDiagnosticTests : GeneratorTestBase
{
    [Fact]
    public Task EOE050_ApiVersionNeutral_With_MapToApiVersion_Reports_Warning()
    {
        const string Source = """
                              using ErrorOr;
                              using Asp.Versioning;

                              namespace DiagnosticTest;

                              [ApiVersion("1.0")]
                              public static class TodoApi
                              {
                                  [Get("/todos")]
                                  [ApiVersionNeutral]
                                  [MapToApiVersion("1.0")]
                                  public static ErrorOr<string> GetAll() => "todos";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task EOE051_MapToApiVersion_Not_In_Declared_Versions_Reports_Warning()
    {
        const string Source = """
                              using ErrorOr;
                              using Asp.Versioning;

                              namespace DiagnosticTest;

                              [ApiVersion("1.0")]
                              public static class TodoApi
                              {
                                  [Get("/todos")]
                                  [MapToApiVersion("2.0")]
                                  public static ErrorOr<string> GetAll() => "todos";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task EOE054_Invalid_ApiVersion_Format_V_Prefix_Reports_Error()
    {
        const string Source = """
                              using ErrorOr;
                              using Asp.Versioning;

                              namespace DiagnosticTest;

                              [ApiVersion("v1")]
                              public static class TodoApi
                              {
                                  [Get("/todos")]
                                  public static ErrorOr<string> GetAll() => "todos";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task EOE054_Invalid_ApiVersion_Format_Semver_Reports_Error()
    {
        const string Source = """
                              using ErrorOr;
                              using Asp.Versioning;

                              namespace DiagnosticTest;

                              [ApiVersion("1.0.0")]
                              public static class TodoApi
                              {
                                  [Get("/todos")]
                                  public static ErrorOr<string> GetAll() => "todos";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task Valid_ApiVersion_Formats_Do_Not_Report_Diagnostics()
    {
        const string Source = """
                              using ErrorOr;
                              using Asp.Versioning;

                              namespace DiagnosticTest;

                              [ApiVersion("1.0")]
                              [ApiVersion("2")]
                              public static class TodoApi
                              {
                                  [Get("/todos")]
                                  public static ErrorOr<string> GetAll() => "todos";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task ApiVersion_With_Status_Suffix_Is_Valid()
    {
        const string Source = """
                              using ErrorOr;
                              using Asp.Versioning;

                              namespace DiagnosticTest;

                              [ApiVersion("1.0-beta")]
                              [ApiVersion("2.0-alpha")]
                              public static class TodoApi
                              {
                                  [Get("/todos")]
                                  public static ErrorOr<string> GetAll() => "todos";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task Multiple_Valid_MapToApiVersion_No_Diagnostics()
    {
        const string Source = """
                              using ErrorOr;
                              using Asp.Versioning;

                              namespace DiagnosticTest;

                              [ApiVersion("1.0")]
                              [ApiVersion("2.0")]
                              [ApiVersion("3.0")]
                              public static class TodoApi
                              {
                                  [Get("/todos")]
                                  [MapToApiVersion("1.0")]
                                  [MapToApiVersion("2.0")]
                                  public static ErrorOr<string> GetAll() => "todos";
                              }
                              """;

        return VerifyAsync(Source);
    }
}
