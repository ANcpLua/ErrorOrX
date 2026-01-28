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

### EOE007 - Type not AOT-serializable

**Severity:** Error

Type used in endpoint is not registered in any `JsonSerializerContext`. This is an **error** because Native AOT
compilation will fail at runtime with cryptic errors.

```csharp
// Error: CustomType not in context
[Get("/data")]
public static ErrorOr<CustomType> GetData() => ...  // EOE007

// Fix: add to your JsonSerializerContext
[JsonSerializable(typeof(CustomType))]
internal partial class AppJsonContext : JsonSerializerContext { }
```

---

## Warnings

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

### EOE017 - Anonymous return type not supported

**Severity:** Error

Handler methods cannot return `ErrorOr<T>` with an anonymous type. Anonymous types have no stable identity for JSON
serialization.

```csharp
// Error: anonymous type
[Get("/data")]
public static ErrorOr<object> GetData() => new { Name = "test" };  // EOE017

// Fixed: use named type
[Get("/data")]
public static ErrorOr<DataResponse> GetData() => new DataResponse { Name = "test" };
```

### EOE018 - Nested [AsParameters] not supported

**Severity:** Error

`[AsParameters]` types cannot contain properties that are also marked with `[AsParameters]`. Recursive parameter
expansion is not supported.

```csharp
// Error: nested [AsParameters]
public class OuterParams {
    [AsParameters]
    public InnerParams Inner { get; set; }  // EOE018
}

// Fixed: flatten or use single level
public class FlatParams {
    public string Name { get; set; }
    public int Page { get; set; }
}
```

### EOE019 - Nullable [AsParameters] not supported

**Severity:** Error

`[AsParameters]` cannot be applied to nullable types. Parameter expansion requires a concrete instance.

```csharp
// Error: nullable
[Get("/users")]
public static ErrorOr<List<User>> Get([AsParameters] SearchParams? search)  // EOE019

// Fixed: non-nullable
[Get("/users")]
public static ErrorOr<List<User>> Get([AsParameters] SearchParams search)
```

---

## Type Accessibility Diagnostics (EOE020-EOE022)

### EOE020 - Inaccessible type in endpoint

**Severity:** Error

Private or protected types cannot be used in endpoint signatures. Generated code cannot access them.

```csharp
// Error: private type
private class SecretRequest { }

[Post("/data")]
public static ErrorOr<Result> Post([FromBody] SecretRequest req)  // EOE020

// Fixed: make accessible
internal class InternalRequest { }

[Post("/data")]
public static ErrorOr<Result> Post([FromBody] InternalRequest req)
```

### EOE021 - Type parameter not supported

**Severity:** Error

Generic type parameters (open generics) cannot be used in endpoint return types. The generator cannot emit code for
unbound generic types.

```csharp
// Error: type parameter
public static ErrorOr<T> GetItem<T>(int id) where T : class  // EOE021

// Fixed: use concrete type
public static ErrorOr<User> GetUser(int id)
```

---

## Route Constraint Diagnostics (EOE023-EOE029)

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

**Severity:** Error

Complex type parameter on a bodyless/custom method requires explicit binding. This is an error because ambiguous binding
can lead to runtime failures.

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

### EOE041 - Missing JsonSerializerContext for AOT

**Severity:** Error

**This is a critical AOT safety error.** Endpoint uses a request body parameter but no `JsonSerializerContext` was found
in the project.

**Why this is an error:** Roslyn source generators cannot see output from other generators. If ErrorOrX generates a
`JsonSerializerContext`, the System.Text.Json source generator will **not** process it, and Native AOT serialization
will fail at runtime with cryptic errors like "JsonTypeInfo metadata not found".

```csharp
// Error: no JsonSerializerContext exists
[Post("/users")]
public static ErrorOr<User> Create([FromBody] CreateUserRequest req) => ...  // EOE041

// Fix: create your own JsonSerializerContext
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(CreateUserRequest))]
[JsonSerializable(typeof(User))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
internal partial class AppJsonSerializerContext : JsonSerializerContext { }

// And register it in Program.cs:
builder.Services.AddErrorOrEndpoints(options => options
    .UseJsonContext<AppJsonSerializerContext>());
```

**See also:
** [ASP.NET Core Request Delegate Generator](https://learn.microsoft.com/aspnet/core/fundamentals/aot/request-delegate-generator/rdg)

---

## API Versioning Diagnostics (EOE050-EOE059)

### EOE050 - Version-neutral with mappings

**Severity:** Warning

Endpoint is marked `[ApiVersionNeutral]` but also has `[MapToApiVersion]`. These are mutually exclusive.

```csharp
// Warning: conflicting attributes
[ApiVersionNeutral]
[MapToApiVersion("2.0")]
[Get("/health")]
public static ErrorOr<HealthStatus> Check() => ...

// Fixed: use one or the other
[ApiVersionNeutral]
[Get("/health")]
public static ErrorOr<HealthStatus> Check() => ...
```

### EOE051 - Mapped version not declared

**Severity:** Warning

Endpoint maps to a version that isn't declared with `[ApiVersion]` on the class.

```csharp
// Warning: 2.0 not in declared versions
[ApiVersion("1.0")]
public static class UsersEndpoints
{
    [MapToApiVersion("2.0")]  // EOE051
    [Get("/users")]
    public static ErrorOr<List<User>> GetAll() => ...
}

// Fixed: declare all versions
[ApiVersion("1.0")]
[ApiVersion("2.0")]
public static class UsersEndpoints { }
```

### EOE052 - Asp.Versioning package not referenced

**Severity:** Warning

Endpoint uses API versioning but the `Asp.Versioning.Http` package is not referenced.

```bash
# Fix: install the package
dotnet add package Asp.Versioning.Http
```

### EOE053 - Endpoint missing versioning

**Severity:** Info

Endpoint has no version information but other endpoints in the project use API versioning.

```csharp
// Info: other endpoints use versioning, but this one doesn't
[Get("/legacy")]
public static ErrorOr<Data> GetLegacy() => ...

// Fixed: declare version scope
[ApiVersion("1.0")]
[Get("/legacy")]
public static ErrorOr<Data> GetLegacy() => ...

// Or mark as version-neutral
[ApiVersionNeutral]
[Get("/legacy")]
public static ErrorOr<Data> GetLegacy() => ...
```

### EOE054 - Invalid API version format

**Severity:** Error

`[ApiVersion]` has an invalid format. Use "major.minor" or just "major".

```csharp
// Error: invalid format
[ApiVersion("v1")]  // EOE054
[ApiVersion("1.0.0")]  // EOE054

// Fixed: use valid format
[ApiVersion("1.0")]
[ApiVersion("2")]
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
