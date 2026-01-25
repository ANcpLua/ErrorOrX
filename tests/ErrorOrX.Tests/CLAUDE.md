---
See [Root CLAUDE.md](/Users/ancplua/ErrorOrX/CLAUDE.md) for project context.
---

# ErrorOrX.Tests

Unit tests for the ErrorOrX runtime library.

## Package Details

- **Target**: `net10.0`
- **SDK**: `ANcpLua.NET.Sdk`
- **Namespace**: `ErrorOrX.Tests`

## Test Framework

| Package           | Version | Purpose                                  |
|-------------------|---------|------------------------------------------|
| xunit.v3.mtp-v2   | 3.2.2   | xUnit v3 with Microsoft Testing Platform |
| AwesomeAssertions | 9.3.0   | Fluent assertions                        |

## Running Tests

```bash
# Run all runtime tests
dotnet test --project tests/ErrorOrX.Tests/ErrorOrX.Tests.csproj

# Run specific test class (xUnit v3 MTP syntax)
dotnet test --project tests/ErrorOrX.Tests --filter-class "*ThenTests*"

# Run specific test method
dotnet test --project tests/ErrorOrX.Tests --filter-method "*Should_Return_Value*"
```

## Test Categories

### ErrorOr Tests (`ErrorOr/`)

| Test File                       | Coverage                             |
|---------------------------------|--------------------------------------|
| `ErrorOr.InstantiationTests.cs` | Construction, implicit conversions   |
| `ErrorOr.EqualityTests.cs`      | IEquatable implementation, operators |
| `ErrorOr.HashCodeTests.cs`      | GetHashCode consistency              |
| `ErrorOr.ImmutabilityTests.cs`  | Immutability guarantees              |
| `ErrorOr.InterfaceTests.cs`     | IErrorOr non-generic interface       |

### Fluent API Tests (`ErrorOr/`)

| Test File                          | Coverage                          |
|------------------------------------|-----------------------------------|
| `ErrorOr.ThenTests.cs`             | Then chaining (sync)              |
| `ErrorOr.ThenAsyncTests.cs`        | ThenAsync variants                |
| `ErrorOr.ElseTests.cs`             | Else fallback (sync)              |
| `ErrorOr.ElseAsyncTests.cs`        | ElseAsync variants                |
| `ErrorOr.MatchTests.cs`            | Match transformation (sync)       |
| `ErrorOr.MatchAsyncTests.cs`       | MatchAsync variants               |
| `ErrorOr.SwitchTests.cs`           | Switch side effects (sync)        |
| `ErrorOr.SwitchAsyncTests.cs`      | SwitchAsync variants              |
| `ErrorOr.FailIfTests.cs`           | FailIf conditional failure (sync) |
| `ErrorOr.FailIfAsyncTests.cs`      | FailIfAsync variants              |
| `ErrorOr.FailIfValidationTests.cs` | FailIf with validation errors     |
| `ErrorOr.ToErrorOrTests.cs`        | ToErrorOr() helpers               |

### Or Extensions Tests (`ErrorOr/`)

| Test File                      | Coverage                                                                                            |
|--------------------------------|-----------------------------------------------------------------------------------------------------|
| `ErrorOr.OrExtensionsTests.cs` | OrNotFound, OrValidation, OrUnauthorized, OrForbidden, OrConflict, OrFailure, OrUnexpected, OrError |

### Error Tests (`Errors/`)

| Test File                    | Coverage                          |
|------------------------------|-----------------------------------|
| `ErrorTests.cs`              | Error factory methods, properties |
| `Error.EqualityTests.cs`     | Error struct equality             |
| `Error.NullMetadataTests.cs` | Null metadata handling            |

### Results Tests (`Results/`)

| Test File              | Coverage                                                       |
|------------------------|----------------------------------------------------------------|
| `TypedResultsTests.cs` | Result.Success, Result.Created, Result.Updated, Result.Deleted |

### Argument Validation Tests (`ErrorOr/`)

| Test File                           | Coverage              |
|-------------------------------------|-----------------------|
| `ErrorOrArgumentValidationTests.cs` | Throw.IfNull behavior |

## Test Patterns

### Standard Test Pattern

```csharp
[Fact]
public void Method_Scenario_ExpectedBehavior()
{
    // Arrange
    var input = ...;

    // Act
    var result = input.SomeMethod();

    // Assert
    result.IsError.Should().BeFalse();
    result.Value.Should().Be(expected);
}
```

### Async Test Pattern

```csharp
[Fact]
public async Task AsyncMethod_Scenario_ExpectedBehavior()
{
    // Arrange
    var input = ...;

    // Act
    var result = await input.SomeAsyncMethod();

    // Assert
    result.IsError.Should().BeFalse();
}
```

### Error Path Testing

```csharp
[Fact]
public void Method_WithError_PropagatesError()
{
    // Arrange
    ErrorOr<int> errorResult = Error.Validation("Test.Error", "Test error");

    // Act
    var result = errorResult.Then(x => x * 2);

    // Assert
    result.IsError.Should().BeTrue();
    result.Errors.Should().ContainSingle()
        .Which.Code.Should().Be("Test.Error");
}
```

## File Structure

```
tests/ErrorOrX.Tests/
├── ErrorOr/
│   ├── ErrorOr.ElseTests.cs
│   ├── ErrorOr.ElseAsyncTests.cs
│   ├── ErrorOr.EqualityTests.cs
│   ├── ErrorOr.FailIfTests.cs
│   ├── ErrorOr.FailIfAsyncTests.cs
│   ├── ErrorOr.FailIfValidationTests.cs
│   ├── ErrorOr.HashCodeTests.cs
│   ├── ErrorOr.ImmutabilityTests.cs
│   ├── ErrorOr.InstantiationTests.cs
│   ├── ErrorOr.InterfaceTests.cs
│   ├── ErrorOr.MatchTests.cs
│   ├── ErrorOr.MatchAsyncTests.cs
│   ├── ErrorOr.OrExtensionsTests.cs
│   ├── ErrorOr.SwitchTests.cs
│   ├── ErrorOr.SwitchAsyncTests.cs
│   ├── ErrorOr.ThenTests.cs
│   ├── ErrorOr.ThenAsyncTests.cs
│   ├── ErrorOr.ToErrorOrTests.cs
│   ├── ErrorOrArgumentValidationTests.cs
│   ├── ErrorOrTests.cs
│   └── TestUtils.cs
├── Errors/
│   ├── Error.EqualityTests.cs
│   ├── Error.NullMetadataTests.cs
│   └── ErrorTests.cs
├── Results/
│   └── TypedResultsTests.cs
├── TestUtils/
│   ├── SerializableError.cs
│   └── Unreachable.cs
└── Usings.cs
```
