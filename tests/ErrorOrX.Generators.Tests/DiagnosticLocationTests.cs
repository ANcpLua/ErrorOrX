using Microsoft.CodeAnalysis;

namespace ErrorOrX.Generators.Tests;

/// <summary>
///     Output-stage diagnostics (those computed over the cached <c>EndpointDescriptor</c> set, after symbols
///     are gone) must still point at the offending declaration. The descriptor carries a
///     <c>LocationInfo</c> snapshot captured in the transform stage; these tests pin that the diagnostics
///     surface a real, navigable source location instead of <see cref="Location.None" />.
/// </summary>
public class DiagnosticLocationTests : GeneratorTestBase
{
    [Fact]
    public async Task EOE004_Duplicate_Route_Points_At_Second_Handler()
    {
        const string Source = """
                              using ErrorOr;

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

        using var result = await RunAsync(Source);

        var diagnostic = result.Diagnostics.Single(static d => d.Id == "EOE004");
        AssertPointsAt(diagnostic, Source, "Get2");
    }

    [Fact]
    public async Task EOE030_Missing_Versioning_Points_At_Unversioned_Handler()
    {
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

        using var result = await RunAsync(Source);

        var diagnostic = result.Diagnostics.Single(static d => d.Id == "EOE030");
        AssertPointsAt(diagnostic, Source, "GetUnversioned");
    }

    [Fact]
    public async Task EOE022_Too_Many_Result_Types_Points_At_Handler()
    {
        const string Source = """
                              using ErrorOr;

                              namespace DiagnosticTest;

                              public static class TodoApi
                              {
                                  [Get("/todos/{id}")]
                                  public static ErrorOr<string> GetById(int id)
                                  {
                                      if (id == 0) return Error.NotFound("Todo.NotFound", "Not found");
                                      if (id == 1) return Error.Validation("Todo.Invalid", "Invalid");
                                      if (id == 2) return Error.Conflict("Todo.Conflict", "Conflict");
                                      if (id == 3) return Error.Unauthorized("Todo.Unauthorized", "Unauthorized");
                                      if (id == 4) return Error.Forbidden("Todo.Forbidden", "Forbidden");
                                      return $"todo {id}";
                                  }
                              }
                              """;

        using var result = await RunAsync(Source);

        var diagnostic = result.Diagnostics.Single(static d => d.Id == "EOE022");
        AssertPointsAt(diagnostic, Source, "GetById");
    }

    [Fact]
    public async Task EOE025_Missing_CamelCase_Policy_Points_At_User_Context()
    {
        const string Source = """
                              using ErrorOr;
                              using System.Text.Json.Serialization;

                              namespace DiagnosticTest;

                              public record Todo(int Id, string Title);

                              [JsonSerializable(typeof(Todo))]
                              internal partial class AppJsonContext : JsonSerializerContext { }

                              public static class TodoApi
                              {
                                  [Get("/todos/{id}")]
                                  public static ErrorOr<Todo> GetById(int id) => new Todo(id, "Title");
                              }
                              """;

        using var result = await RunAsync(Source);

        var diagnostic = result.Diagnostics.Single(static d => d.Id == "EOE025");
        AssertPointsAt(diagnostic, Source, "AppJsonContext");
    }

    [Fact]
    public async Task EOE007_Type_Not_In_Context_Points_At_Handler_That_Needs_It()
    {
        const string Source = """
                              using ErrorOr;
                              using System.Text.Json.Serialization;

                              namespace DiagnosticTest;

                              public record Todo(int Id, string Title);
                              public record Other(int Id);

                              [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
                              [JsonSerializable(typeof(Other))]
                              internal partial class AppJsonContext : JsonSerializerContext { }

                              public static class TodoApi
                              {
                                  [Get("/todos/{id}")]
                                  public static ErrorOr<Todo> GetById(int id) => new Todo(id, "Title");
                              }
                              """;

        using var result = await RunAsync(Source);

        var diagnostic = result.Diagnostics
            .Single(static d => d.Id == "EOE007" && d.GetMessage().Contains("Todo", StringComparison.Ordinal) &&
                                !d.GetMessage().Contains("ProblemDetails", StringComparison.Ordinal));
        AssertPointsAt(diagnostic, Source, "GetById");
    }

    /// <summary>
    ///     Asserts the diagnostic's mapped line/column lands on <paramref name="identifier" /> in
    ///     <paramref name="source" />. The output stage has no syntax tree, so the location is path-based
    ///     (<c>IsInSource == false</c>) but still carries the file path and line span an IDE navigates to
    ///     (the test harness parses without a path, so only the line span is asserted here).
    /// </summary>
    private static void AssertPointsAt(Diagnostic diagnostic, string source, string identifier)
    {
        diagnostic.Location.Should().NotBe(Location.None);

        var lineSpan = diagnostic.Location.GetLineSpan();

        var lines = source.Split('\n');
        var line = lines[lineSpan.StartLinePosition.Line];
        var column = lineSpan.StartLinePosition.Character;

        line.Substring(column).Should().StartWith(identifier,
            $"EOE diagnostic should point at '{identifier}' but points at line {lineSpan.StartLinePosition.Line + 1}: '{line.Trim()}'");
    }
}
