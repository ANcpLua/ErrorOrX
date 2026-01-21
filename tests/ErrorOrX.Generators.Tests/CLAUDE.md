---
See [Root CLAUDE.md](/Users/ancplua/ErrorOrX/CLAUDE.md) for project context.
See [Generator CLAUDE.md](/Users/ancplua/ErrorOrX/src/ErrorOrX.Generators/CLAUDE.md) for generator implementation details.
---

# ErrorOrX.Generators.Tests

Unit tests for the ErrorOrX source generators and analyzers.

## Test Framework

- **xUnit v3** with Microsoft Testing Platform (MTP)
- **ANcpLua.Roslyn.Utilities.Testing** for generator testing utilities

## Running Tests

```bash
# Run all generator tests
dotnet test --project tests/ErrorOrX.Generators.Tests

# Run specific test class (xUnit v3 MTP syntax)
dotnet test --project tests/ErrorOrX.Generators.Tests --filter-class "*Binding*"

# Run specific test method
dotnet test --project tests/ErrorOrX.Generators.Tests --filter-method "*Discovers_Types*"
```

## Test Patterns

### Generator Tests (using GeneratorTestBase)

```csharp
public sealed class MyGeneratorTests : GeneratorTestBase
{
    [Fact]
    public async Task My_Test()
    {
        const string source = """
            // Test source code here
            """;

        using var scope = TestConfiguration.WithAdditionalReferences(typeof(SomeType));
        using var result = await Test<MyGenerator>.Run(source, TestContext.Current.CancellationToken);

        result
            .Produces("ExpectedFile.g.cs")
            .IsClean();  // No diagnostics

        var file = result.Files.FirstOrDefault(f =>
            f.HintName.Contains("ExpectedFile", StringComparison.Ordinal));
        Assert.NotNull(file);
        Assert.Contains("expected content", file.Content, StringComparison.Ordinal);
    }
}
```

### Analyzer Tests (using AnalyzerTestBase)

```csharp
public class MyAnalyzerTests : AnalyzerTestBase<MyAnalyzer>
{
    [Fact]
    public async Task Reports_Diagnostic()
    {
        const string source = """
            // {|EOE001:markedSpan|} - diagnostic expected here
            """;

        await VerifyAsync(source);
    }
}
```

## Important Testing Notes

### ForAttributeWithMetadataName Limitation

Generators can only see types from their own `PostInitializationOutput`. Tests must define route attributes in the test
source:

```csharp
private const string RouteAttributesSource = """
    namespace ErrorOr
    {
        [System.AttributeUsage(System.AttributeTargets.Method)]
        public sealed class GetAttribute : System.Attribute
        {
            public GetAttribute(string route) => Route = route;
            public string Route { get; }
        }
        // ... other attributes

        public readonly struct ErrorOr<T>
        {
            public T Value { get; }
        }
    }
    """;

// Then in tests:
const string source = """
    using ErrorOr;

    [Get("/test")]
    public static ErrorOr<string> Get() => default;
    """ + RouteAttributesSource;
```

### xUnit v3 Patterns

```csharp
// Use TestContext.Current.CancellationToken for async tests
await Test<MyGenerator>.Run(source, TestContext.Current.CancellationToken);

// Use StringComparison.Ordinal for string assertions
Assert.Contains("text", content, StringComparison.Ordinal);
f.HintName.Contains("Endpoint", StringComparison.Ordinal);

// Use Assert.DoesNotContain with predicate, not Assert.Empty with Where
Assert.DoesNotContain(result.Files, f => f.HintName.Contains("X", StringComparison.Ordinal));
```

### Required Type References

When testing generators that use ASP.NET Core types:

```csharp
private static readonly Type[] RequiredTypes =
[
    typeof(HttpContext),
    typeof(FromBodyAttribute),
    typeof(IServiceCollection),
    typeof(JsonSerializerContext),
    typeof(JsonSerializableAttribute),
    typeof(Error)  // From ErrorOrX
];

using var scope = TestConfiguration.WithAdditionalReferences(RequiredTypes);
```

## Test Files

| File                              | Tests                               |
|-----------------------------------|-------------------------------------|
| `DuplicateRouteTests.cs`          | EOE004 duplicate route detection    |
| `ErrorOrEndpointAnalyzerTests.cs` | EOE001, EOE002, etc. analyzer rules |
| `ParameterBindingTests.cs`        | Smart parameter binding inference   |

## Debugging Generator Output

To see what the generator produces:

```bash
# Build sample project
dotnet build samples/ErrorOrX.Sample

# View generated files
ls samples/ErrorOrX.Sample/obj/Debug/net10.0/generated/ErrorOrX.Generators/
```

## Common Test Failures

| Symptom                            | Cause                                     | Fix                                                         |
|------------------------------------|-------------------------------------------|-------------------------------------------------------------|
| `Assert.Contains` finds wrong file | Multiple files match pattern              | Use more specific pattern like `"AppJsonSerializerContext"` |
| Route attributes not found         | `ForAttributeWithMetadataName` limitation | Add `RouteAttributesSource` to test source                  |
| `CA1307` error                     | Missing StringComparison                  | Add `StringComparison.Ordinal`                              |
| `xUnit1051` error                  | Missing CancellationToken                 | Use `TestContext.Current.CancellationToken`                 |
| Types not discovered               | Missing type references                   | Add types to `TestConfiguration.WithAdditionalReferences()` |
