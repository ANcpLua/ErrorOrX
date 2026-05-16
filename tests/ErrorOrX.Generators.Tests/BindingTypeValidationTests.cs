namespace ErrorOrX.Generators.Tests;

/// <summary>
///     Tests for parameter binding type validation diagnostics
///     (EOE010-EOE014, EOE016-EOE017). Verifies that invalid types used with
///     <c>[FromRoute]</c>, <c>[FromQuery]</c>, <c>[FromHeader]</c>, and
///     <c>[AsParameters]</c> are detected and reported.
/// </summary>
public class BindingTypeValidationTests : GeneratorTestBase
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
}
