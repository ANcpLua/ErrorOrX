# ErrorOr ↔ TypedResults Complete Specification

> **Version:** 1.0.0  
> **Status:** CANONICAL - This document is the single source of truth  
> **Created:** 2026-01-08  
> **Target:** .NET 10 / ASP.NET Core 10 / ErrorOr.Endpoints Generator  
> **Principle:** Code = TypedResults Union = OpenAPI = Runtime Behavior (NO LIES)

---

## Table of Contents

1. [Core Principle](#1-core-principle)
2. [ErrorType → HTTP Status Code Mapping](#2-errortype--http-status-code-mapping)
3. [Success Types → HTTP Status Code Mapping](#3-success-types--http-status-code-mapping)
4. [Complete BCL TypedResults Reference](#4-complete-bcl-typedresults-reference)
5. [Error.Custom → HTTP Status Code Mapping](#5-errorcustom--http-status-code-mapping)
6. [Union Type Generation Rules](#6-union-type-generation-rules)
7. [ProblemDetails (RFC 7807) Integration](#7-problemdetails-rfc-7807-integration)
8. [OpenAPI Schema Generation](#8-openapi-schema-generation)
9. [Generator Implementation Contract](#9-generator-implementation-contract)
10. [Validation & Test Requirements](#10-validation--test-requirements)
11. [Extensibility & Future Compatibility](#11-extensibility--future-compatibility)
12. [Appendix A: RFC References](#appendix-a-rfc-references)
13. [Appendix B: BCL Type Full Qualified Names](#appendix-b-bcl-type-full-qualified-names)
14. [Appendix C: Change Log](#appendix-c-change-log)

---

## 1. Core Principle

### 1.1 The Truth Guarantee

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         THE FUNDAMENTAL INVARIANT                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   Developer Code (ErrorOr<T>)                                               │
│         ║                                                                   │
│         ║ MUST BE IDENTICAL TO                                              │
│         ▼                                                                   │
│   Generated TypedResults Union (Results<T1, T2, ...>)                       │
│         ║                                                                   │
│         ║ MUST BE IDENTICAL TO                                              │
│         ▼                                                                   │
│   OpenAPI Document (responses: { "200": ..., "404": ..., "500": ... })      │
│         ║                                                                   │
│         ║ MUST BE IDENTICAL TO                                              │
│         ▼                                                                   │
│   Runtime Behavior (actual HTTP responses)                                  │
│                                                                             │
│   ANY DEVIATION = BUG                                                       │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 1.2 Design Philosophy

1. **No Manual Metadata**: The generator MUST infer all OpenAPI metadata from types
2. **Compile-Time Safety**: All possible responses MUST be known at compile time
3. **BCL Conformance**: Only use BCL `TypedResults.*` methods - no custom result types
4. **RFC Compliance**: HTTP status codes MUST follow RFC 9110 semantics
5. **AOT Compatibility**: All generated code MUST be Native AOT compatible

### 1.3 What This Document Covers

| Scope | Included |
|-------|----------|
| ErrorOr ErrorTypes | ✅ All 7 types |
| ErrorOr Success Types | ✅ All sentinel types |
| BCL TypedResults | ✅ All 35+ result types |
| HTTP Status Codes | ✅ All 1xx-5xx codes |
| Custom Error Codes | ✅ Full range 100-599 |
| ProblemDetails | ✅ RFC 7807 compliant |
| OpenAPI Generation | ✅ Schema requirements |
| Union Type Limits | ✅ BCL 6-type limit handling |

---

## 2. ErrorType → HTTP Status Code Mapping

### 2.1 Canonical Mapping Table

This table is **IMMUTABLE** and defines the exact mapping from ErrorOr's `ErrorType` enum to HTTP status codes and BCL TypedResults types.

| ErrorType | HTTP | Title (RFC 9110) | TypedResults Factory | BCL Return Type | Has Body |
|-----------|------|------------------|---------------------|-----------------|----------|
| `Validation` | 400 | Bad Request | `TypedResults.ValidationProblem(errors)` | `ValidationProblem` | ✅ Yes |
| `Unauthorized` | 401 | Unauthorized | `TypedResults.Unauthorized()` | `UnauthorizedHttpResult` | ❌ No |
| `Forbidden` | 403 | Forbidden | `TypedResults.Forbid()` | `ForbidHttpResult` | ❌ No |
| `NotFound` | 404 | Not Found | `TypedResults.NotFound(problem)` | `NotFound<ProblemDetails>` | ✅ Yes |
| `Conflict` | 409 | Conflict | `TypedResults.Conflict(problem)` | `Conflict<ProblemDetails>` | ✅ Yes |
| `Failure` | **500** | Internal Server Error | `TypedResults.InternalServerError(problem)` | `InternalServerError<ProblemDetails>` | ✅ Yes |
| `Unexpected` | **500** | Internal Server Error | `TypedResults.InternalServerError(problem)` | `InternalServerError<ProblemDetails>` | ✅ Yes |

### 2.2 Critical Clarifications

#### 2.2.1 Failure and Unexpected → 500 (NOT 422)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ ⚠️  CRITICAL: ErrorType.Failure MUST map to 500, NEVER to 422              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ RFC 9110 §15.5.21 (422 Unprocessable Content):                              │
│   "The server understands the content type and syntax of the request        │
│    content but was unable to process the contained instructions."           │
│   → This is a CLIENT error (4xx) - the request was semantically invalid     │
│                                                                             │
│ RFC 9110 §15.6.1 (500 Internal Server Error):                               │
│   "The server encountered an unexpected condition that prevented it         │
│    from fulfilling the request."                                            │
│   → This is a SERVER error (5xx) - something went wrong on the server       │
│                                                                             │
│ ErrorType.Failure = "A known failure occurred" (e.g., database down)        │
│ ErrorType.Unexpected = "An unexpected error occurred" (e.g., exception)     │
│ Both are SERVER problems → 500                                              │
│                                                                             │
│ If you want 422, use: Error.Custom(422, "Code", "Description")              │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

#### 2.2.2 Validation → ValidationProblem (NOT BadRequest)

For `ErrorType.Validation`, the generator MUST use `ValidationProblem` which:
- Returns HTTP 400
- Uses `HttpValidationProblemDetails` (extends `ProblemDetails`)
- Aggregates multiple validation errors into the `errors` dictionary
- Follows RFC 7807 format

```csharp
// Multiple validation errors → aggregated ValidationProblem
if (errors.Any(e => e.Type == ErrorType.Validation))
{
    var dict = errors
        .Where(e => e.Type == ErrorType.Validation)
        .GroupBy(e => e.Code)
        .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
    
    return TypedResults.ValidationProblem(dict);
}
```

#### 2.2.3 Unauthorized and Forbidden → No Body

Per security best practices and RFC 9110:
- `401 Unauthorized`: MUST NOT include error details (prevents auth enumeration)
- `403 Forbidden`: MUST NOT include error details (prevents resource enumeration)

```csharp
// Correct: No body, no details leaked
ErrorType.Unauthorized => TypedResults.Unauthorized()
ErrorType.Forbidden => TypedResults.Forbid()

// WRONG: Never do this
ErrorType.Unauthorized => TypedResults.Unauthorized(problemDetails) // ❌ Does not exist
```

### 2.3 Default/Unknown ErrorType Handling

```csharp
// If an unknown ErrorType is encountered (future enum values, etc.)
// Default to 500 Internal Server Error (server's fault, not client's)
_ => TypedResults.InternalServerError(ToProblemDetails(error, 500, "Internal Server Error"))
```

**Rationale**: Unknown error types indicate a programming error or version mismatch. This is a server problem, not a client problem.

---

## 3. Success Types → HTTP Status Code Mapping

### 3.1 Canonical Success Mapping Table

| ErrorOr Type | HTTP | TypedResults Factory | BCL Return Type | Has Body | Use Case |
|--------------|------|---------------------|-----------------|----------|----------|
| `ErrorOr<T>` (value) | 200 | `TypedResults.Ok(value)` | `Ok<T>` | ✅ Yes | GET, PUT returning entity |
| `ErrorOr<Success>` | 200 | `TypedResults.Ok()` | `Ok` | ❌ No | Generic success |
| `ErrorOr<Created>` | 201 | `TypedResults.Created(uri)` | `Created` | ❌ No | POST without body |
| `ErrorOr<T>` + `[Post]` | 201 | `TypedResults.Created(uri, value)` | `Created<T>` | ✅ Yes | POST returning entity |
| `ErrorOr<Updated>` | 200 | `TypedResults.Ok()` | `Ok` | ❌ No | PUT/PATCH success |
| `ErrorOr<Deleted>` | 204 | `TypedResults.NoContent()` | `NoContent` | ❌ No | DELETE success |

### 3.2 HTTP Verb Inference Rules

The generator MUST infer the success status code from the HTTP verb attribute:

| Attribute | Success Type | Inferred Status | TypedResults |
|-----------|--------------|-----------------|--------------|
| `[HttpGet]` / `MapGet` | `ErrorOr<T>` | 200 | `Ok<T>` |
| `[HttpGet]` / `MapGet` | `ErrorOr<Success>` | 200 | `Ok` |
| `[HttpPost]` / `MapPost` | `ErrorOr<T>` | 201 | `Created<T>` |
| `[HttpPost]` / `MapPost` | `ErrorOr<Created>` | 201 | `Created` |
| `[HttpPut]` / `MapPut` | `ErrorOr<T>` | 200 | `Ok<T>` |
| `[HttpPut]` / `MapPut` | `ErrorOr<Updated>` | 200 | `Ok` |
| `[HttpPatch]` / `MapPatch` | `ErrorOr<T>` | 200 | `Ok<T>` |
| `[HttpPatch]` / `MapPatch` | `ErrorOr<Updated>` | 200 | `Ok` |
| `[HttpDelete]` / `MapDelete` | `ErrorOr<Deleted>` | 204 | `NoContent` |
| `[HttpDelete]` / `MapDelete` | `ErrorOr<T>` | 200 | `Ok<T>` |

### 3.3 Location Header for 201 Created

When returning `Created<T>`, the generator MUST include a Location header:

```csharp
// Pattern 1: URI template
TypedResults.Created($"/api/resources/{entity.Id}", entity)

// Pattern 2: Route name
TypedResults.CreatedAtRoute(entity, "GetResourceById", new { id = entity.Id })
```

---

## 4. Complete BCL TypedResults Reference

### 4.1 All TypedResults in .NET 10

This is the complete list of all `TypedResults.*` factory methods and their return types.

#### 4.1.1 2xx Success Responses

| HTTP | Factory Method | Return Type | Has Body | OpenAPI Schema |
|------|---------------|-------------|----------|----------------|
| 200 | `TypedResults.Ok()` | `Ok` | ❌ | No |
| 200 | `TypedResults.Ok(value)` | `Ok<T>` | ✅ | T |
| 201 | `TypedResults.Created(uri)` | `Created` | ❌ | No |
| 201 | `TypedResults.Created(uri, value)` | `Created<T>` | ✅ | T |
| 201 | `TypedResults.CreatedAtRoute(value, routeName, routeValues)` | `CreatedAtRoute<T>` | ✅ | T |
| 202 | `TypedResults.Accepted(uri)` | `Accepted` | ❌ | No |
| 202 | `TypedResults.Accepted(uri, value)` | `Accepted<T>` | ✅ | T |
| 202 | `TypedResults.AcceptedAtRoute(value, routeName, routeValues)` | `AcceptedAtRoute<T>` | ✅ | T |
| 204 | `TypedResults.NoContent()` | `NoContent` | ❌ | No |

#### 4.1.2 3xx Redirect Responses

| HTTP | Factory Method | Return Type | ErrorOr Applicable |
|------|---------------|-------------|-------------------|
| 301 | `TypedResults.Redirect(url, permanent: true)` | `RedirectHttpResult` | ❌ No |
| 302 | `TypedResults.Redirect(url, permanent: false)` | `RedirectHttpResult` | ❌ No |
| 307 | `TypedResults.Redirect(url, permanent: false, preserveMethod: true)` | `RedirectHttpResult` | ❌ No |
| 308 | `TypedResults.Redirect(url, permanent: true, preserveMethod: true)` | `RedirectHttpResult` | ❌ No |
| 3xx | `TypedResults.LocalRedirect(localUrl)` | `RedirectHttpResult` | ❌ No |
| 3xx | `TypedResults.RedirectToRoute(routeName, routeValues)` | `RedirectToRouteHttpResult` | ❌ No |

**Note**: Redirects are not error/success patterns and should not be used with ErrorOr.

#### 4.1.3 4xx Client Error Responses

| HTTP | Factory Method | Return Type | Has Body | ErrorOr Mapping |
|------|---------------|-------------|----------|-----------------|
| 400 | `TypedResults.BadRequest()` | `BadRequest` | ❌ | `Error.Validation` (single) |
| 400 | `TypedResults.BadRequest(error)` | `BadRequest<T>` | ✅ | `Error.Validation` (with details) |
| 400 | `TypedResults.ValidationProblem(errors)` | `ValidationProblem` | ✅ | `Error.Validation` (multiple) |
| 401 | `TypedResults.Unauthorized()` | `UnauthorizedHttpResult` | ❌ | `Error.Unauthorized` |
| 401 | `TypedResults.Challenge()` | `ChallengeHttpResult` | ❌ | Auth flow only |
| 403 | `TypedResults.Forbid()` | `ForbidHttpResult` | ❌ | `Error.Forbidden` |
| 404 | `TypedResults.NotFound()` | `NotFound` | ❌ | `Error.NotFound` (no details) |
| 404 | `TypedResults.NotFound(value)` | `NotFound<T>` | ✅ | `Error.NotFound` |
| 409 | `TypedResults.Conflict()` | `Conflict` | ❌ | `Error.Conflict` (no details) |
| 409 | `TypedResults.Conflict(error)` | `Conflict<T>` | ✅ | `Error.Conflict` |
| 422 | `TypedResults.UnprocessableEntity()` | `UnprocessableEntity` | ❌ | `Error.Custom(422, ...)` |
| 422 | `TypedResults.UnprocessableEntity(error)` | `UnprocessableEntity<T>` | ✅ | `Error.Custom(422, ...)` |

#### 4.1.4 5xx Server Error Responses

| HTTP | Factory Method | Return Type | Has Body | ErrorOr Mapping |
|------|---------------|-------------|----------|-----------------|
| 500 | `TypedResults.InternalServerError()` | `InternalServerError` | ❌ | `Error.Failure` / `Error.Unexpected` |
| 500 | `TypedResults.InternalServerError(error)` | `InternalServerError<T>` | ✅ | `Error.Failure` / `Error.Unexpected` |

#### 4.1.5 Content Responses

| HTTP | Factory Method | Return Type | ErrorOr Applicable |
|------|---------------|-------------|-------------------|
| 200 | `TypedResults.Content(content, contentType)` | `ContentHttpResult` | ❌ Raw content |
| 200 | `TypedResults.Text(content, contentType)` | `ContentHttpResult` | ❌ Raw content |
| 200 | `TypedResults.Json(value)` | `JsonHttpResult<T>` | ✅ Possible |
| 200 | `TypedResults.Bytes(data, contentType)` | `FileContentHttpResult` | ❌ Binary |
| 200 | `TypedResults.File(data, contentType)` | `FileContentHttpResult` | ❌ Binary |
| 200 | `TypedResults.Stream(stream, contentType)` | `FileStreamHttpResult` | ❌ Streaming |
| 200 | `TypedResults.Stream(callback, contentType)` | `PushStreamHttpResult` | ❌ Streaming |

#### 4.1.6 Problem Details (RFC 7807)

| HTTP | Factory Method | Return Type | ErrorOr Mapping |
|------|---------------|-------------|-----------------|
| Any | `TypedResults.Problem(...)` | `ProblemHttpResult` | `Error.Custom(statusCode, ...)` |
| 400 | `TypedResults.ValidationProblem(errors)` | `ValidationProblem` | `Error.Validation` (multiple) |

#### 4.1.7 Special/Utility

| HTTP | Factory Method | Return Type | ErrorOr Applicable |
|------|---------------|-------------|-------------------|
| Any | `TypedResults.StatusCode(code)` | `StatusCodeHttpResult` | `Error.Custom(code, ...)` |
| 200 | `TypedResults.Empty` | `EmptyHttpResult` | ❌ No-op |

#### 4.1.8 Server-Sent Events (.NET 10+)

| HTTP | Factory Method | Return Type | ErrorOr Applicable |
|------|---------------|-------------|-------------------|
| 200 | `TypedResults.ServerSentEvents(events)` | `ServerSentEventsResult<T>` | ❌ Streaming |

### 4.2 TypedResults NOT Applicable to ErrorOr

The following TypedResults are **not applicable** to ErrorOr pattern:

| Category | Types | Reason |
|----------|-------|--------|
| Redirects | `RedirectHttpResult`, `RedirectToRouteHttpResult` | Not error/success pattern |
| Auth Flow | `ChallengeHttpResult` | Triggers browser auth flow |
| Raw Content | `ContentHttpResult`, `FileContentHttpResult`, `FileStreamHttpResult`, `PushStreamHttpResult` | Binary/streaming content |
| SSE | `ServerSentEventsResult<T>` | Streaming events |
| Utility | `EmptyHttpResult` | No-op result |

---

## 5. Error.Custom → HTTP Status Code Mapping

### 5.1 Direct TypedResults Mapping

For `Error.Custom(statusCode, code, description)`, map to the most specific TypedResult:

| NumericType | TypedResults Factory | Return Type |
|-------------|---------------------|-------------|
| 400 | `TypedResults.BadRequest(problem)` | `BadRequest<ProblemDetails>` |
| 401 | `TypedResults.Unauthorized()` | `UnauthorizedHttpResult` |
| 403 | `TypedResults.Forbid()` | `ForbidHttpResult` |
| 404 | `TypedResults.NotFound(problem)` | `NotFound<ProblemDetails>` |
| 409 | `TypedResults.Conflict(problem)` | `Conflict<ProblemDetails>` |
| 422 | `TypedResults.UnprocessableEntity(problem)` | `UnprocessableEntity<ProblemDetails>` |
| 500 | `TypedResults.InternalServerError(problem)` | `InternalServerError<ProblemDetails>` |

### 5.2 ProblemHttpResult Fallback

For status codes without a direct TypedResult helper, use `ProblemHttpResult`:

| NumericType | TypedResults Factory | Example Use Case |
|-------------|---------------------|------------------|
| 402 | `TypedResults.Problem(statusCode: 402)` | Payment Required |
| 405 | `TypedResults.Problem(statusCode: 405)` | Method Not Allowed |
| 406 | `TypedResults.Problem(statusCode: 406)` | Not Acceptable |
| 408 | `TypedResults.Problem(statusCode: 408)` | Request Timeout |
| 410 | `TypedResults.Problem(statusCode: 410)` | Gone |
| 412 | `TypedResults.Problem(statusCode: 412)` | Precondition Failed |
| 413 | `TypedResults.Problem(statusCode: 413)` | Content Too Large |
| 415 | `TypedResults.Problem(statusCode: 415)` | Unsupported Media Type |
| 418 | `TypedResults.Problem(statusCode: 418)` | I'm a teapot 🫖 |
| 423 | `TypedResults.Problem(statusCode: 423)` | Locked (WebDAV) |
| 424 | `TypedResults.Problem(statusCode: 424)` | Failed Dependency (WebDAV) |
| 429 | `TypedResults.Problem(statusCode: 429)` | Too Many Requests |
| 451 | `TypedResults.Problem(statusCode: 451)` | Unavailable For Legal Reasons |
| 501 | `TypedResults.Problem(statusCode: 501)` | Not Implemented |
| 502 | `TypedResults.Problem(statusCode: 502)` | Bad Gateway |
| 503 | `TypedResults.Problem(statusCode: 503)` | Service Unavailable |
| 504 | `TypedResults.Problem(statusCode: 504)` | Gateway Timeout |

### 5.3 Complete NumericType Resolution Algorithm

```csharp
static IResult MapCustomError(Error error)
{
    var code = error.NumericType;
    var problem = ToProblemDetails(error, code, GetTitleForStatusCode(code));
    
    return code switch
    {
        // Direct TypedResult helpers (prefer these for OpenAPI schema)
        400 => TypedResults.BadRequest(problem),
        401 => TypedResults.Unauthorized(),
        403 => TypedResults.Forbid(),
        404 => TypedResults.NotFound(problem),
        409 => TypedResults.Conflict(problem),
        422 => TypedResults.UnprocessableEntity(problem),
        500 => TypedResults.InternalServerError(problem),
        
        // Fallback to Problem() for all other valid HTTP status codes
        >= 400 and < 600 => TypedResults.Problem(
            detail: error.Description,
            statusCode: code,
            title: GetTitleForStatusCode(code),
            type: $"https://httpstatuses.io/{code}"
        ),
        
        // Invalid status code → default to 500
        _ => TypedResults.InternalServerError(problem)
    };
}
```

### 5.4 HTTP Status Code Title Reference

```csharp
static string GetTitleForStatusCode(int code) => code switch
{
    // 4xx Client Errors
    400 => "Bad Request",
    401 => "Unauthorized",
    402 => "Payment Required",
    403 => "Forbidden",
    404 => "Not Found",
    405 => "Method Not Allowed",
    406 => "Not Acceptable",
    407 => "Proxy Authentication Required",
    408 => "Request Timeout",
    409 => "Conflict",
    410 => "Gone",
    411 => "Length Required",
    412 => "Precondition Failed",
    413 => "Content Too Large",
    414 => "URI Too Long",
    415 => "Unsupported Media Type",
    416 => "Range Not Satisfiable",
    417 => "Expectation Failed",
    418 => "I'm a teapot",
    421 => "Misdirected Request",
    422 => "Unprocessable Content",
    423 => "Locked",
    424 => "Failed Dependency",
    425 => "Too Early",
    426 => "Upgrade Required",
    428 => "Precondition Required",
    429 => "Too Many Requests",
    431 => "Request Header Fields Too Large",
    451 => "Unavailable For Legal Reasons",
    
    // 5xx Server Errors
    500 => "Internal Server Error",
    501 => "Not Implemented",
    502 => "Bad Gateway",
    503 => "Service Unavailable",
    504 => "Gateway Timeout",
    505 => "HTTP Version Not Supported",
    506 => "Variant Also Negotiates",
    507 => "Insufficient Storage",
    508 => "Loop Detected",
    510 => "Not Extended",
    511 => "Network Authentication Required",
    
    // Default
    _ => "Error"
};
```

---

## 6. Union Type Generation Rules

### 6.1 BCL Results<> Limit

The BCL provides `Results<T1, T2, ..., T6>` - maximum 6 type parameters.

```csharp
// Valid: Up to 6 types
Results<Ok<Todo>, NotFound, BadRequest<string>, Conflict<string>, UnprocessableEntity<string>, InternalServerError<string>>

// Invalid: 7+ types - will not compile
Results<Ok<Todo>, Created<Todo>, NotFound, BadRequest, Conflict, UnprocessableEntity, InternalServerError> // ❌
```

### 6.2 Type Deduplication Rules

Before generating the union, deduplicate types:

```csharp
// If endpoint can return both Error.Failure and Error.Unexpected
// Both map to InternalServerError<ProblemDetails>
// Union should contain only ONE InternalServerError<ProblemDetails>

// Input: [Ok<Todo>, NotFound<ProblemDetails>, InternalServerError<ProblemDetails>, InternalServerError<ProblemDetails>]
// Output: Results<Ok<Todo>, NotFound<ProblemDetails>, InternalServerError<ProblemDetails>>
```

### 6.3 Type Ordering Convention

Order types in the union by status code (ascending):

```csharp
// Correct ordering
Results<
    Ok<Todo>,                           // 200
    Created<Todo>,                      // 201
    NoContent,                          // 204
    BadRequest<ProblemDetails>,         // 400
    NotFound<ProblemDetails>,           // 404
    Conflict<ProblemDetails>,           // 409
    InternalServerError<ProblemDetails> // 500
>
```

### 6.4 Exceeding 6-Type Limit

When more than 6 distinct response types are needed:

**Option 1: Use IResult (lose compile-time safety)**
```csharp
// Fallback to IResult - requires manual .Produces() for OpenAPI
public static IResult Handler() { ... }
```

**Option 2: Consolidate error types**
```csharp
// Use ProblemHttpResult for multiple error codes
Results<Ok<Todo>, NotFound<ProblemDetails>, ProblemHttpResult>
// ProblemHttpResult can represent 400, 409, 422, 500, etc.
```

**Option 3: Generate IEndpointMetadataProvider**
```csharp
// Custom metadata provider for complex scenarios
public class ComplexEndpointMetadata : IEndpointMetadataProvider
{
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        builder.Metadata.Add(new ProducesResponseTypeMetadata(typeof(Todo), 200));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(typeof(ProblemDetails), 400));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(typeof(ProblemDetails), 404));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(typeof(ProblemDetails), 409));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(typeof(ProblemDetails), 422));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(typeof(ProblemDetails), 500));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(typeof(ProblemDetails), 503));
    }
}
```

### 6.5 Generic vs Non-Generic Types

| Scenario | Use Generic | Use Non-Generic |
|----------|-------------|-----------------|
| Want body in response | `NotFound<ProblemDetails>` | - |
| No body needed | - | `NotFound` |
| OpenAPI schema required | `BadRequest<ProblemDetails>` | - |
| Simple status only | - | `Unauthorized` |

**Generator Default**: Always use generic variants with `ProblemDetails` for error types to ensure OpenAPI schema generation.

---

## 7. ProblemDetails (RFC 7807) Integration

### 7.1 Standard ProblemDetails Structure

```json
{
    "type": "https://httpstatuses.io/404",
    "title": "Not Found",
    "status": 404,
    "detail": "Todo with ID 123 was not found",
    "instance": "/api/todos/123",
    "code": "Todo.NotFound",
    "traceId": "00-abc123def456-789..."
}
```

### 7.2 ErrorOr → ProblemDetails Mapping

```csharp
static ProblemDetails ToProblemDetails(Error error, int statusCode, string title)
{
    return new ProblemDetails
    {
        Type = $"https://httpstatuses.io/{statusCode}",
        Title = title,
        Status = statusCode,
        Detail = error.Description,
        Extensions =
        {
            ["code"] = error.Code,
            ["traceId"] = Activity.Current?.Id ?? Guid.NewGuid().ToString()
        }
    };
}
```

### 7.3 Validation Errors → HttpValidationProblemDetails

```csharp
static HttpValidationProblemDetails ToValidationProblemDetails(IReadOnlyList<Error> errors)
{
    var problemDetails = new HttpValidationProblemDetails();
    problemDetails.Status = 400;
    problemDetails.Title = "One or more validation errors occurred.";
    problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
    
    foreach (var error in errors.Where(e => e.Type == ErrorType.Validation))
    {
        if (!problemDetails.Errors.TryGetValue(error.Code, out var messages))
        {
            problemDetails.Errors[error.Code] = new[] { error.Description };
        }
        else
        {
            var newMessages = new string[messages.Length + 1];
            messages.CopyTo(newMessages, 0);
            newMessages[messages.Length] = error.Description;
            problemDetails.Errors[error.Code] = newMessages;
        }
    }
    
    return problemDetails;
}
```

### 7.4 ProblemDetails Content-Type

All ProblemDetails responses MUST use:
```
Content-Type: application/problem+json
```

This is automatically handled by `TypedResults.Problem()` and `TypedResults.ValidationProblem()`.

---

## 8. OpenAPI Schema Generation

### 8.1 IEndpointMetadataProvider Integration

All BCL TypedResults implement `IEndpointMetadataProvider`, which automatically populates OpenAPI metadata.

```csharp
// This union type:
Results<Ok<Todo>, NotFound<ProblemDetails>, InternalServerError<ProblemDetails>>

// Generates this OpenAPI:
responses:
  "200":
    description: "OK"
    content:
      application/json:
        schema:
          $ref: "#/components/schemas/Todo"
  "404":
    description: "Not Found"
    content:
      application/problem+json:
        schema:
          $ref: "#/components/schemas/ProblemDetails"
  "500":
    description: "Internal Server Error"
    content:
      application/problem+json:
        schema:
          $ref: "#/components/schemas/ProblemDetails"
```

### 8.2 Schema Requirements by Type

| TypedResult | OpenAPI Response | Content-Type | Schema |
|-------------|------------------|--------------|--------|
| `Ok<T>` | 200 | `application/json` | T |
| `Ok` | 200 | - | - |
| `Created<T>` | 201 | `application/json` | T |
| `Created` | 201 | - | - |
| `NoContent` | 204 | - | - |
| `BadRequest<ProblemDetails>` | 400 | `application/problem+json` | ProblemDetails |
| `ValidationProblem` | 400 | `application/problem+json` | HttpValidationProblemDetails |
| `Unauthorized` | 401 | - | - |
| `Forbid` | 403 | - | - |
| `NotFound<ProblemDetails>` | 404 | `application/problem+json` | ProblemDetails |
| `Conflict<ProblemDetails>` | 409 | `application/problem+json` | ProblemDetails |
| `UnprocessableEntity<ProblemDetails>` | 422 | `application/problem+json` | ProblemDetails |
| `InternalServerError<ProblemDetails>` | 500 | `application/problem+json` | ProblemDetails |
| `ProblemHttpResult` | varies | `application/problem+json` | ProblemDetails |

### 8.3 JsonSerializerContext Requirements (AOT)

For Native AOT compatibility, all types must be registered:

```csharp
[JsonSerializable(typeof(Todo))]
[JsonSerializable(typeof(Todo[]))]
[JsonSerializable(typeof(List<Todo>))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
[JsonSerializable(typeof(Dictionary<string, string[]>))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
```

---

## 9. Generator Implementation Contract

### 9.1 Required Generated Code Structure

```csharp
// 1. Error mapping method (MUST be correct)
private static IResult MapErrorToResult(Error error)
{
    return error.Type switch
    {
        ErrorType.Validation => HandleValidationError(error),
        ErrorType.Unauthorized => TypedResults.Unauthorized(),
        ErrorType.Forbidden => TypedResults.Forbid(),
        ErrorType.NotFound => TypedResults.NotFound(ToProblemDetails(error, 404, "Not Found")),
        ErrorType.Conflict => TypedResults.Conflict(ToProblemDetails(error, 409, "Conflict")),
        ErrorType.Failure => TypedResults.InternalServerError(ToProblemDetails(error, 500, "Internal Server Error")),
        ErrorType.Unexpected => TypedResults.InternalServerError(ToProblemDetails(error, 500, "Internal Server Error")),
        _ when error.NumericType is >= 100 and <= 599 => MapCustomError(error),
        _ => TypedResults.InternalServerError(ToProblemDetails(error, 500, "Internal Server Error"))
    };
}

// 2. Multiple errors handling
private static IResult MapErrorsToResult(IReadOnlyList<Error> errors)
{
    if (errors.Count == 0)
        return TypedResults.InternalServerError();
    
    // Validation errors: aggregate into ValidationProblem
    if (errors.Any(e => e.Type == ErrorType.Validation))
        return TypedResults.ValidationProblem(AggregateValidationErrors(errors));
    
    // Non-validation: use first error
    return MapErrorToResult(errors[0]);
}

// 3. ProblemDetails builder
private static ProblemDetails ToProblemDetails(Error error, int status, string title)
{
    return new ProblemDetails
    {
        Type = $"https://httpstatuses.io/{status}",
        Title = title,
        Status = status,
        Detail = error.Description,
        Extensions = { ["code"] = error.Code }
    };
}
```

### 9.2 Generated Endpoint Pattern

```csharp
// Input: Developer writes
public static ErrorOr<Todo> GetById(int id) { ... }

// Output: Generator produces
app.MapGet("/api/todos/{id}", Results<Ok<Todo>, NotFound<ProblemDetails>, InternalServerError<ProblemDetails>> (int id) =>
{
    var result = TodoEndpoints.GetById(id);
    
    if (result.IsError)
        return MapErrorsToResult(result.Errors);
    
    return TypedResults.Ok(result.Value);
});
```

### 9.3 Compile-Time Error Type Inference

The generator SHOULD analyze the method body to infer possible error types:

```csharp
// Analyzed method
public static ErrorOr<Todo> GetById(int id)
{
    if (id <= 0)
        return Error.Validation("Id.Invalid", "ID must be positive");  // → 400
    
    var todo = _db.Find(id);
    if (todo is null)
        return Error.NotFound("Todo.NotFound", $"Todo {id} not found"); // → 404
    
    return todo; // → 200
}

// Inferred union (minimal, only what's used)
Results<Ok<Todo>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>>

// vs. Full union (all possible ErrorTypes)
Results<Ok<Todo>, ValidationProblem, UnauthorizedHttpResult, ForbidHttpResult, NotFound<ProblemDetails>, Conflict<ProblemDetails>, InternalServerError<ProblemDetails>>
```

---

## 10. Validation & Test Requirements

### 10.1 Required Unit Tests

```csharp
public class ErrorTypeMappingTests
{
    [Theory]
    [InlineData(ErrorType.Validation, 400)]
    [InlineData(ErrorType.Unauthorized, 401)]
    [InlineData(ErrorType.Forbidden, 403)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Conflict, 409)]
    [InlineData(ErrorType.Failure, 500)]      // CRITICAL TEST
    [InlineData(ErrorType.Unexpected, 500)]   // CRITICAL TEST
    public void ErrorType_MapsToCorrectStatusCode(ErrorType errorType, int expectedStatus)
    {
        var (_, status, _) = GetErrorMapping(errorType);
        Assert.Equal(expectedStatus, status);
    }
    
    [Fact]
    public void Failure_MustReturn500_Not422()
    {
        var error = Error.Failure("Test.Failure", "Test");
        var result = MapErrorToResult(error);
        
        Assert.IsType<InternalServerError<ProblemDetails>>(result);
        // NOT UnprocessableEntity
    }
    
    [Fact]
    public void DefaultErrorType_MustReturn500_Not400()
    {
        var (_, status, _) = GetErrorMapping((ErrorType)999);
        Assert.Equal(500, status);
    }
}
```

### 10.2 Integration Test Requirements

```csharp
public class OpenApiSchemaTests
{
    [Fact]
    public async Task GeneratedOpenApi_MatchesActualBehavior()
    {
        // 1. Get OpenAPI document
        var openApiDoc = await GetOpenApiDocument();
        
        // 2. For each endpoint, verify:
        //    - Documented status codes match actual possible responses
        //    - Schema types match actual response bodies
        //    - Content-Types match actual responses
    }
    
    [Fact]
    public async Task ErrorResponse_ReturnsDocumentedStatusCode()
    {
        // Return Error.NotFound → Response MUST be 404
        // Return Error.Failure → Response MUST be 500 (NOT 422)
    }
}
```

### 10.3 Compile-Time Verification

The generator SHOULD emit compile-time warnings/errors for:

1. Union type exceeds 6 types
2. Incompatible error types for the return type
3. Missing JsonSerializable attributes for AOT

---

## 11. Extensibility & Future Compatibility

### 11.1 Adding New ErrorTypes

If `ErrorOr` adds new `ErrorType` enum values:

1. Add to mapping table with appropriate HTTP status code
2. Determine if BCL has direct TypedResult or use `ProblemHttpResult`
3. Update tests
4. Document in this spec

### 11.2 Adding New TypedResults

If ASP.NET Core adds new `TypedResults.*` methods:

1. Evaluate if applicable to ErrorOr pattern
2. Add to Section 4 reference table
3. Determine ErrorOr mapping (if any)
4. Update generator if needed

### 11.3 HTTP Status Code Registry Changes

Monitor IANA HTTP Status Code Registry for new codes:
- https://www.iana.org/assignments/http-status-codes/http-status-codes.xhtml

### 11.4 Version Compatibility Matrix

| .NET Version | Supported | Notes |
|--------------|-----------|-------|
| .NET 8 | ✅ | Missing some TypedResults |
| .NET 9 | ✅ | Full support |
| .NET 10 | ✅ | Full support + SSE |
| .NET 11+ | ✅ | Follow this spec |

---

## Appendix A: RFC References

| RFC | Title | Relevant Sections |
|-----|-------|-------------------|
| RFC 9110 | HTTP Semantics | §15 (Status Codes) |
| RFC 7807 | Problem Details for HTTP APIs | All |
| RFC 7231 | HTTP/1.1 Semantics and Content | Legacy, superseded by RFC 9110 |
| RFC 6585 | Additional HTTP Status Codes | 428, 429, 431, 511 |
| RFC 7725 | 451 Unavailable For Legal Reasons | All |
| RFC 4918 | HTTP Extensions for WebDAV | 422, 423, 424, 507 |

---

## Appendix B: BCL Type Full Qualified Names

```csharp
// Namespace: Microsoft.AspNetCore.Http.HttpResults
namespace Microsoft.AspNetCore.Http.HttpResults
{
    // 2xx Success
    public sealed class Ok : IResult, IEndpointMetadataProvider { }
    public sealed class Ok<TValue> : IResult, IEndpointMetadataProvider { }
    public sealed class Created : IResult, IEndpointMetadataProvider { }
    public sealed class Created<TValue> : IResult, IEndpointMetadataProvider { }
    public sealed class CreatedAtRoute<TValue> : IResult, IEndpointMetadataProvider { }
    public sealed class Accepted : IResult, IEndpointMetadataProvider { }
    public sealed class Accepted<TValue> : IResult, IEndpointMetadataProvider { }
    public sealed class AcceptedAtRoute<TValue> : IResult, IEndpointMetadataProvider { }
    public sealed class NoContent : IResult, IEndpointMetadataProvider { }
    
    // 3xx Redirect
    public sealed class RedirectHttpResult : IResult { }
    public sealed class RedirectToRouteHttpResult : IResult { }
    
    // 4xx Client Error
    public sealed class BadRequest : IResult, IEndpointMetadataProvider { }
    public sealed class BadRequest<TValue> : IResult, IEndpointMetadataProvider { }
    public sealed class ValidationProblem : IResult, IEndpointMetadataProvider { }
    public sealed class UnauthorizedHttpResult : IResult, IEndpointMetadataProvider { }
    public sealed class ChallengeHttpResult : IResult { }
    public sealed class ForbidHttpResult : IResult, IEndpointMetadataProvider { }
    public sealed class NotFound : IResult, IEndpointMetadataProvider { }
    public sealed class NotFound<TValue> : IResult, IEndpointMetadataProvider { }
    public sealed class Conflict : IResult, IEndpointMetadataProvider { }
    public sealed class Conflict<TValue> : IResult, IEndpointMetadataProvider { }
    public sealed class UnprocessableEntity : IResult, IEndpointMetadataProvider { }
    public sealed class UnprocessableEntity<TValue> : IResult, IEndpointMetadataProvider { }
    
    // 5xx Server Error
    public sealed class InternalServerError : IResult, IEndpointMetadataProvider { }
    public sealed class InternalServerError<TValue> : IResult, IEndpointMetadataProvider { }
    
    // Problem Details
    public sealed class ProblemHttpResult : IResult, IEndpointMetadataProvider { }
    
    // Content
    public sealed class ContentHttpResult : IResult { }
    public sealed class JsonHttpResult<TValue> : IResult, IEndpointMetadataProvider { }
    public sealed class FileContentHttpResult : IResult { }
    public sealed class FileStreamHttpResult : IResult { }
    public sealed class PushStreamHttpResult : IResult { }
    
    // Utility
    public sealed class StatusCodeHttpResult : IResult { }
    public sealed class EmptyHttpResult : IResult { }
    
    // SSE (.NET 10+)
    public sealed class ServerSentEventsResult<T> : IResult { }
}

// Namespace: Microsoft.AspNetCore.Mvc
namespace Microsoft.AspNetCore.Mvc
{
    public class ProblemDetails { }
    public class HttpValidationProblemDetails : ProblemDetails { }
}

// Union Types
namespace Microsoft.AspNetCore.Http.HttpResults
{
    public sealed class Results<TResult1, TResult2> : IResult { }
    public sealed class Results<TResult1, TResult2, TResult3> : IResult { }
    public sealed class Results<TResult1, TResult2, TResult3, TResult4> : IResult { }
    public sealed class Results<TResult1, TResult2, TResult3, TResult4, TResult5> : IResult { }
    public sealed class Results<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6> : IResult { }
}
```

---

## Appendix C: Change Log

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2026-01-08 | Initial canonical specification |

---

## Document Control

| Property | Value |
|----------|-------|
| Document ID | ERROROR-TYPEDRESULTS-SPEC-001 |
| Status | CANONICAL |
| Owner | ErrorOr.Endpoints Maintainers |
| Review Cycle | On each .NET major release |
| Last Reviewed | 2026-01-08 |

---

**END OF SPECIFICATION**
