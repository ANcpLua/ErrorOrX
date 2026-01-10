// ═══════════════════════════════════════════════════════════════════════════
// ErrorOr.Endpoints Comprehensive Test Suite
// Tests all Minimal API scenarios and diagnostic coverage
// ═══════════════════════════════════════════════════════════════════════════

using ErrorOr.Core.Errors;
using ErrorOr.Endpoints.Sample.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ErrorOr.Endpoints.Sample;

// ═══════════════════════════════════════════════════════════════════════════
// ROUTE CONSTRAINT TYPE VALIDATION (EOE023)
// ═══════════════════════════════════════════════════════════════════════════

public static class RouteConstraintTests
{
    [Get("/users/{id}")]
    public static ErrorOr<string> GetUserById(string id)
    {
        return id;
    }

    // [Get("/users/{userId}")]
    // public static ErrorOr<string> GetUserByUserId(string userId) => userId;

    // [Get("/test/duplicate")]
    // public static ErrorOr<string> Duplicate2() => "2";

    // ✅ Should pass - constraint matches type
    [Get("/test/constraint/int/{id:int}")]
    public static ErrorOr<int> ConstraintInt(int id)
    {
        return id;
    }

    [Get("/test/constraint/long/{id:long}")]
    public static ErrorOr<long> ConstraintLong(long id)
    {
        return id;
    }

    [Get("/test/constraint/guid/{id:guid}")]
    public static ErrorOr<Guid> ConstraintGuid(Guid id)
    {
        return id;
    }

    [Get("/test/constraint/bool/{flag:bool}")]
    public static ErrorOr<bool> ConstraintBool(bool flag)
    {
        return flag;
    }

    [Get("/test/constraint/decimal/{value:decimal}")]
    public static ErrorOr<decimal> ConstraintDecimal(decimal value)
    {
        return value;
    }

    [Get("/test/constraint/double/{value:double}")]
    public static ErrorOr<double> ConstraintDouble(double value)
    {
        return value;
    }

    [Get("/test/constraint/float/{value:float}")]
    public static ErrorOr<float> ConstraintFloat(float value)
    {
        return value;
    }

    [Get("/test/constraint/datetime/{dt:datetime}")]
    public static ErrorOr<DateTime> ConstraintDateTime(DateTime dt)
    {
        return dt;
    }

    // .NET 6+ date/time types
    [Get("/test/constraint/dateonly/{date:dateonly}")]
    public static ErrorOr<DateOnly> ConstraintDateOnly(DateOnly date)
    {
        return date;
    }

    [Get("/test/constraint/timeonly/{time:timeonly}")]
    public static ErrorOr<TimeOnly> ConstraintTimeOnly(TimeOnly time)
    {
        return time;
    }

    [Get("/test/constraint/timespan/{duration:timespan}")]
    public static ErrorOr<TimeSpan> ConstraintTimeSpan(TimeSpan duration)
    {
        return duration;
    }

    [Get("/test/constraint/datetimeoffset/{dt:datetimeoffset}")]
    public static ErrorOr<DateTimeOffset> ConstraintDateTimeOffset(DateTimeOffset dt)
    {
        return dt;
    }

    // Optional constraints with nullable types
    [Get("/test/constraint/optional-int/{id:int?}")]
    public static ErrorOr<int> ConstraintOptionalInt(int? id)
    {
        return id ?? -1;
    }

    [Get("/test/constraint/optional-guid/{id:guid?}")]
    public static ErrorOr<Guid> ConstraintOptionalGuid(Guid? id)
    {
        return id ?? Guid.Empty;
    }

    // Alpha constraint (string only)
    [Get("/test/constraint/alpha/{name:alpha}")]
    public static ErrorOr<string> ConstraintAlpha(string name)
    {
        return name;
    }

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
    // [Get("/test/duplicate")]
    // public static ErrorOr<string> CrossClassDuplicate() => "duplicate";

    // ✅ Should pass - catch-all is string
    [Get("/test/catchall/valid/{*path}")]
    public static ErrorOr<string> CatchAllValid(string path)
    {
        return path ?? "/";
    }

    // ✅ Catch-all at different positions
    [Get("/api/files/{*filePath}")]
    public static ErrorOr<string> CatchAllFilePath(string filePath)
    {
        return filePath;
    }

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
    public static ErrorOr<string> QueryString([FromQuery] string search)
    {
        return search;
    }

    [Get("/test/query/int")]
    public static ErrorOr<int> QueryInt([FromQuery] int page)
    {
        return page;
    }

    [Get("/test/query/nullable-int")]
    public static ErrorOr<int> QueryNullableInt([FromQuery] int? page)
    {
        return page ?? 1;
    }

