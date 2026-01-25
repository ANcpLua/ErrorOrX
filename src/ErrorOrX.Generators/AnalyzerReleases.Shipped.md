## Release 2.0.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
EOE001 | ErrorOr.Endpoints | Error | Invalid return type
EOE002 | ErrorOr.Endpoints | Error | Handler must be static
EOE003 | ErrorOr.Endpoints | Error | Route parameter not bound
EOE004 | ErrorOr.Endpoints | Error | Duplicate route
EOE005 | ErrorOr.Endpoints | Error | Invalid route pattern
EOE006 | ErrorOr.Endpoints | Error | Multiple body sources
EOE007 | ErrorOr.Endpoints | Warning | Type not AOT-serializable
EOE009 | ErrorOr.Endpoints | Warning | Body on read-only HTTP method
EOE010 | ErrorOr.Endpoints | Warning | [AcceptedResponse] on read-only method
EOE011 | ErrorOr.Endpoints | Error | Invalid [FromRoute] type
EOE012 | ErrorOr.Endpoints | Error | Invalid [FromQuery] type
EOE013 | ErrorOr.Endpoints | Error | Invalid [AsParameters] type
EOE014 | ErrorOr.Endpoints | Error | [AsParameters] type has no constructor
EOE016 | ErrorOr.Endpoints | Error | Invalid [FromHeader] type
EOE017 | ErrorOr.Endpoints | Error | Anonymous return type not supported
EOE018 | ErrorOr.Endpoints | Error | Nested [AsParameters] not supported
EOE019 | ErrorOr.Endpoints | Error | Nullable [AsParameters] not supported
EOE020 | ErrorOr.Endpoints | Error | Inaccessible type in endpoint
EOE021 | ErrorOr.Endpoints | Error | Type parameter not supported
EOE023 | ErrorOr.Endpoints | Warning | Route constraint type mismatch
EOE025 | ErrorOr.Endpoints | Warning | Ambiguous parameter binding
EOE030 | ErrorOr.Endpoints | Info | Too many result types
EOE032 | ErrorOr.Endpoints | Warning | Unknown error factory
EOE033 | ErrorOr.Endpoints | Error | Undocumented interface call
EOE040 | ErrorOr.Endpoints | Warning | Missing CamelCase policy
EOE050 | ErrorOr.Endpoints | Warning | Version-neutral with mappings
EOE051 | ErrorOr.Endpoints | Warning | Mapped version not declared
EOE052 | ErrorOr.Endpoints | Warning | Asp.Versioning package not referenced
EOE053 | ErrorOr.Endpoints | Info | Endpoint missing versioning
EOE054 | ErrorOr.Endpoints | Error | Invalid API version format
AOT001 | ErrorOr.AotSafety | Warning | Activator.CreateInstance is not AOT-safe
AOT002 | ErrorOr.AotSafety | Warning | Type.GetType is not AOT-safe
AOT003 | ErrorOr.AotSafety | Warning | Reflection over members is not AOT-safe
AOT004 | ErrorOr.AotSafety | Warning | Expression.Compile is not AOT-safe
AOT005 | ErrorOr.AotSafety | Warning | 'dynamic' is not AOT-safe

### Removed Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------

### Changed Rules

Rule ID | New Category | New Severity | Old Category | Old Severity | Notes
--------|--------------|--------------|--------------|--------------|------
