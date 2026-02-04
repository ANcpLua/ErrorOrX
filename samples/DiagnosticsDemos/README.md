# ErrorOrX Diagnostics Demos

This project demonstrates all ErrorOrX analyzer and generator diagnostics (EOE001-EOE038).

Each file in the `Demos/` folder contains:

- A clear explanation of what the diagnostic checks
- Code that would **trigger** the diagnostic (commented out)
- The **fixed** version showing the correct pattern

## Quick Start

```bash
# Build to see diagnostics in IDE
dotnet build samples/DiagnosticsDemos

# View generated files
ls samples/DiagnosticsDemos/obj/GeneratedFiles
```

## Viewing Diagnostics in Your IDE

1. Open the solution in Visual Studio, Rider, or VS Code
2. Navigate to any file in `Demos/`
3. Uncomment the "TRIGGERS" code sections
4. The IDE will show squiggles and error/warning messages
5. Hover over the squiggle to see the diagnostic message

## Diagnostics Reference

### EOE001-007: Core Validation

| ID     | Name                      | Severity | Description                                                                      |
|--------|---------------------------|----------|----------------------------------------------------------------------------------|
| EOE001 | Invalid return type       | Error    | Handler must return `ErrorOr<T>`, `Task<ErrorOr<T>>`, or `ValueTask<ErrorOr<T>>` |
| EOE002 | Handler must be static    | Error    | Instance methods cannot be used with source generation                           |
| EOE003 | Route parameter not bound | Error    | Route has `{param}` but no method parameter captures it                          |
| EOE004 | Duplicate route           | Error    | Same route + HTTP method registered by multiple handlers                         |
| EOE005 | Invalid route pattern     | Error    | Route pattern syntax is invalid (unclosed braces, etc.)                          |
| EOE006 | Multiple body sources     | Error    | Only one of `[FromBody]`, `[FromForm]`, Stream, or PipeReader allowed            |
| EOE007 | Type not AOT-serializable | Error    | Type not in any `[JsonSerializable]` context for AOT                             |

### EOE008-014: Parameter Binding Validation

| ID     | Name                                   | Severity | Description                                                   |
|--------|----------------------------------------|----------|---------------------------------------------------------------|
| EOE008 | Body on read-only method               | Warning  | GET/DELETE with request body is discouraged                   |
| EOE009 | [AcceptedResponse] on read-only method | Warning  | 202 Accepted unusual for GET/DELETE                           |
| EOE010 | Invalid [FromRoute] type               | Error    | Route parameter must be primitive or have TryParse            |
| EOE011 | Invalid [FromQuery] type               | Error    | Query parameter must be primitive or collection of primitives |
| EOE012 | Invalid [AsParameters] type            | Error    | Must be class or struct, not primitive                        |
| EOE013 | [AsParameters] no constructor          | Error    | Type must have accessible constructor                         |
| EOE014 | Invalid [FromHeader] type              | Error    | Header must be string or primitive with TryParse              |

### EOE015-019: Return Type and Parameter Type Validation

| ID     | Name                         | Severity | Description                                                  |
|--------|------------------------------|----------|--------------------------------------------------------------|
| EOE015 | Anonymous return type        | Error    | Anonymous types cannot be serialized; use named types        |
| EOE016 | Nested [AsParameters]        | Error    | Recursive parameter expansion not supported                  |
| EOE017 | Nullable [AsParameters]      | Error    | Parameter expansion requires concrete instance               |
| EOE018 | Inaccessible type            | Error    | Private/protected types cannot be accessed by generated code |
| EOE019 | Type parameter not supported | Error    | Open generic type parameters cannot be used in return types  |

### EOE020-021: Route Constraint Validation

| ID     | Name                           | Severity | Description                                                                                   |
|--------|--------------------------------|----------|-----------------------------------------------------------------------------------------------|
| EOE020 | Route constraint type mismatch | Warning  | `{id:int}` requires int parameter, not string                                                 |
| EOE021 | Ambiguous parameter binding    | Error    | Complex type on GET/DELETE needs explicit `[AsParameters]`, `[FromBody]`, or `[FromServices]` |

### EOE022-024: Result Types and Error Factories

| ID     | Name                        | Severity | Description                                                             |
|--------|-----------------------------|----------|-------------------------------------------------------------------------|
| EOE022 | Too many result types       | Info     | Endpoint exceeds Results<...> max arity (6 types)                       |
| EOE023 | Unknown error factory       | Warning  | `Error.Custom()` doesn't map to known HTTP status                       |
| EOE024 | Undocumented interface call | Error    | Interface returning ErrorOr needs `[ProducesError]` or `[ReturnsError]` |

### EOE025-026: JSON/AOT Validation

| ID     | Name                          | Severity | Description                                             |
|--------|-------------------------------|----------|---------------------------------------------------------|
| EOE025 | Missing CamelCase policy      | Warning  | JsonSerializerContext should use CamelCase for web APIs |
| EOE026 | Missing JsonSerializerContext | Error    | Endpoint with body needs `[JsonSerializable]` for AOT   |

### EOE027-031: API Versioning

