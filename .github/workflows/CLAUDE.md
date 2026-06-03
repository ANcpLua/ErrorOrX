# GitHub Workflows

CI/CD workflows for ErrorOrX.

## Workflows

| Workflow             | Trigger              | Purpose                                                            |
|----------------------|----------------------|-------------------------------------------------------------------|
| `nuget-publish.yml`  | Tag push (`v*`)      | Pack + `dotnet nuget push --skip-duplicate` to NuGet (version from the tag) |
| `aot-publish.yml`    | PR / push to `main`  | **Gate**: Native AOT publish of a package-based sample; fails on any trim/AOT `ILxxxx` warning |
| `version-check.yml`  | PR / push            | Audits version consistency                                        |
| `auto-merge.yml`     | PR                   | Auto-merges eligible (e.g. dependency) PRs                        |
| `sdk-hardening.yml`  | PR / push            | Audits `ANcpLua.NET.Sdk` usage / hardening                        |

There is **no** `ci.yml` or `release.yml`. Publishing happens on tag push via `nuget-publish.yml`; a GitHub
release must still be created manually afterwards (`gh release create vX.Y.Z --generate-notes`).

## AOT gate (why it matters)

`aot-publish.yml` is the safety net for the one AOT failure mode with no compile-time signal: a generated
endpoint slipping onto the reflection `RequestDelegateFactory` path, or generated validation emitting a
`[RequiresUnreferencedCode]` call site. A green `dotnet build` cannot catch these — only a real
`dotnet publish -r <rid> -p:PublishAot=true` surfaces the `IL2xxx`/`IL3xxx` warnings, so the gate greps the
publish log and fails on any `ILxxxx`.

## Artifacts

| Package | Location |
|---------|----------|
| ErrorOrX | `lib/net10.0/ErrorOrX.dll` |
| ErrorOrX.Generators | `analyzers/dotnet/cs/ErrorOrX.Generators.dll` |