    // ✅ Collections of primitives
    [Get("/test/query/int-array")]
    public static ErrorOr<int[]> QueryIntArray([FromQuery] int[] ids)
    {
        return ids;
    }

    [Get("/test/query/string-list")]
    public static ErrorOr<List<string>> QueryStringList([FromQuery] List<string> tags)
    {
        return tags;
    }

    [Get("/test/query/guid-array")]
    public static ErrorOr<Guid[]> QueryGuidArray([FromQuery] Guid[] ids)
    {
        return ids;
    }

    // ❌ Should trigger EOE012 - complex type not allowed
    // Uncomment to test diagnostics:
    // [Get("/test/query/invalid-complex")]
    // public static ErrorOr<Todo> QueryInvalidComplex([FromQuery] Todo todo) => todo;

    // ═══════════════════════════════════════════════════════════════════════
    // [FromHeader] Tests
    // ═══════════════════════════════════════════════════════════════════════

    // ✅ String headers
    [Get("/test/header/string")]
    public static ErrorOr<string> HeaderString([FromHeader(Name = "X-Request-Id")] string requestId)
    {
        return requestId;
    }

    // ✅ Primitive headers (int, bool, etc.)
    [Get("/test/header/int")]
    public static ErrorOr<int> HeaderInt([FromHeader(Name = "X-Page-Size")] int pageSize)
    {
        return pageSize;
    }

    [Get("/test/header/bool")]
    public static ErrorOr<bool> HeaderBool([FromHeader(Name = "X-Include-Deleted")] bool includeDeleted)
    {
        return includeDeleted;
    }