| ID     | Name                          | Severity | Description                                                                             |
|--------|-------------------------------|----------|-----------------------------------------------------------------------------------------|
| EOE027 | Version-neutral with mappings | Warning  | `[ApiVersionNeutral]` and `[MapToApiVersion]` are mutually exclusive                    |
| EOE028 | Mapped version not declared   | Warning  | `[MapToApiVersion("X")]` requires `[ApiVersion("X")]` on class                          |
| EOE029 | Package not referenced        | Warning  | Asp.Versioning.Http package needed for versioning attributes                            |
| EOE030 | Endpoint missing versioning   | Info     | Other endpoints use versioning; consider adding `[ApiVersion]` or `[ApiVersionNeutral]` |
| EOE031 | Invalid version format        | Error    | Use "major.minor" or "major", not "v1" or "1.0.0"                                       |

### EOE032-033: Route Parameter and Naming Validation

| ID     | Name                              | Severity | Description                                                  |
|--------|-----------------------------------|----------|--------------------------------------------------------------|
| EOE032 | Duplicate route parameter binding | Warning  | Multiple parameters bind to same route parameter             |
| EOE033 | Method name not PascalCase        | Warning  | Handler methods should use PascalCase (GetById, not getById) |

### EOE034-038: AOT Safety

| ID     | Name                     | Severity | Description                                               |
|--------|--------------------------|----------|-----------------------------------------------------------|
| EOE034 | Activator.CreateInstance | Warning  | Uses reflection; use explicit construction or factories   |
| EOE035 | Type.GetType             | Warning  | Types may be trimmed; use typeof() or registry            |
| EOE036 | Reflection over members  | Warning  | GetProperties/GetMethods may fail; members may be trimmed |
| EOE037 | Expression.Compile       | Warning  | Runtime code generation not supported in AOT              |
| EOE038 | 'dynamic' keyword        | Warning  | Runtime binding not supported in AOT                      |

## File Structure

```
samples/DiagnosticsDemos/
├── DiagnosticsDemos.csproj    # Project file with references
├── README.md                   # This file
├── GlobalUsings.cs            # Common using directives
└── Demos/
    ├── EOE001_InvalidReturnType.cs
    ├── EOE002_NonStaticHandler.cs
    ├── EOE003_RouteParameterNotBound.cs
    ├── EOE004_DuplicateRoute.cs
    ├── EOE005_InvalidRoutePattern.cs
    ├── EOE006_MultipleBodySources.cs
    ├── EOE007_TypeNotInJsonContext.cs
    ├── EOE008_BodyOnReadOnlyMethod.cs
    ├── EOE009_AcceptedOnReadOnlyMethod.cs
    ├── EOE010_InvalidFromRouteType.cs
    ├── EOE011_InvalidFromQueryType.cs
    ├── EOE012_InvalidAsParametersType.cs
    ├── EOE013_AsParametersNoConstructor.cs
    ├── EOE014_InvalidFromHeaderType.cs
    ├── EOE015_AnonymousReturnType.cs
    ├── EOE016_NestedAsParameters.cs
    ├── EOE017_NullableAsParameters.cs
    ├── EOE018_InaccessibleType.cs
    ├── EOE019_TypeParameterNotSupported.cs
    ├── EOE020_RouteConstraintTypeMismatch.cs
    ├── EOE021_AmbiguousParameterBinding.cs
    ├── EOE022_TooManyResultTypes.cs
    ├── EOE023_UnknownErrorFactory.cs
    ├── EOE024_UndocumentedInterfaceCall.cs
    ├── EOE025_MissingCamelCasePolicy.cs
    ├── EOE026_MissingJsonContextForBody.cs
    ├── EOE027_VersionNeutralWithMappings.cs
    ├── EOE028_MappedVersionNotDeclared.cs
    ├── EOE029_ApiVersioningPackageNotReferenced.cs
    ├── EOE030_EndpointMissingVersioning.cs
    ├── EOE031_InvalidApiVersionFormat.cs
    ├── EOE032_DuplicateRouteParameterBinding.cs
    ├── EOE033_MethodNameNotPascalCase.cs
    ├── EOE034_ActivatorCreateInstance.cs
    ├── EOE035_TypeGetType.cs
    ├── EOE036_ReflectionOverMembers.cs
    ├── EOE037_ExpressionCompile.cs
    └── EOE038_DynamicKeyword.cs
```

## Severity Levels

- **Error**: Code will not compile or will fail at runtime. Must be fixed.
- **Warning**: Code compiles but may have issues. Should be reviewed.
- **Info**: Informational; may indicate missing best practices.

## Suppressing Diagnostics

If you need to suppress a diagnostic:

```csharp
// Suppress single occurrence
#pragma warning disable EOE008
[Get("/search")]
public static ErrorOr<string> Search([FromBody] SearchRequest req) => "ok";
#pragma warning restore EOE008

// Suppress in .editorconfig
[*.cs]
dotnet_diagnostic.EOE008.severity = none

// Suppress in project file
<PropertyGroup>
  <NoWarn>$(NoWarn);EOE008</NoWarn>
</PropertyGroup>
```
