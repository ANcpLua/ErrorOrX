## Release 2.0.0

### New Rules

 Rule ID | Category          | Severity | Notes
---------|-------------------|----------|--------------------------------------------
 EOE001  | ErrorOr.Endpoints | Error    | Invalid return type
 EOE002  | ErrorOr.Endpoints | Error    | Handler must be static
 EOE003  | ErrorOr.Endpoints | Error    | Route parameter not bound
 EOE004  | ErrorOr.Endpoints | Error    | Duplicate route
 EOE005  | ErrorOr.Endpoints | Error    | Invalid route pattern
 EOE006  | ErrorOr.Endpoints | Error    | Multiple body sources
 EOE007  | ErrorOr.Endpoints | Error    | Type not AOT-serializable
 EOE008  | ErrorOr.Endpoints | Warning  | Body on read-only HTTP method
 EOE009  | ErrorOr.Endpoints | Warning  | [AcceptedResponse] on read-only method
 EOE010  | ErrorOr.Endpoints | Error    | Invalid [FromRoute] type
 EOE011  | ErrorOr.Endpoints | Error    | Invalid [FromQuery] type
 EOE012  | ErrorOr.Endpoints | Error    | Invalid [AsParameters] type
 EOE013  | ErrorOr.Endpoints | Error    | [AsParameters] type has no constructor
 EOE014  | ErrorOr.Endpoints | Error    | Invalid [FromHeader] type
 EOE015  | ErrorOr.Endpoints | Error    | Anonymous return type not supported
 EOE016  | ErrorOr.Endpoints | Error    | Nested [AsParameters] not supported
 EOE017  | ErrorOr.Endpoints | Error    | Nullable [AsParameters] not supported
 EOE018  | ErrorOr.Endpoints | Error    | Inaccessible type in endpoint
 EOE019  | ErrorOr.Endpoints | Error    | Type parameter not supported
 EOE020  | ErrorOr.Endpoints | Warning  | Route constraint type mismatch
 EOE021  | ErrorOr.Endpoints | Error    | Ambiguous parameter binding
 EOE022  | ErrorOr.Endpoints | Info     | Too many result types
 EOE023  | ErrorOr.Endpoints | Warning  | Unknown error factory
 EOE024  | ErrorOr.Endpoints | Error    | Undocumented interface call
 EOE025  | ErrorOr.Endpoints | Warning  | Missing CamelCase policy
 EOE026  | ErrorOr.Endpoints | Error    | Missing JsonSerializerContext for AOT
 EOE027  | ErrorOr.Endpoints | Warning  | Version-neutral with mappings
 EOE028  | ErrorOr.Endpoints | Warning  | Mapped version not declared
 EOE029  | ErrorOr.Endpoints | Warning  | Asp.Versioning package not referenced
 EOE030  | ErrorOr.Endpoints | Info     | Endpoint missing versioning
 EOE031  | ErrorOr.Endpoints | Error    | Invalid API version format
 EOE032  | ErrorOr.Endpoints | Warning  | Duplicate route parameter binding
 EOE033  | ErrorOr.Endpoints | Warning  | Handler method name not PascalCase
 EOE034  | ErrorOr.Endpoints | Warning  | Activator.CreateInstance is not AOT-safe
 EOE035  | ErrorOr.Endpoints | Warning  | Type.GetType is not AOT-safe
 EOE036  | ErrorOr.Endpoints | Warning  | Reflection over members is not AOT-safe
 EOE037  | ErrorOr.Endpoints | Warning  | Expression.Compile is not AOT-safe
 EOE038  | ErrorOr.Endpoints | Warning  | 'dynamic' is not AOT-safe
 EOE039  | ErrorOr.Endpoints | Info     | DataAnnotations validation uses reflection
 EOE040  | ErrorOr.Endpoints | Warning  | JsonSerializerContext missing CamelCase
 EOE041  | ErrorOr.Endpoints | Warning  | JsonSerializerContext missing error types

### Removed Rules

 Rule ID | Category | Severity | Notes
---------|----------|----------|-------

### Changed Rules

 Rule ID | New Category | New Severity | Old Category | Old Severity | Notes
---------|--------------|--------------|--------------|--------------|-------
