---
See [Root CLAUDE.md](/Users/ancplua/ErrorOrX/CLAUDE.md) for project context.
---

# ErrorOrX Runtime Library

This project contains the runtime types for the ErrorOrX library:

- `ErrorOr<T>` - The discriminated union type
- `Error` - Error value with code, description, type, and metadata
- `ErrorType` - Enum of error categories (Validation, NotFound, etc.)
- `Result` - Factory for built-in result types (Deleted, Updated, Created, Success)
- Fluent API extensions: `Then`, `Else`, `Match`, `Switch`, `FailIf`

## Package Details

- **PackageId**: `ErrorOrX`
- **Target**: `net10.0`
- **Namespace**: `ErrorOr`

## Dependencies

- `Microsoft.AspNetCore.App` framework reference (for `TypedResults` helpers)

## Consumers

This package is referenced as a dependency by `ErrorOrX.Generators`. End users only need to reference `ErrorOrX.Generators` to get both packages.
