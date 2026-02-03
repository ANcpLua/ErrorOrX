# ErrorOrX.Generators.Tests

Snapshot tests for the ErrorOrX source generator. Target: `net10.0`.

## Running Tests

```bash
dotnet test --project tests/ErrorOrX.Generators.Tests
dotnet test --project tests/ErrorOrX.Generators.Tests --filter-class "*ResultsUnionTypeBuilder*"
dotnet test --project tests/ErrorOrX.Generators.Tests --filter-method "*Payload_Returns_Ok200*"
```

## Test Categories

### Generator Output Tests

Snapshot tests verify generated code matches expected output:

| Test Area | What It Tests |
|-----------|---------------|
| Basic endpoints | Simple GET/POST/PUT/DELETE handlers |
| Parameter binding | Route, query, body, service inference |
| Results union | `Results<Ok<T>, NotFound<PD>, ...>` generation |
| Middleware | Authorization, rate limiting, caching emission |
| JSON context | `ErrorOrJsonContext` generation |

### Analyzer Tests

Verify diagnostics are emitted correctly:

| Diagnostic | Test Coverage |
|------------|---------------|
| EOE001 | Invalid return type |
| EOE002 | Handler must be static |
| EOE003-005 | Route validation |
| EOE021 | Ambiguous parameter binding |
| EOE034-038 | AOT safety (Activator, Type.GetType, reflection, Expression.Compile, dynamic) |
| EOE039-041 | Validation reflection, JSON context warnings |

## Snapshot Testing

Uses `ANcpLua.Roslyn.Utilities.Testing` for generator snapshot verification:

```csharp
[Fact]
public async Task Endpoint_With_Body_Generates_AcceptsMetadata()
{
    var source = """
        [Post("/todos")]
        public static ErrorOr<Todo> Create(CreateTodoRequest req) => ...
        """;

    var result = await GeneratorTestHelper.RunGenerator(source);

    await Verify(result);
}
```

Snapshots stored in `Snapshots/` directory.

## Test Infrastructure

| File | Purpose |
|------|---------|
| `GeneratorTestHelper.cs` | Creates compilation, runs generator |
| `TestCompilationHelper.cs` | Reference assembly setup |
| `SnapshotSettings.cs` | Verify configuration |

## Dependencies

| Package | Purpose |
|---------|---------|
| ANcpLua.Roslyn.Utilities.Testing | Generator test framework |
| xunit.v3.mtp-v2 | xUnit v3 with MTP |
| AwesomeAssertions | Fluent assertions |
| Verify.Xunit | Snapshot testing |
