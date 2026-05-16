namespace ErrorOrX.Generators.Tests;

/// <summary>
///     Tests for results-union and error-factory diagnostics
///     (EOE022-EOE024). Covers unions with too many result types, unknown
///     <c>Error.*</c> factories, and undocumented interface calls that need an
///     explicit <c>[ProducesError]</c> attribute.
/// </summary>
public class UnionTypeAndFactoryTests : GeneratorTestBase
{
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
}
