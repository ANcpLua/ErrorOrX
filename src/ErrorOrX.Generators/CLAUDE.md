---
See [Root CLAUDE.md](/Users/ancplua/ErrorOrX/CLAUDE.md) for project context.
---

# ErrorOrX.Generators

This project contains the Roslyn source generator and analyzers for ErrorOrX.

## Package Details

- **PackageId**: `ErrorOrX.Generators`
- **Target**: `netstandard2.0` (required for Roslyn analyzers)
- **SDK**: `Microsoft.NET.Sdk` (not ANcpLua.NET.Sdk - PolySharp is needed for C# features)

## What It Does

Converts `ErrorOr<T>` methods with route attributes into ASP.NET Core Minimal API endpoints:

```csharp
[Get("/todos/{id}")]
public static ErrorOr<Todo> GetById(int id) => ...
```

Generates:
- `MapErrorOrEndpoints()` extension method
- Typed `Results<...>` union for OpenAPI
- Automatic parameter binding
- Middleware attribute emission

## Dependencies

- `Microsoft.CodeAnalysis.CSharp` - Roslyn APIs
- `ANcpLua.Roslyn.Utilities` - Bundled in package
- `PolySharp` - C# polyfills for netstandard2.0

## Package Structure

The `.nupkg` contains:
- `analyzers/dotnet/cs/ErrorOrX.Generators.dll` - The generator
- `analyzers/dotnet/cs/ANcpLua.Roslyn.Utilities.dll` - Bundled dependency
- `build/ErrorOrX.Generators.props` - CompilerVisibleProperty definitions
- Dependency on `ErrorOrX` (flows to consumers)
