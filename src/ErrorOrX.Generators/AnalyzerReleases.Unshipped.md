### New Rules

 Rule ID | Category          | Severity | Notes
---------|-------------------|----------|---------------------------------------
 EOE041  | ErrorOr.Endpoints | Error    | Missing JsonSerializerContext for AOT
 EOE055  | ErrorOr.Endpoints | Warning  | Duplicate route parameter binding

### Removed Rules

 Rule ID | Category | Severity | Notes
---------|----------|----------|-------

### Changed Rules

 Rule ID | New Category      | New Severity | Old Category      | Old Severity | Notes
---------|-------------------|--------------|-------------------|--------------|------------------------------------------------------------------
 EOE007  | ErrorOr.Endpoints | Error        | ErrorOr.Endpoints | Warning      | Type not in JsonSerializerContext is now an error for AOT safety
 EOE025  | ErrorOr.Endpoints | Error        | ErrorOr.Endpoints | Warning      | Ambiguous parameter binding is now an error
