namespace ErrorOrX.Generators.Tests;

/// <summary>
///     Snapshot tests for API versioning code generation.
///     Verifies that [ApiVersion], [ApiVersionNeutral], and [MapToApiVersion] attributes
///     are correctly extracted and emitted in generated endpoint mappings.
/// </summary>
public class ApiVersioningTests : GeneratorTestBase
{
    [Fact]
    public Task Single_ApiVersion_On_Class_Emits_HasApiVersion()
    {
        const string Source = """
                              using ErrorOr;
                              using Asp.Versioning;

                              namespace VersioningTest;

                              [ApiVersion("1.0")]
                              public static class TodoApi
                              {
                                  [Get("/todos")]
                                  public static ErrorOr<string> GetAll() => "todos";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task Multiple_ApiVersions_On_Class_Emits_All_Versions()
    {
        const string Source = """
                              using ErrorOr;
                              using Asp.Versioning;

                              namespace VersioningTest;

                              [ApiVersion("1.0")]
                              [ApiVersion("2.0")]
                              public static class TodoApi
                              {
                                  [Get("/todos")]
                                  public static ErrorOr<string> GetAll() => "todos";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task ApiVersion_Major_Only_Emits_Single_Parameter_Constructor()
    {
        const string Source = """
                              using ErrorOr;
                              using Asp.Versioning;

                              namespace VersioningTest;

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
    public Task ApiVersionNeutral_On_Class_Emits_IsApiVersionNeutral()
    {
        const string Source = """
                              using ErrorOr;
                              using Asp.Versioning;

                              namespace VersioningTest;

                              [ApiVersionNeutral]
                              public static class HealthApi
                              {
                                  [Get("/health")]
                                  public static ErrorOr<string> Check() => "ok";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task MapToApiVersion_On_Method_Emits_MapToApiVersion()
    {
        const string Source = """
                              using ErrorOr;
                              using Asp.Versioning;

                              namespace VersioningTest;

                              [ApiVersion("1.0")]
                              [ApiVersion("2.0")]
                              public static class TodoApi
                              {
                                  [Get("/todos")]
                                  [MapToApiVersion("2.0")]
                                  public static ErrorOr<string> GetAll() => "todos v2";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task Deprecated_ApiVersion_Emits_HasDeprecatedApiVersion()
    {
        const string Source = """
                              using ErrorOr;
                              using Asp.Versioning;

                              namespace VersioningTest;

                              [ApiVersion("1.0", Deprecated = true)]
                              [ApiVersion("2.0")]
                              public static class TodoApi
                              {
                                  [Get("/todos")]
                                  public static ErrorOr<string> GetAll() => "todos";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task Multiple_MapToApiVersion_On_Method_Emits_All_Mappings()
    {
        const string Source = """
                              using ErrorOr;
                              using Asp.Versioning;

                              namespace VersioningTest;

                              [ApiVersion("1.0")]
                              [ApiVersion("2.0")]
                              [ApiVersion("3.0")]
                              public static class TodoApi
                              {
                                  [Get("/todos")]
                                  [MapToApiVersion("2.0")]
                                  [MapToApiVersion("3.0")]
                                  public static ErrorOr<string> GetAll() => "todos v2/v3";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task ApiVersionNeutral_On_Method_Overrides_Class_Version()
    {
        const string Source = """
                              using ErrorOr;
                              using Asp.Versioning;

                              namespace VersioningTest;

                              [ApiVersion("1.0")]
                              public static class TodoApi
                              {
                                  [Get("/todos")]
                                  public static ErrorOr<string> GetAll() => "todos";

                                  [Get("/health")]
                                  [ApiVersionNeutral]
                                  public static ErrorOr<string> Health() => "ok";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task No_Versioning_Attributes_Generates_No_Version_Calls()
    {
        const string Source = """
                              using ErrorOr;

                              namespace NoVersioning;

                              public static class TodoApi
                              {
                                  [Get("/todos")]
                                  public static ErrorOr<string> GetAll() => "todos";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task ApiVersionNeutral_On_Class_Applies_To_All_Methods()
    {
        const string Source = """
                              using ErrorOr;
                              using Asp.Versioning;

                              namespace NeutralTest;

                              [ApiVersionNeutral]
                              public static class HealthApi
                              {
                                  [Get("/health")]
                                  public static ErrorOr<string> Check() => "ok";

                                  [Get("/ready")]
                                  public static ErrorOr<string> Ready() => "ready";
                              }
                              """;

        return VerifyAsync(Source);
    }
}
