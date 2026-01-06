// ═══════════════════════════════════════════════════════════════════════════
// ErrorOr.Http.Bcl Comprehensive Test Suite
// Tests all BCL Minimal API scenarios and diagnostic coverage
// ═══════════════════════════════════════════════════════════════════════════

using System.Runtime.CompilerServices;
using ErrorOr.Http;
using ErrorOr.Http.Bcl.Sample.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ErrorOr.Http.Bcl.Sample.Tests;

// ═══════════════════════════════════════════════════════════════════════════
// ROUTE CONSTRAINT TYPE VALIDATION (EOE023)
// ═══════════════════════════════════════════════════════════════════════════

public static class RouteConstraintTests
{
    // ✅ Should pass - constraint matches type
    [Get("/test/constraint/int/{id:int}")]
    public static ErrorOr<int> ConstraintInt(int id) => id;

    [Get("/test/constraint/long/{id:long}")]
    public static ErrorOr<long> ConstraintLong(long id) => id;

    [Get("/test/constraint/guid/{id:guid}")]
    public static ErrorOr<Guid> ConstraintGuid(Guid id) => id;

    [Get("/test/constraint/bool/{flag:bool}")]
    public static ErrorOr<bool> ConstraintBool(bool flag) => flag;

    [Get("/test/constraint/decimal/{value:decimal}")]
    public static ErrorOr<decimal> ConstraintDecimal(decimal value) => value;

    [Get("/test/constraint/double/{value:double}")]
    public static ErrorOr<double> ConstraintDouble(double value) => value;

    [Get("/test/constraint/float/{value:float}")]
    public static ErrorOr<float> ConstraintFloat(float value) => value;

    [Get("/test/constraint/datetime/{dt:datetime}")]
    public static ErrorOr<DateTime> ConstraintDateTime(DateTime dt) => dt;

    // .NET 6+ date/time types
    [Get("/test/constraint/dateonly/{date:dateonly}")]
    public static ErrorOr<DateOnly> ConstraintDateOnly(DateOnly date) => date;

    [Get("/test/constraint/timeonly/{time:timeonly}")]
    public static ErrorOr<TimeOnly> ConstraintTimeOnly(TimeOnly time) => time;

    [Get("/test/constraint/timespan/{duration:timespan}")]
    public static ErrorOr<TimeSpan> ConstraintTimeSpan(TimeSpan duration) => duration;

    [Get("/test/constraint/datetimeoffset/{dt:datetimeoffset}")]
    public static ErrorOr<DateTimeOffset> ConstraintDateTimeOffset(DateTimeOffset dt) => dt;

    // Optional constraints with nullable types
    [Get("/test/constraint/optional-int/{id:int?}")]
    public static ErrorOr<int> ConstraintOptionalInt(int? id) => id ?? -1;

    [Get("/test/constraint/optional-guid/{id:guid?}")]
    public static ErrorOr<Guid> ConstraintOptionalGuid(Guid? id) => id ?? Guid.Empty;

    // Alpha constraint (string only)
    [Get("/test/constraint/alpha/{name:alpha}")]
    public static ErrorOr<string> ConstraintAlpha(string name) => name;

    // ❌ Should trigger EOE023 - constraint type mismatch
    // Uncomment to test diagnostics:
    // [Get("/test/constraint/mismatch-int/{id:int}")]
    // public static ErrorOr<string> ConstraintMismatchInt(string id) => id;

    // [Get("/test/constraint/mismatch-guid/{id:guid}")]
    // public static ErrorOr<string> ConstraintMismatchGuid(string id) => id;
}

// ═══════════════════════════════════════════════════════════════════════════
// CATCH-ALL PARAMETERS (EOE024)
// ═══════════════════════════════════════════════════════════════════════════

public static class CatchAllTests
{
    // ✅ Should pass - catch-all is string
    [Get("/test/catchall/valid/{*path}")]
    public static ErrorOr<string> CatchAllValid(string path) => path ?? "/";

    // ✅ Catch-all at different positions
    [Get("/api/files/{*filePath}")]
    public static ErrorOr<string> CatchAllFilePath(string filePath) => filePath;

    // ❌ Should trigger EOE024 - catch-all must be string
    // Uncomment to test diagnostics:
    // [Get("/test/catchall/invalid/{*path}")]
    // public static ErrorOr<int> CatchAllInvalid(int path) => path;
}

