# Changelog

All notable changes to this project are documented in this file.

## [3.0.0] - 2026-01-06

### Breaking Changes

- **Target framework**: Changed from `netstandard2.0` to `net10.0` only. This library now requires .NET 10+.
- **Immutable error collections**: Changed `List<Error>` to `IReadOnlyList<Error>` in public API. The `Errors` and `ErrorsOrEmptyList` properties now return `IReadOnlyList<Error>` instead of `List<Error>`.

### Changed

- `EmptyErrors.Instance` now returns `Array.Empty<Error>()` instead of a mutable `List<Error>`, preventing accidental mutation of the shared empty instance.
- Uses modern throw helpers (`ArgumentNullException.ThrowIfNull`) instead of manual null checks.

### Removed

- Removed polyfill packages (`Microsoft.Bcl.HashCode`, `Nullable`) - no longer needed with .NET 10.
- Removed `StyleCop.Analyzers` in favor of `ErrorProne.NET` analyzers.

### Added

- Added `ErrorProne.NET.CoreAnalyzers` and `ErrorProne.NET.Structs` for improved code quality analysis.
- Added `JonSkeet.RoslynAnalyzers` for additional code quality checks.

## [1.10.0] - 2024-02-14

### Added

- `ErrorType.Forbidden`
- README to NuGet package

## [1.9.0] - 2024-01-06

### Added

- `ToErrorOr`

## [2.0.0] - 2024-03-26

### Added

- `FailIf`

```csharp
public ErrorOr<TValue> FailIf(Func<TValue, bool> onValue, Error error)
```

```csharp
ErrorOr<int> errorOr = 1;
errorOr.FailIf(x => x > 0, Error.Failure());
```

### Breaking Changes

- `Then` that receives an action is now called `ThenDo`

```diff
-public ErrorOr<TValue> Then(Action<TValue> action)
+public ErrorOr<TValue> ThenDo(Action<TValue> action)
```

```diff
-public static async Task<ErrorOr<TValue>> Then<TValue>(this Task<ErrorOr<TValue>> errorOr, Action<TValue> action)
+public static async Task<ErrorOr<TValue>> ThenDo<TValue>(this Task<ErrorOr<TValue>> errorOr, Action<TValue> action)
```

- `ThenAsync` that receives an action is now called `ThenDoAsync`

```diff
-public async Task<ErrorOr<TValue>> ThenAsync(Func<TValue, Task> action)
+public async Task<ErrorOr<TValue>> ThenDoAsync(Func<TValue, Task> action)
```

```diff
-public static async Task<ErrorOr<TValue>> ThenAsync<TValue>(this Task<ErrorOr<TValue>> errorOr, Func<TValue, Task> action)
+public static async Task<ErrorOr<TValue>> ThenDoAsync<TValue>(this Task<ErrorOr<TValue>> errorOr, Func<TValue, Task> action)
```
