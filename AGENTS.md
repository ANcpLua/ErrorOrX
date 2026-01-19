# AGENTS.md - ErrorOrX Personal Custom Instructions

Last updated: 2025-12-14

## Response Preferences
- Prefer .NET 10 / C# 14 and cutting-edge APIs unless incompatible with repo constraints.
- If uncertain about versions, verify via search and state the verified version.
- Standard responses end with 1-2 follow-ups:
  - Cutting-edge alternative (current, non-deprecated API/pattern/package)
  - Enterprise pattern (established approach for production/scale)
- When context is incomplete:
  - Provide one working vertical slice (extractable, compilable).
  - Optionally include a short todo list and ask: "more detail or less?"
  - All package versions must be explicit.

## Package Versioning Rules
- Always include exact package versions in code/csproj and ask for the current version.
- Confirm central versioning via `Directory.Packages.props` and any automated NUKE component if present.
- Follow SemVer, prefer LTS when available, and keep strongly-typed module boundaries.

## Avoid These Anti-Patterns
- Outdated or unspecified package versions.
- Incremental, low-value suggestions without a clear deliverable.
- Vague "it depends" responses without a concrete path.
- Unstructured discussion without a working outcome.

## ErrorOrX-Specific Engineering Guidance
- Respect repo toolchain:
  - `global.json` pins .NET SDK 10.0.102; `Directory.Build.props` sets `LangVersion=preview`.
  - Generator projects target `netstandard2.0`; avoid APIs not available on that TFM.
- Preserve the AOT-safe wrapper pattern in generated endpoints:
  - Use typed `MapGet/MapPost/...` and wrapper `Task` + `IResult.ExecuteAsync`.
  - Keep `AcceptsMetadata(string[] contentTypes, Type? requestType)` signature order.
- Keep generator and analyzer invariants:
  - Attributes are emitted in `ErrorOrEndpointGenerator` so `ForAttributeWithMetadataName` can see them.
  - OpenAPI transformers follow strict 1:1 attribute-to-transformer mapping.
  - Diagnostics are defined in `Descriptors.cs` and documented in `docs/diagnostics.md`.
- Parameter binding changes must update:
  - `ParameterBinding.cs`, `docs/parameter-binding.md`, and relevant diagnostics (EOE0xx).
- JSON context generation:
  - Honor `ErrorOrGenerateJsonContext` and existing user contexts.
  - Emit missing-types guidance and keep CamelCase warning behavior.
- Incremental generator best practices:
  - Use `EquatableArray`, `DiagnosticFlow`, and avoid caching Roslyn symbols in models.
  - Add `WithTrackingName()` on custom steps and keep caching tests passing.
- Tests and docs:
  - Update generator tests and snapshots in `tests/ErrorOrX.Generators.Tests`.
  - Keep README and docs aligned with behavior changes.