// ═══════════════════════════════════════════════════════════════════════════
// PARAMETER BINDING TESTS (EOE011-016)
// ═══════════════════════════════════════════════════════════════════════════

public static class ParameterBindingTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // [FromQuery] Tests
    // ═══════════════════════════════════════════════════════════════════════

    // ✅ Primitives
    [Get("/test/query/string")]
    public static ErrorOr<string> QueryString([FromQuery] string search) => search;

    [Get("/test/query/int")]
    public static ErrorOr<int> QueryInt([FromQuery] int page) => page;

    [Get("/test/query/nullable-int")]
    public static ErrorOr<int> QueryNullableInt([FromQuery] int? page) => page ?? 1;

    // ✅ Collections of primitives
    [Get("/test/query/int-array")]
    public static ErrorOr<int[]> QueryIntArray([FromQuery] int[] ids) => ids;

    [Get("/test/query/string-list")]
    public static ErrorOr<List<string>> QueryStringList([FromQuery] List<string> tags) => tags;

    [Get("/test/query/guid-array")]
    public static ErrorOr<Guid[]> QueryGuidArray([FromQuery] Guid[] ids) => ids;

    // ❌ Should trigger EOE012 - complex type not allowed
    // Uncomment to test diagnostics:
    // [Get("/test/query/invalid-complex")]
    // public static ErrorOr<Todo> QueryInvalidComplex([FromQuery] Todo todo) => todo;

    // ═══════════════════════════════════════════════════════════════════════
    // [FromHeader] Tests
    // ═══════════════════════════════════════════════════════════════════════

    // ✅ String headers
    [Get("/test/header/string")]
    public static ErrorOr<string> HeaderString([FromHeader(Name = "X-Request-Id")] string requestId) => requestId;

    // ✅ Primitive headers (int, bool, etc.)
    [Get("/test/header/int")]
    public static ErrorOr<int> HeaderInt([FromHeader(Name = "X-Page-Size")] int pageSize) => pageSize;

    [Get("/test/header/bool")]
    public static ErrorOr<bool> HeaderBool([FromHeader(Name = "X-Include-Deleted")] bool includeDeleted) =>
        includeDeleted;

    // ✅ Collection headers
    [Get("/test/header/string-array")]
    public static ErrorOr<string[]> HeaderStringArray([FromHeader(Name = "X-Tags")] string[] tags) => tags;

    // ═══════════════════════════════════════════════════════════════════════
    // [FromRoute] Tests
    // ═══════════════════════════════════════════════════════════════════════

    // ✅ All primitive types
    [Get("/test/route/int/{id}")]
    public static ErrorOr<int> RouteInt([FromRoute] int id) => id;

    [Get("/test/route/guid/{id}")]
    public static ErrorOr<Guid> RouteGuid([FromRoute] Guid id) => id;

    [Get("/test/route/string/{slug}")]
    public static ErrorOr<string> RouteString([FromRoute] string slug) => slug;

    // ✅ Custom name binding
    [Get("/test/route/custom-name/{userId}")]
    public static ErrorOr<int> RouteCustomName([FromRoute(Name = "userId")] int id) => id;

    // ═══════════════════════════════════════════════════════════════════════
    // [AsParameters] Tests
    // ═══════════════════════════════════════════════════════════════════════

    // ✅ Valid DTO with public constructor
    [Get("/test/as-parameters/pagination")]
    public static ErrorOr<PaginationResult> AsParametersPagination([AsParameters] PaginationParams p) =>
        new(p.Page, p.PageSize, p.SortBy ?? "id");

    // ✅ Nested route parameters
    [Get("/test/as-parameters/with-route/{id}")]
    public static ErrorOr<string> AsParametersWithRoute([AsParameters] RouteAndQueryParams p) =>
        $"id={p.Id}, filter={p.Filter}";

    // ═══════════════════════════════════════════════════════════════════════
    // Implicit Binding Tests
    // ═══════════════════════════════════════════════════════════════════════

    // ✅ Implicit route binding (parameter name matches route)
    [Get("/test/implicit/route/{productId}")]
    public static ErrorOr<Guid> ImplicitRoute(Guid productId) => productId;

    // ✅ Implicit query binding (primitive not in route)
    [Get("/test/implicit/query")]
    public static ErrorOr<int> ImplicitQuery(int page, int pageSize) => page * pageSize;

    // ✅ HttpContext and CancellationToken (special types)
    [Get("/test/implicit/special-types")]
    public static ErrorOr<string> ImplicitSpecialTypes(HttpContext ctx, CancellationToken ct) =>
        ctx.Request.Path.Value ?? "/";

    // ✅ Service injection
    [Get("/test/implicit/service")]
    public static Task<ErrorOr<List<Todo>>> ImplicitService(
        [FromServices] ITodoService svc,
        CancellationToken ct) => svc.GetAllAsync(ct);
}

