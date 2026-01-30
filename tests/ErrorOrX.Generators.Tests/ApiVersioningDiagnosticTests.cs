namespace ErrorOrX.Generators.Tests;

/// <summary>
///     Tests for API versioning diagnostics (EOE027-EOE031).
///     Verifies that invalid versioning configurations are detected and reported.
/// </summary>
public class ApiVersioningDiagnosticTests : GeneratorTestBase
{
    [Fact]
    public Task EOE027_ApiVersionNeutral_With_MapToApiVersion_Reports_Warning()
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
    public Task EOE028_MapToApiVersion_Not_In_Declared_Versions_Reports_Warning()
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
    public Task EOE031_Invalid_ApiVersion_Format_V_Prefix_Reports_Error()
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
    public Task EOE031_Invalid_ApiVersion_Format_Semver_Reports_Error()
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

    [Fact]
    public Task EOE030_Endpoint_Missing_Versioning_In_Versioned_Project_Reports_Info()
    {
        // When project has versioned endpoints, unversioned endpoints should be warned
        const string Source = """
                              using ErrorOr;
                              using Asp.Versioning;

                              namespace DiagnosticTest;

                              [ApiVersion("1.0")]
                              public static class VersionedApi
                              {
                                  [Get("/versioned")]
                                  [MapToApiVersion("1.0")]
                                  public static ErrorOr<string> GetVersioned() => "versioned";
                              }

                              public static class UnversionedApi
                              {
                                  [Get("/unversioned")]
                                  public static ErrorOr<string> GetUnversioned() => "unversioned";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task EOE030_No_Diagnostic_When_No_Versioned_Endpoints()
    {
        // When no endpoints use versioning, don't warn about missing versioning
        const string Source = """
                              using ErrorOr;

                              namespace DiagnosticTest;

                              public static class Api1
                              {
                                  [Get("/api1")]
                                  public static ErrorOr<string> Get1() => "api1";
                              }

                              public static class Api2
                              {
                                  [Get("/api2")]
                                  public static ErrorOr<string> Get2() => "api2";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task EOE030_No_Diagnostic_When_ApiVersionNeutral()
    {
        // Endpoints marked [ApiVersionNeutral] should not trigger EOE030
        const string Source = """
                              using ErrorOr;
                              using Asp.Versioning;

                              namespace DiagnosticTest;

                              [ApiVersion("1.0")]
                              public static class VersionedApi
                              {
                                  [Get("/versioned")]
                                  [MapToApiVersion("1.0")]
                                  public static ErrorOr<string> GetVersioned() => "versioned";
                              }

                              public static class NeutralApi
                              {
                                  [Get("/neutral")]
                                  [ApiVersionNeutral]
                                  public static ErrorOr<string> GetNeutral() => "neutral";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task EOE029_ApiVersioning_Package_Not_Referenced_Reports_Warning()
    {
        // When the Asp.Versioning.Http package is not referenced, we detect versioning
        // attribute usage by name and warn that the package needs to be installed.
        // We define fake attributes with matching names AND run without the real package.
        const string Source = """
                              using ErrorOr;

                              namespace DiagnosticTest;

                              // Fake versioning attributes to simulate package not referenced
                              [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
                              public class ApiVersionAttribute : System.Attribute
                              {
                                  public ApiVersionAttribute(string version) { }
                              }

                              [ApiVersion("1.0")]
                              public static class TodoApi
                              {
                                  [Get("/todos")]
                                  public static ErrorOr<string> GetAll() => "todos";
                              }
                              """;

        // Use VerifyWithoutVersioningAsync to exclude the real Asp.Versioning package
        return VerifyWithoutVersioningAsync(Source);
    }
}
