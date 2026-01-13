# Diagnostics

Analyzer warnings and errors for ErrorOrX endpoints.

## Error Codes

### EOE001 - Invalid Return Type

**Severity:** Error

Handler methods must return `ErrorOr<T>`, `Task<ErrorOr<T>>`, or `ValueTask<ErrorOr<T>>`.

```csharp
// Error
[Get("/users")]
public static User GetUser() => new User();

// Fixed
[Get("/users")]
public static ErrorOr<User> GetUser() => new User();
```

### EOE002 - Handler Must Be Static

**Severity:** Error

Handler methods must be static for source generation.

```csharp
// Error
[Get("/users")]
public ErrorOr<User> GetUser() => ...

// Fixed
[Get("/users")]
public static ErrorOr<User> GetUser() => ...
```

### EOE003 - Route Parameter Not Bound

**Severity:** Error

Route template has a parameter that no method parameter captures.

```csharp
// Error: {id} not captured
[Get("/users/{id}")]
public static ErrorOr<User> GetUser() => ...

// Fixed
[Get("/users/{id}")]
public static ErrorOr<User> GetUser(int id) => ...
```

### EOE004 - Duplicate Route

**Severity:** Error

Same route + HTTP method registered by multiple handlers.

```csharp
// Error: duplicate GET /users
[Get("/users")]
public static ErrorOr<List<User>> GetAll() => ...

[Get("/users")]
public static ErrorOr<List<User>> ListUsers() => ...  // EOE004
```

### EOE005 - Invalid Route Pattern

**Severity:** Error

Route pattern syntax is invalid.

```csharp
// Error: unclosed brace
[Get("/users/{id")]  // EOE005
public static ErrorOr<User> GetUser(int id) => ...
```

### EOE006 - Multiple Body Sources

**Severity:** Error

Endpoint has multiple body sources. Only one of `[FromBody]`, `[FromForm]`, `Stream`, or `PipeReader` is allowed.

```csharp
// Error
[Post("/upload")]
public static ErrorOr<Result> Upload(
    [FromBody] Request body,
    [FromForm] IFormFile file)  // EOE006

// Fixed: choose one
[Post("/upload")]
public static ErrorOr<Result> Upload([FromForm] IFormFile file) => ...
```

---

## Warnings

### EOE007 - Type Not AOT-Serializable

**Severity:** Warning

Type used in endpoint is not registered in `JsonSerializerContext` for AOT.

```csharp
// Warning: CustomType not in context
[Get("/data")]
public static ErrorOr<CustomType> GetData() => ...

// Fix: add to your JsonSerializerContext
[JsonSerializable(typeof(CustomType))]
internal partial class AppJsonContext : JsonSerializerContext { }
```

### EOE009 - Body on Read-Only HTTP Method

**Severity:** Warning

GET, HEAD, DELETE, OPTIONS should not have request bodies per HTTP semantics.

```csharp
// Warning
[Get("/search")]
public static ErrorOr<Results> Search([FromBody] SearchQuery query)

// Fixed
[Post("/search")]
public static ErrorOr<Results> Search([FromBody] SearchQuery query)
```

### EOE010 - AcceptedResponse on Read-Only Method

**Severity:** Warning

`[AcceptedResponse]` on GET/DELETE is semantically unusual. 202 Accepted is for async operations.

---

## Parameter Binding Errors (EOE011-EOE019)

### EOE011 - Invalid FromRoute Type

**Severity:** Error

`[FromRoute]` parameter type must be a primitive or implement `TryParse`.

```csharp
// Error
[Get("/users/{filter}")]
public static ErrorOr<List<User>> Get([FromRoute] ComplexFilter filter)

// Fixed: use primitive
[Get("/users/{status}")]
public static ErrorOr<List<User>> Get([FromRoute] string status)
```

### EOE012 - Invalid FromQuery Type

**Severity:** Error

`[FromQuery]` parameter must be a primitive or collection of primitives.

### EOE013 - Invalid AsParameters Type

**Severity:** Error

`[AsParameters]` must be used on a class or struct type.

### EOE014 - AsParameters No Constructor

**Severity:** Error

Type used with `[AsParameters]` must have an accessible constructor.

### EOE016 - Invalid FromHeader Type

**Severity:** Error

`[FromHeader]` must be string, a primitive with `TryParse`, or a collection thereof.

---

## Route Constraint Diagnostics (EOE020-EOE029)

### EOE023 - Route Constraint Type Mismatch

**Severity:** Warning

Route constraint type does not match parameter type.

```csharp
// Warning: {id:int} expects int, got string
[Get("/users/{id:int}")]
public static ErrorOr<User> Get(string id)  // EOE023

// Fixed
[Get("/users/{id:int}")]
public static ErrorOr<User> Get(int id)
```

### EOE025 - Ambiguous Parameter Binding

**Severity:** Error

Complex type parameter on GET/DELETE requires explicit binding attribute.

```csharp
// Error: SearchFilter is complex type on GET
[Get("/users")]
public static ErrorOr<List<User>> Search(SearchFilter filter)  // EOE025

// Fixed with [FromQuery]
[Get("/users")]
public static ErrorOr<List<User>> Search([FromQuery] SearchFilter filter)

// Or use [AsParameters]
[Get("/users")]
public static ErrorOr<List<User>> Search([AsParameters] SearchFilter filter)
```

---

## OpenAPI Diagnostics (EOE030-EOE039)

### EOE030 - Too Many Result Types

**Severity:** Info

Endpoint has too many possible response types for `Results<...>` union (max 6). OpenAPI documentation may be incomplete.

### EOE032 - Unknown Error Factory

**Severity:** Warning

Error factory method is not a known ErrorType.

```csharp
// Warning
return Error.Unknown("code", "desc");  // Unknown factory

// Supported factories
Error.Validation()
Error.NotFound()
Error.Conflict()
Error.Unauthorized()
Error.Forbidden()
Error.Failure()
Error.Unexpected()
```

### EOE033 - Undocumented Interface Call

**Severity:** Error

Endpoint calls interface method returning `ErrorOr<T>` without error documentation.

```csharp
// Error: IUserService.GetById returns ErrorOr but errors unknown
[Get("/users/{id}")]
public static ErrorOr<User> Get(int id, IUserService svc)
    => svc.GetById(id);  // EOE033

// Fixed: document errors
[Get("/users/{id}")]
[ProducesError(ErrorType.NotFound)]
public static ErrorOr<User> Get(int id, IUserService svc)
    => svc.GetById(id);
```

---

## JSON Context Diagnostics (EOE040+)

### EOE040 - Missing CamelCase Policy

**Severity:** Warning

User's `JsonSerializerContext` should use camelCase for web API compatibility.

```csharp
// Warning: missing CamelCase policy
[JsonSerializable(typeof(User))]
internal partial class AppJsonContext : JsonSerializerContext { }

// Fixed
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(User))]
internal partial class AppJsonContext : JsonSerializerContext { }
```