// ═══════════════════════════════════════════════════════════════════════════
// FORM FILE BINDING
// ═══════════════════════════════════════════════════════════════════════════

public static class FormFileTests
{
    // ✅ Single file
    [Post("/test/form/single-file")]
    public static ErrorOr<string> SingleFile(IFormFile file) =>
        $"Received: {file.FileName} ({file.Length} bytes)";

    // ✅ Multiple files
    [Post("/test/form/multiple-files")]
    public static ErrorOr<int> MultipleFiles(IFormFileCollection files) => files.Count;

    // ✅ File with form data
    [Post("/test/form/file-with-data")]
    public static ErrorOr<string> FileWithData(
        IFormFile file,
        [FromForm] string description,
        [FromForm] string category) =>
        $"{file.FileName}: {description} ({category})";

    // ✅ Complex form DTO
    [Post("/test/form/dto")]
    public static ErrorOr<string> FormDto([FromForm] FileUploadRequest request) =>
        $"{request.File.FileName}: {request.Title}";
}

// ═══════════════════════════════════════════════════════════════════════════
// SSE / STREAMING TESTS (EOE040-041)
// ═══════════════════════════════════════════════════════════════════════════

public static class SseTests
{
    // ✅ Basic SSE endpoint - may trigger EOE040 info
    [Get("/test/sse/basic")]
    public static async Task<ErrorOr<IAsyncEnumerable<string>>> BasicSse(CancellationToken ct)
    {
        return StreamEvents(ct);

        static async IAsyncEnumerable<string> StreamEvents(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 0; i < 10; i++)
            {
                yield return $"Event {i}";
                await Task.Delay(100, cancellationToken);
            }
        }
    }

    // ⚠️ SSE with body - should trigger EOE041 warning
    // [Post("/test/sse/with-body")]
    // public static async Task<ErrorOr<IAsyncEnumerable<string>>> SseWithBody(
    //     [FromBody] SseRequest request,
    //     CancellationToken ct) => ...
}

// ═══════════════════════════════════════════════════════════════════════════
// RESULTS UNION EDGE CASES
// ═══════════════════════════════════════════════════════════════════════════

public static class ResultsUnionTests
{
    // ✅ All 7 ErrorOr error types
    [Get("/test/erroror/failure")]
    public static ErrorOr<string> TestFailure() =>
        Error.Failure("Test.Failure", "A failure occurred");

    [Get("/test/erroror/unexpected")]
    public static ErrorOr<string> TestUnexpected() =>
        Error.Unexpected("Test.Unexpected", "Something unexpected");

    [Get("/test/erroror/validation")]
    public static ErrorOr<string> TestValidation() =>
        Error.Validation("Test.Validation", "Validation failed");

    [Get("/test/erroror/conflict")]
    public static ErrorOr<string> TestConflict() =>
        Error.Conflict("Test.Conflict", "Resource conflict");

    [Get("/test/erroror/notfound")]
    public static ErrorOr<string> TestNotFound() =>
        Error.NotFound("Test.NotFound", "Resource not found");

    [Get("/test/erroror/unauthorized")]
    public static ErrorOr<string> TestUnauthorized() =>
        Error.Unauthorized("Test.Unauthorized", "Not authorized");

    [Get("/test/erroror/forbidden")]
    public static ErrorOr<string> TestForbidden() =>
        Error.Forbidden("Test.Forbidden", "Access forbidden");

