namespace ErrorOrX.Generators.Tests;

/// <summary>
///     Regression tests for identified bugs in ErrorOrX.Generators.
///     These tests verify the fixes remain effective and prevent regressions.
/// </summary>
public class BugRegressionTests : GeneratorTestBase
{
    #region BUG-004: BuildRouteParameterLookup - Duplicate Route Parameter Names

    [Fact]
    public Task BUG004_Duplicate_Route_Parameter_Names_First_Wins()
    {
        const string Source = """
                              using ErrorOr;
                              using Microsoft.AspNetCore.Mvc;

                              public static class FileApi
                              {
                                  // Both parameters bind to route parameter 'id' via explicit attribute
                                  // First parameter should win for deterministic behavior
                                  [Get("/files/{id}")]
                                  public static ErrorOr<FileInfo> Get(
                                      [FromRoute(Name = "id")] int fileId,
                                      [FromRoute(Name = "id")] string category)
                                  {
                                      return new FileInfo(fileId, category);
                                  }
                              }

                              public record FileInfo(int Id, string Category);
                              """;

        // Should handle duplicate route parameter names without crashing
        // First parameter wins in lookup dictionary
        return VerifyAsync(Source);
    }

    #endregion

    #region BUG-005: GroupAggregator - Empty Endpoints Defensive Check

    [Fact]
    public Task BUG005_Route_Group_With_Valid_Endpoint_Works()
    {
        const string Source = """
                              using ErrorOr;

                              [RouteGroup("/api/v1", ApiName = "TodosApi")]
                              public static class TodoApi
                              {
                                  [Get("/todos")]
                                  public static ErrorOr<string> GetAll() => "todos";
                              }
                              """;

        // Should work correctly with non-empty route group
        return VerifyAsync(Source);
    }

    #endregion

    #region Combined Scenarios

    [Fact]
    public Task Combined_Local_Variables_In_Route_Group()
    {
        const string Source = """
                              using ErrorOr;

                              [RouteGroup("/api/v1", ApiName = "TodosApi")]
                              public static class TodoApi
                              {
                                  [Get("/todos/{id}")]
                                  public static ErrorOr<Todo> GetById(int id)
                                  {
                                      var notFound = Error.NotFound();  // BUG-001
                                      return id <= 0 ? notFound : new Todo(id, "Test");
                                  }
                              }

                              public record Todo(int Id, string Title);
                              """;

        // Tests BUG-001 within route group context
        return VerifyAsync(Source);
    }

    #endregion

    #region BUG-001: TryGetReferencedSymbol - Local Variable Handling

    [Fact]
    public Task BUG001_Local_Variable_Error_Factory_Does_Not_Crash()
    {
        const string Source = """
                              using ErrorOr;

                              public static class TodoApi
                              {
                                  [Get("/todos/{id}")]
                                  public static ErrorOr<Todo> GetById(int id)
                                  {
                                      // Local variable referencing Error factory
                                      var notFoundError = Error.NotFound();
                                      return id <= 0 ? notFoundError : new Todo(id, "Test");
                                  }
                              }

                              public record Todo(int Id, string Title);
                              """;

        // Should NOT crash - should detect NotFound error from local variable
        return VerifyAsync(Source);
    }

    [Fact]
    public Task BUG001_Multiple_Local_Variables_With_Different_Errors()
    {
        const string Source = """
                              using ErrorOr;

                              public static class UserApi
                              {
                                  [Get("/users/{id}")]
                                  public static ErrorOr<User> GetById(int id)
                                  {
                                      var notFound = Error.NotFound();
                                      var unauthorized = Error.Unauthorized();
                                      var forbidden = Error.Forbidden();

                                      return id switch
                                      {
                                          0 => notFound,
                                          -1 => unauthorized,
                                          -2 => forbidden,
                                          _ => new User(id, "User")
                                      };
                                  }
                              }

                              public record User(int Id, string Name);
                              """;

        // Should detect all error types from local variables
        return VerifyAsync(Source);
    }

    #endregion
}
