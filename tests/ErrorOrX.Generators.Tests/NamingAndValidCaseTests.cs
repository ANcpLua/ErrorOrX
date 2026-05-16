namespace ErrorOrX.Generators.Tests;

/// <summary>
///     Tests for handler-method naming diagnostics (EOE033) plus baseline
///     valid-input cases that must NOT emit any diagnostics. The valid cases
///     guard against false positives in the other rule sets.
/// </summary>
public class NamingAndValidCaseTests : GeneratorTestBase
{
    #region EOE033 - Handler method name not PascalCase

    [Fact]
    public Task EOE033_Method_Name_Lowercase_Start()
    {
        const string Source = """
                              using ErrorOr;

                              namespace DiagnosticTest;

                              public static class TodoApi
                              {
                                  [Get("/todos/{id}")]
                                  public static ErrorOr<string> getById(int id) => $"todo {id}";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task EOE033_Method_Name_With_Underscore()
    {
        const string Source = """
                              using ErrorOr;

                              namespace DiagnosticTest;

                              public static class TodoApi
                              {
                                  [Get("/todos/{id}")]
                                  public static ErrorOr<string> Get_By_Id(int id) => $"todo {id}";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task EOE033_Method_Name_Snake_Case()
    {
        const string Source = """
                              using ErrorOr;

                              namespace DiagnosticTest;

                              public static class TodoApi
                              {
                                  [Get("/todos/{id}")]
                                  public static ErrorOr<string> get_by_id(int id) => $"todo {id}";
                              }
                              """;

        return VerifyAsync(Source);
    }

    #endregion

    #region Valid cases - no diagnostics

    [Fact]
    public Task Valid_Route_Parameter_Bound()
    {
        const string Source = """
                              using ErrorOr;

                              namespace DiagnosticTest;

                              public static class TodoApi
                              {
                                  [Get("/todos/{id}")]
                                  public static ErrorOr<string> GetById(int id) => $"todo {id}";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task Valid_Complex_Type_With_AsParameters()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Http;

                              namespace DiagnosticTest;

                              public class SearchFilter
                              {
                                  public string Query { get; set; }
                                  public int Page { get; set; }
                              }

                              public static class TodoApi
                              {
                                  [Get("/todos")]
                                  public static ErrorOr<string> Search([AsParameters] SearchFilter filter) => "todos";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task Valid_Complex_Type_With_FromBody_On_Post()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Mvc;

                              namespace DiagnosticTest;

                              public record CreateTodoRequest(string Title);

                              public static class TodoApi
                              {
                                  [Post("/todos")]
                                  public static ErrorOr<string> Create([FromBody] CreateTodoRequest request) => "created";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task Valid_Service_Type_Inferred()
    {
        const string Source = """
                              using ErrorOr;

                              namespace DiagnosticTest;

                              public interface ITodoService { }

                              public static class TodoApi
                              {
                                  [Get("/todos")]
                                  public static ErrorOr<string> GetAll(ITodoService service) => "todos";
                              }
                              """;

        return VerifyAsync(Source);
    }

    #endregion
}