    // ✅ Custom error WITH [ProducesError] - no warning
    [Post("/test/erroror/custom-documented")]
    [ProducesError(429, "RateLimit.Exceeded")]
    [ProducesError(402, "Payment.Required")]
    public static ErrorOr<string> TestCustomDocumented(bool triggerRateLimit, bool triggerPayment)
    {
        if (triggerRateLimit)
            return Error.Custom(429, "RateLimit.Exceeded", "Too many requests");
        if (triggerPayment)
            return Error.Custom(402, "Payment.Required", "Payment required");
        return "Success";
    }

    // ⚠️ Custom error WITHOUT [ProducesError] - should trigger EOE008
    [Post("/test/erroror/custom-undocumented")]
    public static ErrorOr<string> TestCustomUndocumented() =>
        Error.Custom(418, "Teapot.IAmOne", "I'm a teapot");

    // ✅ 202 Accepted response
    [Post("/test/erroror/accepted")]
    [AcceptedResponse]
    public static ErrorOr<JobReference> TestAccepted() =>
        new JobReference(Guid.NewGuid(), "/api/jobs/status");

    // ✅ Multiple error types in one endpoint
    [Put("/test/erroror/multi-error/{id:guid}")]
    public static ErrorOr<Todo> TestMultiError(Guid id, [FromBody] UpdateTodoRequest request)
    {
        if (id == Guid.Empty)
            return Error.Validation("Id.Invalid", "ID cannot be empty");
        if (request.Title.Length > 100)
            return Error.Validation("Title.TooLong", "Title exceeds 100 characters");
        if (id == new Guid("11111111-1111-1111-1111-111111111111"))
            return Error.NotFound("Todo.NotFound", $"Todo {id} not found");
        if (id == new Guid("22222222-2222-2222-2222-222222222222"))
            return Error.Conflict("Todo.AlreadyModified", "Todo was modified by another user");

        return new Todo(id, request.Title, request.DueBy, request.IsComplete);
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// MANUAL BCL RESULTS<...> COMPARISON
// ═══════════════════════════════════════════════════════════════════════════

public static class ManualBclComparison
{
    // This shows what the generator should produce - for comparison testing
    // Run both and compare OpenAPI output

    // Manual Results<...> with auto-metadata
    public static Results<Ok<Todo>, NotFound, ProblemHttpResult> ManualResultsUnion(Guid id)
    {
        if (id == Guid.Empty)
            return TypedResults.NotFound();
        if (id == new Guid("11111111-1111-1111-1111-111111111111"))
            return TypedResults.Problem("Something went wrong", statusCode: 500);
        return TypedResults.Ok(new Todo(id, "Test Todo"));
    }

    // Manual IResult with explicit metadata
    public static IResult ManualIResultWithMetadata(Guid id)
    {
        if (id == Guid.Empty)
            return TypedResults.NotFound();
        return TypedResults.Ok(new Todo(id, "Test Todo"));
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// SUPPORTING TYPES
// ═══════════════════════════════════════════════════════════════════════════

public record PaginationParams(int Page = 1, int PageSize = 20, string? SortBy = null);

public record PaginationResult(int Page, int PageSize, string SortBy);

public record RouteAndQueryParams(
    [FromRoute] Guid Id,
    [FromQuery] string? Filter = null);

public record FileUploadRequest(
    IFormFile File,
    [FromForm] string Title,
    [FromForm] string? Description = null);

public record SseRequest(string Topic, int? MaxEvents = null);

public record JobReference(Guid JobId, string StatusUrl);

// ═══════════════════════════════════════════════════════════════════════════
// CUSTOM TRYPARSE TYPE (for testing custom binding detection)
// ═══════════════════════════════════════════════════════════════════════════

public readonly struct CustomerId
{
    public int Value { get; }

    private CustomerId(int value) => Value = value;

    public static bool TryParse(string? s, out CustomerId result)
    {
        if (int.TryParse(s, out var value) && value > 0)
        {
            result = new CustomerId(value);
            return true;
        }

        result = default;
        return false;
    }

    public override string ToString() => Value.ToString();
}

public static class CustomTryParseTests
{
    // ✅ Custom type with TryParse - should be detected and allowed
    [Get("/test/custom-type/route/{id}")]
    public static ErrorOr<int> CustomTypeRoute(CustomerId id) => id.Value;

    [Get("/test/custom-type/query")]
    public static ErrorOr<int> CustomTypeQuery([FromQuery] CustomerId id) => id.Value;
}