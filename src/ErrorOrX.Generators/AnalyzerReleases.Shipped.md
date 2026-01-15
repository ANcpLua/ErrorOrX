## Release 2.0.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
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
EOE023 | ErrorOr.Endpoints | Warning | Route constraint type mismatch
EOE025 | ErrorOr.Endpoints | Error | Ambiguous parameter binding
EOE030 | ErrorOr.Endpoints | Info | Too many result types
EOE032 | ErrorOr.Endpoints | Warning | Unknown error factory
EOE033 | ErrorOr.Endpoints | Error | Undocumented interface call
EOE040 | ErrorOr.Endpoints | Warning | Missing CamelCase policy
AOTJ001 | ErrorOr.AotJson | Warning | JsonSerializerContext not registered
AOTJ002 | ErrorOr.AotJson | Info | Missing [AotJson] attribute
AOTJ003 | ErrorOr.AotJson | Warning | Duplicate [AotJson] contexts
AOTJ004 | ErrorOr.AotJson | Warning | Type not serializable
AOTJ005 | ErrorOr.AotJson | Error | [AotJson] on non-partial class
AOT001 | ErrorOr.AotSafety | Warning | Activator.CreateInstance is not AOT-safe
AOT002 | ErrorOr.AotSafety | Warning | Type.GetType is not AOT-safe
AOT003 | ErrorOr.AotSafety | Warning | Reflection over members is not AOT-safe
AOT004 | ErrorOr.AotSafety | Warning | Expression.Compile is not AOT-safe
AOT005 | ErrorOr.AotSafety | Warning | 'dynamic' is not AOT-safe
