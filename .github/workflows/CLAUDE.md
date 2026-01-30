# GitHub Workflows

CI/CD workflows for ErrorOrX.

## Workflows

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `ci.yml` | Push/PR to main | Build, test, pack |
| `release.yml` | Tag push | Publish to NuGet |

## CI Pipeline

1. **Build** - `dotnet build ErrorOrX.slnx`
2. **Test** - `dotnet test --solution ErrorOrX.slnx`
3. **Pack** - `dotnet pack -c Release`

## Artifacts

| Package | Location |
|---------|----------|
| ErrorOrX | `lib/net10.0/ErrorOrX.dll` |
| ErrorOrX.Generators | `analyzers/dotnet/cs/ErrorOrX.Generators.dll` |
