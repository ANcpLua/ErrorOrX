# Diagnostics

Analyzer warnings and errors for ErrorOrX endpoints.

## Error Codes

### EOE001 - Invalid return type

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

### EOE002 - Handler must be static

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

### EOE003 - Route parameter not bound

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

### EOE004 - Duplicate route

**Severity:** Error

Same route + HTTP method registered by multiple handlers.

```csharp
// Error: duplicate GET /users
[Get("/users")]
public static ErrorOr<List<User>> GetAll() => ...

[Get("/users")]
public static ErrorOr<List<User>> ListUsers() => ...  // EOE004
```

### EOE005 - Invalid route pattern

**Severity:** Error

Route pattern syntax is invalid.

```csharp
// Error: unclosed brace
[Get("/users/{id")]  // EOE005
public static ErrorOr<User> GetUser(int id) => ...
```

### EOE006 - Multiple body sources

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

### EOE007 - Type not AOT-serializable

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

### EOE009 - Body on read-only HTTP method

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

### EOE010 - [AcceptedResponse] on read-only method

**Severity:** Warning

`[AcceptedResponse]` on GET/DELETE is semantically unusual. 202 Accepted is for async operations.

---

## Parameter Binding Errors (EOE011-EOE019)

### EOE011 - Invalid [FromRoute] type

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

### EOE012 - Invalid [FromQuery] type

**Severity:** Error

`[FromQuery]` parameter must be a primitive or collection of primitives.

### EOE013 - Invalid [AsParameters] type

**Severity:** Error

`[AsParameters]` must be used on a class or struct type.

### EOE014 - [AsParameters] type has no constructor

**Severity:** Error

Type used with `[AsParameters]` must have an accessible constructor.

### EOE016 - Invalid [FromHeader] type

**Severity:** Error

`[FromHeader]` must be string, a primitive with `TryParse`, or a collection thereof.

---

## Route Constraint Diagnostics (EOE020-EOE029)

### EOE023 - Route constraint type mismatch

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

### EOE025 - Ambiguous parameter binding

**Severity:** Warning

Complex type parameter on a bodyless/custom method requires explicit binding. The generator will default to DI and warn.

```csharp
// Warning: SearchFilter is complex type on GET
[Get("/users")]
public static ErrorOr<List<User>> Search(SearchFilter filter)  // EOE025

// Fixed with [AsParameters]
[Get("/users")]
public static ErrorOr<List<User>> Search([AsParameters] SearchFilter filter)

// Or explicitly allow body binding
[Get("/users")]
public static ErrorOr<List<User>> Search([FromBody] SearchFilter filter)
```

---

## OpenAPI Diagnostics (EOE030-EOE039)

### EOE030 - Too many result types

**Severity:** Info

Endpoint has too many possible response types for `Results<...>` union (max 6). OpenAPI documentation may be incomplete.

### EOE032 - Unknown error factory

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

### EOE033 - Undocumented interface call

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

### EOE040 - Missing CamelCase policy

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

---

## AOT Safety Diagnostics (AOT001-AOT009)

### AOT001 - Activator.CreateInstance is not AOT-safe

**Severity:** Warning

`Activator.CreateInstance<T>()` uses reflection and is not compatible with NativeAOT.

### AOT002 - Type.GetType is not AOT-safe

**Severity:** Warning

`Type.GetType(string)` uses runtime type lookup and is not compatible with NativeAOT.

### AOT003 - Reflection over members is not AOT-safe

**Severity:** Warning

Reflection over type members is not compatible with NativeAOT because members may be trimmed.

### AOT004 - Expression.Compile is not AOT-safe

**Severity:** Warning

`Expression.Compile()` generates code at runtime and is not compatible with NativeAOT.

### AOT005 - 'dynamic' is not AOT-safe

**Severity:** Warning

The `dynamic` keyword uses runtime binding and is not compatible with NativeAOT.
