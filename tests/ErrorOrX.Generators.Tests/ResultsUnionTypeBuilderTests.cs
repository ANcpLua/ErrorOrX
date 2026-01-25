namespace ErrorOrX.Generators.Tests;

/// <summary>
/// Tests for ResultsUnionTypeBuilder behavior via generated code inspection.
/// Direct unit tests are not possible due to PolySharp polyfill collisions between
/// netstandard2.0 (generator) and net10.0 (tests).
/// </summary>
public class ResultsUnionTypeBuilderTests : GeneratorTestBase
{
    #region Success Response Tests

    [Fact]
    public async Task Payload_Returns_Ok200()
    {
        const string Source = """
            using ErrorOr;

            public static class Api
            {
                [Get("/todos/{id}")]
                public static ErrorOr<Todo> GetById(int id) => new Todo(id, "Test");
            }

            public record Todo(int Id, string Title);
            """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain("Ok<global::Todo>");
        generated.Should().Contain("TypedResults.Ok(result.Value)");
    }

    [Fact]
    public async Task Created_Returns_Created201()
    {
        const string Source = """
            using ErrorOr;
            using System.Text.Json.Serialization;
            using Microsoft.AspNetCore.Mvc;
            using Microsoft.AspNetCore.Http;

            public static class Api
            {
                [Post("/todos")]
                public static ErrorOr<Created> Create(CreateTodo cmd) => Result.Created;
            }

            public record CreateTodo(string Title);

            [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
            [JsonSerializable(typeof(CreateTodo))]
            [JsonSerializable(typeof(ProblemDetails))]
            [JsonSerializable(typeof(HttpValidationProblemDetails))]
            internal partial class TestJsonContext : JsonSerializerContext { }
            """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain("HttpResults.Created");
        generated.Should().Contain("TypedResults.Created");
    }

    [Fact]
    public async Task Deleted_Returns_NoContent204()
    {
        const string Source = """
            using ErrorOr;

            public static class Api
            {
                [Delete("/todos/{id}")]
                public static ErrorOr<Deleted> Delete(int id) => Result.Deleted;
            }
            """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain("NoContent");
        generated.Should().Contain("TypedResults.NoContent()");
    }

    [Fact]
    public async Task AcceptedResponse_Returns_Accepted202()
    {
        const string Source = """
            using ErrorOr;
            using System.Text.Json.Serialization;
            using Microsoft.AspNetCore.Mvc;
            using Microsoft.AspNetCore.Http;

            public static class Api
            {
                [Post("/jobs")]
                [AcceptedResponse]
                public static ErrorOr<Job> StartJob(StartJobCmd cmd) => new Job("job-123");
            }

            public record StartJobCmd(string Name);
            public record Job(string Id);

            [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
            [JsonSerializable(typeof(StartJobCmd))]
            [JsonSerializable(typeof(Job))]
            [JsonSerializable(typeof(ProblemDetails))]
            [JsonSerializable(typeof(HttpValidationProblemDetails))]
            internal partial class TestJsonContext : JsonSerializerContext { }
            """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain("Accepted<global::Job>");
        generated.Should().Contain("TypedResults.Accepted");
    }

    #endregion

    #region Union Type Tests

    [Fact]
    public async Task WithinMaxArity_ReturnsUnion()
    {
        const string Source = """
            using ErrorOr;

            public static class Api
            {
                [Get("/todos/{id}")]
                public static ErrorOr<Todo> GetById(int id) =>
                    id <= 0 ? Error.NotFound() : new Todo(id, "Test");
            }

            public record Todo(int Id, string Title);
            """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        // Should use Results<...> union type
        generated.Should().Contain("Results<");
        generated.Should().Contain("Ok<global::Todo>");
        generated.Should().Contain("NotFound<");
    }

    [Fact]
    public async Task ValidationError_UsesValidationProblem()
    {
        const string Source = """
            using ErrorOr;
            using System.Text.Json.Serialization;
            using Microsoft.AspNetCore.Mvc;
            using Microsoft.AspNetCore.Http;

            public static class Api
            {
                [Post("/todos")]
                public static ErrorOr<Todo> Create(CreateTodo cmd) =>
                    string.IsNullOrEmpty(cmd.Title)
                        ? Error.Validation("Title.Required", "Title is required")
                        : new Todo(1, cmd.Title);
            }

            public record CreateTodo(string Title);
            public record Todo(int Id, string Title);

            [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
            [JsonSerializable(typeof(CreateTodo))]
            [JsonSerializable(typeof(Todo))]
            [JsonSerializable(typeof(ProblemDetails))]
            [JsonSerializable(typeof(HttpValidationProblemDetails))]
            internal partial class TestJsonContext : JsonSerializerContext { }
            """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        // ValidationProblem is a separate type from BadRequest (both 400)
        generated.Should().Contain("ValidationProblem");
        generated.Should().Contain("BadRequest<");
    }

    #endregion

    #region Middleware-Induced Status Codes

    [Fact]
    public async Task WithAuthorization_Adds401And403()
    {
        const string Source = """
            using ErrorOr;
            using Microsoft.AspNetCore.Authorization;

            public static class Api
            {
                [Get("/admin/users")]
                [Authorize]
                public static ErrorOr<User[]> GetUsers() => Array.Empty<User>();
            }

            public record User(string Name);
            """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        generated.Should().Contain("UnauthorizedHttpResult");
        generated.Should().Contain("ForbidHttpResult");
        generated.Should().Contain(".RequireAuthorization()");
    }

