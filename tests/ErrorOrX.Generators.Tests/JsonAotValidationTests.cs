namespace ErrorOrX.Generators.Tests;

/// <summary>
///     Tests for JSON-serialization and AOT-safety diagnostics (EOE007,
///     EOE025, EOE034, EOE036). Covers missing types in the user's
///     <c>JsonSerializerContext</c>, missing CamelCase policy,
///     DataAnnotations validation that relies on reflection, and the absence
///     of error-payload types (<c>ProblemDetails</c>) in the user's context.
/// </summary>
public class JsonAotValidationTests : GeneratorTestBase
{
    #region EOE007 - Type not in JSON context

    [Fact]
    public Task EOE007_Type_Not_In_Json_Context()
    {
        const string Source = """
                              using ErrorOr;
                              using System.Text.Json.Serialization;

                              namespace DiagnosticTest;

                              public record Todo(int Id, string Title);
                              public record AnotherType(string Name);

                              // User context that's missing Todo
                              [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
                              [JsonSerializable(typeof(AnotherType))]
                              internal partial class AppJsonContext : JsonSerializerContext { }

                              public static class TodoApi
                              {
                                  [Get("/todos/{id}")]
                                  public static ErrorOr<Todo> GetById(int id) => new Todo(id, "Title");
                              }
                              """;

        return VerifyAsync(Source);
    }

    #endregion

    #region EOE025 - Missing CamelCase policy

    [Fact]
    public Task EOE025_Missing_CamelCase_Policy()
    {
        const string Source = """
                              using ErrorOr;
                              using System.Text.Json.Serialization;

                              namespace DiagnosticTest;

                              public record Todo(int Id, string Title);

                              // User context WITHOUT CamelCase policy
                              [JsonSerializable(typeof(Todo))]
                              internal partial class AppJsonContext : JsonSerializerContext { }

                              public static class TodoApi
                              {
                                  [Get("/todos/{id}")]
                                  public static ErrorOr<Todo> GetById(int id) => new Todo(id, "Title");
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task EOE025_With_CamelCase_Policy_No_Diagnostic()
    {
        const string Source = """
                              using ErrorOr;
                              using System.Text.Json.Serialization;

                              namespace DiagnosticTest;

                              public record Todo(int Id, string Title);

                              // User context WITH CamelCase policy
                              [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
                              [JsonSerializable(typeof(Todo))]
                              internal partial class AppJsonContext : JsonSerializerContext { }

                              public static class TodoApi
                              {
                                  [Get("/todos/{id}")]
                                  public static ErrorOr<Todo> GetById(int id) => new Todo(id, "Title");
                              }
                              """;

        return VerifyAsync(Source);
    }

    #endregion

    #region EOE034 - DataAnnotations validation uses reflection

    [Fact]
    public Task EOE034_Validation_Attribute_On_Parameter()
    {
        const string Source = """
                              using ErrorOr;
                              using System.ComponentModel.DataAnnotations;

                              namespace DiagnosticTest;

                              public record CreateTodoRequest([Required] string Title);

                              public static class TodoApi
                              {
                                  [Post("/todos")]
                                  public static ErrorOr<string> Create(CreateTodoRequest request) => "created";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task EOE034_Multiple_Validation_Attributes()
    {
        const string Source = """
                              using ErrorOr;
                              using System.ComponentModel.DataAnnotations;

                              namespace DiagnosticTest;

                              public static class TodoApi
                              {
                                  [Post("/todos")]
                                  public static ErrorOr<string> Create(
                                      [Required] [StringLength(100)] string title,
                                      [Range(1, 100)] int priority) => "created";
                              }
                              """;

        return VerifyAsync(Source);
    }

    #endregion

    #region EOE036 - JsonSerializerContext missing error types

    [Fact]
    public Task EOE036_Missing_ProblemDetails_In_JsonContext()
    {
        const string Source = """
                              using ErrorOr;
                              using System.Text.Json.Serialization;

                              namespace DiagnosticTest;

                              public record Todo(int Id, string Title);

                              // User context missing ProblemDetails types
                              [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
                              [JsonSerializable(typeof(Todo))]
                              internal partial class AppJsonContext : JsonSerializerContext { }

                              public static class TodoApi
                              {
                                  [Get("/todos/{id}")]
                                  public static ErrorOr<Todo> GetById(int id) => new Todo(id, "Title");
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task EOE036_No_Diagnostic_When_ProblemDetails_Present()
    {
        const string Source = """
                              using ErrorOr;
                              using System.Text.Json.Serialization;
                              using Microsoft.AspNetCore.Mvc;
                              using Microsoft.AspNetCore.Http;

                              namespace DiagnosticTest;

                              public record Todo(int Id, string Title);

                              // User context WITH ProblemDetails types
                              [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
                              [JsonSerializable(typeof(Todo))]
                              [JsonSerializable(typeof(ProblemDetails))]
                              [JsonSerializable(typeof(HttpValidationProblemDetails))]
                              internal partial class AppJsonContext : JsonSerializerContext { }

                              public static class TodoApi
                              {
                                  [Get("/todos/{id}")]
                                  public static ErrorOr<Todo> GetById(int id) => new Todo(id, "Title");
                              }
                              """;

        return VerifyAsync(Source);
    }

    #endregion
}
