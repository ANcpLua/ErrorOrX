namespace ErrorOrX.Generators.Tests;

/// <summary>
///     Tests for route shape, body-source, and high-level handler validation
///     diagnostics (EOE003, EOE005-EOE006, EOE015, EOE018-EOE021). Covers
///     unbound route parameters, malformed route patterns, multiple body
///     sources, inaccessible or unsupported return types, generic type
///     parameters, route constraint mismatches, and ambiguous bindings.
/// </summary>
public class RouteBodyValidationTests : GeneratorTestBase
{
    #region EOE003 - Route parameter not bound

    [Fact]
    public Task EOE003_Route_Parameter_Not_Bound()
    {
        const string Source = """
                              using ErrorOr;

                              namespace DiagnosticTest;

                              public static class TodoApi
                              {
                                  [Get("/todos/{id}")]
                                  public static ErrorOr<string> GetById() => "todo";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task EOE003_Route_Parameter_With_Constraint_Not_Bound()
    {
        const string Source = """
                              using ErrorOr;

                              namespace DiagnosticTest;

                              public static class TodoApi
                              {
                                  [Get("/todos/{id:int}")]
                                  public static ErrorOr<string> GetById() => "todo";
                              }
                              """;

        return VerifyAsync(Source);
    }

    #endregion

    #region EOE005 - Invalid route pattern

    [Fact]
    public Task EOE005_Unclosed_Brace_In_Route()
    {
        const string Source = """
                              using ErrorOr;

                              namespace DiagnosticTest;

                              public static class TodoApi
                              {
                                  [Get("/todos/{id")]
                                  public static ErrorOr<string> GetById(int id) => "todo";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task EOE005_Unmatched_Close_Brace()
    {
        const string Source = """
                              using ErrorOr;

                              namespace DiagnosticTest;

                              public static class TodoApi
                              {
                                  [Get("/todos/id}")]
                                  public static ErrorOr<string> GetById(int id) => "todo";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task EOE005_Empty_Parameter_Name()
    {
        const string Source = """
                              using ErrorOr;

                              namespace DiagnosticTest;

                              public static class TodoApi
                              {
                                  [Get("/todos/{}")]
                                  public static ErrorOr<string> GetById() => "todo";
                              }
                              """;

        return VerifyAsync(Source);
    }

    #endregion

    #region EOE006 - Multiple body sources

    [Fact]
    public Task EOE006_Multiple_Body_Sources_FromBody_And_FromForm()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Http;
                              using Microsoft.AspNetCore.Mvc;

                              namespace DiagnosticTest;

                              public record CreateRequest(string Name);

                              public static class TodoApi
                              {
                                  [Post("/todos")]
                                  public static ErrorOr<string> Create(
                                      [FromBody] CreateRequest body,
                                      [FromForm] IFormFile file) => "created";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task EOE006_Multiple_Body_Sources_Stream_And_FromBody()
    {
        const string Source = """
                              using ErrorOr;
                              using System.IO;
                              using Microsoft.AspNetCore.Mvc;

                              namespace DiagnosticTest;

                              public record CreateRequest(string Name);

                              public static class TodoApi
                              {
                                  [Post("/upload")]
                                  public static ErrorOr<string> Upload(
                                      [FromBody] CreateRequest body,
                                      Stream data) => "uploaded";
                              }
                              """;

        return VerifyAsync(Source);
    }

    #endregion

    #region EOE015 - Anonymous return type not supported

    [Fact]
    public Task EOE015_Anonymous_Return_Type()
    {
        const string Source = """
                              using ErrorOr;

                              namespace DiagnosticTest;

                              public static class TodoApi
                              {
                                  [Get("/data")]
                                  public static ErrorOr<object> GetData() => new { Name = "test" };
                              }
                              """;

        return VerifyAsync(Source);
    }

    #endregion

    #region EOE018 - Inaccessible type in endpoint

    [Fact]
    public Task EOE018_Private_Return_Type()
    {
        const string Source = """
                              using ErrorOr;

                              namespace DiagnosticTest;

                              public static class TodoApi
                              {
                                  private class SecretData { public string Value { get; set; } }

                                  [Get("/secret")]
                                  public static ErrorOr<SecretData> GetSecret() => new SecretData { Value = "secret" };
                              }
                              """;

        return VerifyAsync(Source);
    }

    #endregion

    #region EOE019 - Type parameter not supported

    [Fact]
    public Task EOE019_Generic_Type_Parameter()
    {
        const string Source = """
                              using ErrorOr;

                              namespace DiagnosticTest;

                              public static class GenericApi
                              {
                                  [Get("/items")]
                                  public static ErrorOr<T> GetItem<T>() where T : class => default!;
                              }
                              """;

        return VerifyAsync(Source);
    }

    #endregion

    #region EOE020 - Route constraint type mismatch

    [Fact]
    public Task EOE020_Int_Constraint_With_String_Parameter()
    {
        const string Source = """
                              using ErrorOr;

                              namespace DiagnosticTest;

                              public static class TodoApi
                              {
                                  [Get("/todos/{id:int}")]
                                  public static ErrorOr<string> GetById(string id) => "todo";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task EOE020_Guid_Constraint_With_Int_Parameter()
    {
        const string Source = """
                              using ErrorOr;

                              namespace DiagnosticTest;

                              public static class TodoApi
                              {
                                  [Get("/todos/{id:guid}")]
                                  public static ErrorOr<string> GetById(int id) => "todo";
                              }
                              """;

        return VerifyAsync(Source);
    }

    #endregion

    #region EOE021 - Ambiguous parameter binding

    [Fact]
    public Task EOE021_Complex_Type_On_Get_Without_Binding()
    {
        const string Source = """
                              using ErrorOr;

                              namespace DiagnosticTest;

                              public class SearchFilter
                              {
                                  public string Query { get; set; }
                                  public int Page { get; set; }
                              }

                              public static class TodoApi
                              {
                                  [Get("/todos")]
                                  public static ErrorOr<string> Search(SearchFilter filter) => "todos";
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task EOE021_Complex_Type_On_Delete_Without_Binding()
    {
        const string Source = """
                              using ErrorOr;

                              namespace DiagnosticTest;

                              public class DeleteOptions
                              {
                                  public bool Force { get; set; }
                              }

                              public static class TodoApi
                              {
                                  [Delete("/todos/{id}")]
                                  public static ErrorOr<string> Delete(int id, DeleteOptions options) => "deleted";
                              }
                              """;

        return VerifyAsync(Source);
    }

    #endregion
}