    [Fact]
    public async Task WithRateLimiting_Adds429()
    {
        const string Source = """
            using ErrorOr;
            using Microsoft.AspNetCore.RateLimiting;

            public static class Api
            {
                [Get("/api/data")]
                [EnableRateLimiting("fixed")]
                public static ErrorOr<Data> GetData() => new Data("test");
            }

            public record Data(string Value);
            """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        // 429 uses StatusCodeHttpResult
        generated.Should().Contain("StatusCodeHttpResult");
        generated.Should().Contain(".RequireRateLimiting(");
    }

    [Fact]
    public async Task AllowAnonymous_OverridesAuthorization()
    {
        const string Source = """
            using ErrorOr;
            using Microsoft.AspNetCore.Authorization;

            public static class Api
            {
                [Get("/public/info")]
                [Authorize]
                [AllowAnonymous]
                public static ErrorOr<Info> GetInfo() => new Info("public");
            }

            public record Info(string Data);
            """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        // Should NOT contain auth-related types when AllowAnonymous overrides
        generated.Should().NotContain("UnauthorizedHttpResult");
        generated.Should().NotContain("ForbidHttpResult");
    }

    [Fact]
    public async Task DisableRateLimiting_OverridesEnableRateLimiting()
    {
        const string Source = """
            using ErrorOr;
            using Microsoft.AspNetCore.RateLimiting;

            public static class Api
            {
                [Get("/api/unlimited")]
                [EnableRateLimiting("fixed")]
                [DisableRateLimiting]
                public static ErrorOr<Data> GetData() => new Data("unlimited");
            }

            public record Data(string Value);
            """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        // Should NOT add 429 when DisableRateLimiting overrides
        // Count StatusCodeHttpResult - should only appear for inherent cases, not for 429
        var statusCodeCount = generated.Split("StatusCodeHttpResult").Length - 1;
        statusCodeCount.Should().BeLessThanOrEqualTo(1, "429 should not be added when DisableRateLimiting overrides");
    }

    #endregion

    #region Body Binding Tests

    [Fact]
    public async Task BodyBinding_Adds415UnsupportedMediaType()
    {
        const string Source = """
            using ErrorOr;
            using System.Text.Json.Serialization;
            using Microsoft.AspNetCore.Mvc;
            using Microsoft.AspNetCore.Http;

            public static class Api
            {
                [Post("/todos")]
                public static ErrorOr<Todo> Create(CreateTodo cmd) => new Todo(1, cmd.Title);
            }

            public record CreateTodo(string Title);
            public record Todo(int Id, string Title);

            [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
            [JsonSerializable(typeof(CreateTodo))]
            [JsonSerializable(typeof(Todo))]
            [JsonSerializable(typeof(ProblemDetails))]
            [JsonSerializable(typeof(HttpValidationProblemDetails))]
            internal partial class TestJsonContext : JsonSerializerContext { }
            """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        // POST with body adds 415 UnsupportedMediaType (uses StatusCodeHttpResult)
        generated.Should().Contain("StatusCodeHttpResult");
    }

    #endregion

    #region Always-Present Status Codes

    [Fact]
    public async Task AlwaysIncludes_400_And_500()
    {
        const string Source = """
            using ErrorOr;

            public static class Api
            {
                [Get("/simple")]
                public static ErrorOr<string> GetSimple() => "hello";
            }
            """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;
        // 400 BadRequest for binding failures
        generated.Should().Contain("BadRequest<");
        // 500 InternalServerError as safety net
        generated.Should().Contain("InternalServerError<");
    }

    #endregion

    #region Status Code Deduplication

    [Fact]
    public async Task DuplicateStatusCodes_AreDeduplicated()
    {
        const string Source = """
            using ErrorOr;

            public static class Api
            {
                [Get("/test")]
                public static ErrorOr<string> Test() =>
                    Random.Shared.Next(2) == 0
                        ? Error.Failure()
                        : Error.Unexpected();
            }
            """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;

        // Find the line with Results<...> type declaration - that's where deduplication should occur
        var lines = generated.Split('\n');
        var resultsLine = lines.FirstOrDefault(static l => l.Contains("Results<", StringComparison.Ordinal) &&
                                                     l.Contains("InternalServerError", StringComparison.Ordinal));

        resultsLine.Should().NotBeNull("should have a Results line with InternalServerError");

        // Count InternalServerError only in the Results type declaration line
        var internalServerErrorCount = resultsLine!.Split("InternalServerError").Length - 1;
        internalServerErrorCount.Should().Be(1, "500 status code should only appear once in Results<> type");
    }

    #endregion

    #region Status Code Sorting

    [Fact]
    public async Task StatusCodes_AreSortedCorrectly()
    {
        const string Source = """
            using ErrorOr;

            public static class Api
            {
                [Get("/todos/{id}")]
                public static ErrorOr<Todo> GetById(int id) =>
                    id == 0 ? Error.Conflict() :
                    id < 0 ? Error.NotFound() :
                    new Todo(id, "Test");
            }

            public record Todo(int Id, string Title);
            """;

        using var result = await RunAsync(Source);

        result.Diagnostics.Should().BeEmpty();
        var generated = result.Files.First(static f => f.HintName == "ErrorOrEndpointMappings.cs").Content;

        // 200 should come before 400
        var okIndex = generated.IndexOf("Ok<", StringComparison.Ordinal);
        var badRequestIndex = generated.IndexOf("BadRequest<", StringComparison.Ordinal);
        okIndex.Should().BeLessThan(badRequestIndex, "2xx should come before 4xx");

        // 404 should come before 409
        var notFoundIndex = generated.IndexOf("NotFound<", StringComparison.Ordinal);
        var conflictIndex = generated.IndexOf("Conflict<", StringComparison.Ordinal);
        notFoundIndex.Should().BeLessThan(conflictIndex, "404 should come before 409");
    }

    #endregion
}
