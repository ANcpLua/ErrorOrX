namespace ErrorOrX.Generators.Tests;

/// <summary>
///     Tests for generator diagnostics (EOE003-EOE038).
///     Verifies that invalid endpoint configurations are detected and reported.
/// </summary>
public class DiagnosticTests : GeneratorTestBase
{
    #region EOE010 - Invalid [FromRoute] type

    [Fact]
    public Task EOE010_Invalid_FromRoute_Type_Complex()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Mvc;

                              namespace DiagnosticTest;

                              public class ComplexFilter { public string Name { get; set; } }

                              public static class TodoApi
                              {
                                  [Get("/todos/{filter}")]
                                  public static ErrorOr<string> GetByFilter([FromRoute] ComplexFilter filter) => "todo";
                              }
                              """;

        return VerifyAsync(Source);
    }

    #endregion

    #region EOE011 - Invalid [FromQuery] type

    [Fact]
    public Task EOE011_Invalid_FromQuery_Type_Complex()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Mvc;

                              namespace DiagnosticTest;

                              public class ComplexFilter { public string Name { get; set; } }

                              public static class TodoApi
                              {
                                  [Get("/todos")]
                                  public static ErrorOr<string> Search([FromQuery] ComplexFilter filter) => "todos";
                              }
                              """;

        return VerifyAsync(Source);
    }

    #endregion

    #region EOE012 - Invalid [AsParameters] type

    [Fact]
    public Task EOE012_Invalid_AsParameters_Type_Primitive()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Http;

                              namespace DiagnosticTest;

                              public static class TodoApi
                              {
                                  [Get("/todos")]
                                  public static ErrorOr<string> Search([AsParameters] int page) => "todos";
                              }
                              """;

        return VerifyAsync(Source);
    }

    #endregion

    #region EOE013 - [AsParameters] type has no constructor

    [Fact]
    public Task EOE013_AsParameters_No_Constructor()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Http;

                              namespace DiagnosticTest;

                              public class SearchParams
                              {
                                  private SearchParams() { }
                                  public string Query { get; set; }
                              }

                              public static class TodoApi
                              {
                                  [Get("/todos")]
                                  public static ErrorOr<string> Search([AsParameters] SearchParams search) => "todos";
                              }
                              """;

        return VerifyAsync(Source);
    }

    #endregion

    #region EOE014 - Invalid [FromHeader] type

    [Fact]
    public Task EOE014_Invalid_FromHeader_Type_Complex()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Mvc;

                              namespace DiagnosticTest;

                              public class ComplexHeader { public string Value { get; set; } }

                              public static class TodoApi
                              {
                                  [Get("/todos")]
                                  public static ErrorOr<string> GetAll([FromHeader] ComplexHeader header) => "todos";
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

    #region EOE016 - Nested [AsParameters] not supported

    [Fact]
    public Task EOE016_Nested_AsParameters()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Http;

                              namespace DiagnosticTest;

                              public class InnerParams
                              {
                                  public int Page { get; set; }
                              }

                              public class OuterParams
                              {
                                  [AsParameters]
                                  public InnerParams Inner { get; set; }
                              }

                              public static class TodoApi
                              {
                                  [Get("/todos")]
                                  public static ErrorOr<string> Search([AsParameters] OuterParams search) => "todos";
                              }
                              """;

        return VerifyAsync(Source);
    }

    #endregion

    #region EOE017 - Nullable [AsParameters] not supported

    [Fact]
    public Task EOE017_Nullable_AsParameters()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Http;

                              namespace DiagnosticTest;

                              public class SearchParams
                              {
                                  public string Query { get; set; }
                              }

                              public static class TodoApi
                              {
                                  [Get("/todos")]
                                  public static ErrorOr<string> Search([AsParameters] SearchParams? search) => "todos";
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

    #region EOE023 - Unknown error factory

    [Fact]
    public Task EOE023_Unknown_Error_Factory()
    {
        const string Source = """
                              using ErrorOr;

                              namespace DiagnosticTest;

                              public static class TodoApi
                              {
                                  [Get("/todos/{id}")]
                                  public static ErrorOr<string> GetById(int id)
                                  {
                                      if (id < 0)
                                          return Error.Custom(999, "custom", "description");
                                      return "todo";
                                  }
                              }
                              """;

        return VerifyAsync(Source);
    }

    #endregion

    #region EOE022 - Too many result types

    [Fact]
    public Task EOE022_Too_Many_Result_Types()
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
                                      if (id == 5) return Error.Failure("Todo.Failure", "Failure");
                                      if (id == 6) return Error.Unexpected("Todo.Unexpected", "Unexpected");
                                      return $"todo {id}";
                                  }
                              }
                              """;

        return VerifyAsync(Source);
    }

    #endregion

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

    #region EOE024 - Undocumented interface call

    [Fact]
    public Task EOE024_Undocumented_Interface_Call()
    {
        const string Source = """
                              using ErrorOr;

                              namespace DiagnosticTest;

                              public interface ITodoService
                              {
                                  ErrorOr<string> GetById(int id);
                              }

                              public static class TodoApi
                              {
                                  [Get("/todos/{id}")]
                                  public static ErrorOr<string> GetById(int id, ITodoService svc)
                                      => svc.GetById(id);
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task EOE024_Interface_Call_With_ProducesError_No_Diagnostic()
    {
        const string Source = """
                              using ErrorOr;

                              namespace DiagnosticTest;

                              public interface ITodoService
                              {
                                  ErrorOr<string> GetById(int id);
                              }

                              public static class TodoApi
                              {
                                  [Get("/todos/{id}")]
                                  [ProducesError(404, "NotFound")]
                                  public static ErrorOr<string> GetById(int id, ITodoService svc)
                                      => svc.GetById(id);
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

    #region EOE039 - DataAnnotations validation uses reflection

    [Fact]
    public Task EOE039_Validation_Attribute_On_Parameter()
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
    public Task EOE039_Multiple_Validation_Attributes()
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

    #region EOE041 - JsonSerializerContext missing error types

    [Fact]
    public Task EOE041_Missing_ProblemDetails_In_JsonContext()
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
    public Task EOE041_No_Diagnostic_When_ProblemDetails_Present()
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