    // ✅ Collection headers
    [Get("/test/header/string-array")]
    public static ErrorOr<string[]> HeaderStringArray([FromHeader(Name = "X-Tags")] string[] tags)
    {
        return tags;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // [FromRoute] Tests
    // ═══════════════════════════════════════════════════════════════════════

    // ✅ All primitive types
    [Get("/test/route/int/{id}")]
    public static ErrorOr<int> RouteInt([FromRoute] int id)
    {
        return id;
    }

    [Get("/test/route/guid/{id}")]
    public static ErrorOr<Guid> RouteGuid([FromRoute] Guid id)
    {
        return id;
    }

    [Get("/test/route/string/{slug}")]
    public static ErrorOr<string> RouteString([FromRoute] string slug)
    {
        return slug;
    }

    // ✅ Custom name binding
    [Get("/test/route/custom-name/{userId}")]
    public static ErrorOr<int> RouteCustomName([FromRoute(Name = "userId")] int id)
    {
        return id;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // [AsParameters] Tests
    // ═══════════════════════════════════════════════════════════════════════

    // ✅ Valid DTO with public constructor
    [Get("/test/as-parameters/pagination")]
    public static ErrorOr<PaginationResult> AsParametersPagination([AsParameters] PaginationParams p)
    {
        return new PaginationResult(p.Page, p.PageSize, p.SortBy ?? "id");
    }

    // Note: [AsParameters] with nested [FromRoute] requires analyzer improvement
    // The analyzer doesn't yet expand record properties to match route parameters
#pragma warning disable EOE003 // Analyzer doesn't yet handle [AsParameters] record property binding
    [Get("/test/as-parameters/with-route/{id}")]
    public static ErrorOr<string> AsParametersWithRoute([AsParameters] RouteAndQueryParams p)
#pragma warning restore EOE003
    {
        return $"id={p.Id}, filter={p.Filter}";
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Implicit Binding Tests
    // ═══════════════════════════════════════════════════════════════════════

    // ✅ Implicit route binding (parameter name matches route)
    [Get("/test/implicit/route/{productId}")]
    public static ErrorOr<Guid> ImplicitRoute(Guid productId)
    {
        return productId;
    }

    // ✅ Implicit query binding (primitive not in route)
    [Get("/test/implicit/query")]
    public static ErrorOr<int> ImplicitQuery(int page, int pageSize)
    {
        return page * pageSize;
    }

    // ✅ HttpContext and CancellationToken (special types)
    [Get("/test/implicit/special-types")]
    public static ErrorOr<string> ImplicitSpecialTypes(HttpContext ctx, CancellationToken ct)
    {
        return ctx.Request.Path.Value ?? "/";
    }

    // ✅ Service injection - errors now inferred from [ReturnsError] on ITodoService.GetAllAsync
    [Get("/test/implicit/service")]
    public static Task<ErrorOr<List<Todo>>> ImplicitService(
        [FromServices] ITodoService svc,
        CancellationToken ct)
    {
        return svc.GetAllAsync(ct);
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// FORM FILE BINDING
// ═══════════════════════════════════════════════════════════════════════════

public static class FormFileTests
{
    // ✅ Single file
    [Post("/test/form/single-file")]
    public static ErrorOr<string> SingleFile(IFormFile file)
    {
        return $"Received: {file.FileName} ({file.Length} bytes)";
    }

    // ✅ Multiple files
    [Post("/test/form/multiple-files")]
    public static ErrorOr<int> MultipleFiles(IFormFileCollection files)
    {
        return files.Count;
    }

    // Note: IFormFile + [FromForm] parameters are valid in ASP.NET Core (all from same form body)
    // but the analyzer currently treats them as multiple body sources
    [Post("/test/form/file-with-data")]
    public static ErrorOr<string> FileWithData(
        IFormFile file,
        [FromForm] string description,
        [FromForm] string category)
        => $"{file.FileName}: {description} ({category})";

    // ✅ Complex form DTO
    [Post("/test/form/dto")]
    public static ErrorOr<string> FormDto([FromForm] FileUploadRequest request)
        => $"{request.File.FileName}: {request.Title}";
}

// ═══════════════════════════════════════════════════════════════════════════
// SSE / STREAMING TESTS
// NOTE: SSE endpoints don't work with ErrorOr pattern - streams can't be wrapped.
// SSE should be mapped directly with app.MapGet() + TypedResults.ServerSentEvents()
// ═══════════════════════════════════════════════════════════════════════════

// SSE endpoints are registered in Program.cs using raw MapGet, not ErrorOr attributes

// ═══════════════════════════════════════════════════════════════════════════
// RESULTS UNION EDGE CASES
// ═══════════════════════════════════════════════════════════════════════════

public static class ResultsUnionTests
{
    // ✅ All 7 ErrorOr error types
    [Get("/test/erroror/failure")]
    public static ErrorOr<string> TestFailure()
    {
        return Error.Failure("Test.Failure", "A failure occurred");
    }

    [Get("/test/erroror/unexpected")]
    public static ErrorOr<string> TestUnexpected()
    {
        return Error.Unexpected("Test.Unexpected", "Something unexpected");
    }

    [Get("/test/erroror/validation")]
    public static ErrorOr<string> TestValidation()
    {
        return Error.Validation("Test.Validation", "Validation failed");
    }

    [Get("/test/erroror/conflict")]
    public static ErrorOr<string> TestConflict()
    {
        return Error.Conflict("Test.Conflict", "Resource conflict");
    }

    [Get("/test/erroror/notfound")]
    public static ErrorOr<string> TestNotFound()
    {
        return Error.NotFound("Test.NotFound", "Resource not found");
    }

    [Get("/test/erroror/unauthorized")]
    public static ErrorOr<string> TestUnauthorized()
    {
        return Error.Unauthorized("Test.Unauthorized", "Not authorized");
    }

    [Get("/test/erroror/forbidden")]
    public static ErrorOr<string> TestForbidden()
    {
        return Error.Forbidden("Test.Forbidden", "Access forbidden");
    }

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
    public static ErrorOr<string> TestCustomUndocumented()
    {
        return Error.Custom(418, "Teapot.IAmOne", "I'm a teapot");
    }

    // ✅ 202 Accepted response
    [Post("/test/erroror/accepted")]
    [AcceptedResponse]
    public static ErrorOr<JobReference> TestAccepted()
    {
        return new JobReference(Guid.NewGuid(), "/api/jobs/status");
    }

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

        return new Todo(id, request.Title, request.IsComplete);
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

// Input DTO for [AsParameters]
public record PaginationParams(int Page = 1, int PageSize = 20, string? SortBy = null);

// Output result
public record PaginationResult(int Page, int PageSize, string SortBy);

public record RouteAndQueryParams(
    [FromRoute] Guid Id,
    [FromQuery] string? Filter = null);

#pragma warning disable AL0023 // Generator handles IFormFile in record constructors specially
public record FileUploadRequest(
    IFormFile File,
    [FromForm] string Title,
    [FromForm] string? Description = null);
#pragma warning restore AL0023

public record JobReference(Guid JobId, string StatusUrl);

// ═══════════════════════════════════════════════════════════════════════════
// CUSTOM TRYPARSE TYPE (for testing custom binding detection)
// ═══════════════════════════════════════════════════════════════════════════

public readonly struct CustomerId
{
    public int Value { get; }

    private CustomerId(int value)
    {
        Value = value;
    }

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

    public override string ToString()
    {
        return Value.ToString();
    }
}

public static class CustomTryParseTests
{
    // ✅ Custom type with TryParse - should be detected and allowed
    [Get("/test/custom-type/route/{id}")]
    public static ErrorOr<int> CustomTypeRoute(CustomerId id)
    {
        return id.Value;
    }

    [Get("/test/custom-type/query")]
    public static ErrorOr<int> CustomTypeQuery([FromQuery] CustomerId id)
    {
        return id.Value;
    }
}