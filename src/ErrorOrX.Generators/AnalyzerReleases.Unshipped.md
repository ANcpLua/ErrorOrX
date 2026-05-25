### New Rules

 Rule ID | Category | Severity | Notes
---------|----------|----------|-------

### Removed Rules

 Rule ID | Category          | Severity | Notes
---------|-------------------|----------|------------------------------------------
 EOE034  | ErrorOr.Endpoints | Warning  | Activator.CreateInstance is not AOT-safe
 EOE035  | ErrorOr.Endpoints | Warning  | Type.GetType is not AOT-safe
 EOE036  | ErrorOr.Endpoints | Warning  | Reflection over members is not AOT-safe
 EOE037  | ErrorOr.Endpoints | Warning  | Expression.Compile is not AOT-safe
 EOE038  | ErrorOr.Endpoints | Warning  | 'dynamic' is not AOT-safe

### Changed Rules

 Rule ID | New Category      | New Severity | Old Category      | Old Severity | Notes
---------|-------------------|--------------|-------------------|--------------|--------------------------------------------------------------------------------------------------------------------------------------------------
 EOE015  | ErrorOr.Endpoints | Warning      | ErrorOr.Endpoints | Error        | Behavior change: now detects ErrorOr<object> / ErrorOr<dynamic> (the reachable user mistake) instead of the unreachable anonymous-type-in-signature check. Severity softened from Error to Warning to avoid breaking consumer builds on upgrade; suppress with #pragma if you intentionally use object-typed payloads with a registered JsonSerializerContext.
