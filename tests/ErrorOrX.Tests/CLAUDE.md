# ErrorOrX.Tests

Unit tests for the ErrorOrX runtime library. Target: `net10.0`.

## Running Tests

```bash
dotnet test --project tests/ErrorOrX.Tests
dotnet test --project tests/ErrorOrX.Tests --filter-class "*ThenTests*"
dotnet test --project tests/ErrorOrX.Tests --filter-method "*Should_Return_Value*"
```

## Test Categories

### ErrorOr Core

| Test File                       | Coverage                           |
|---------------------------------|------------------------------------|
| `ErrorOr.InstantiationTests.cs` | Construction, implicit conversions |
| `ErrorOr.EqualityTests.cs`      | IEquatable, operators              |
| `ErrorOr.HashCodeTests.cs`      | GetHashCode consistency            |
| `ErrorOr.ImmutabilityTests.cs`  | Immutability guarantees            |
| `ErrorOr.InterfaceTests.cs`     | IErrorOr non-generic interface     |

### Fluent API

| Test File                      | Coverage                       |
|--------------------------------|--------------------------------|
| `ErrorOr.ThenTests.cs`         | Then chaining (sync)           |
| `ErrorOr.ThenAsyncTests.cs`    | ThenAsync variants             |
| `ErrorOr.ElseTests.cs`         | Else fallback (sync)           |
| `ErrorOr.ElseAsyncTests.cs`    | ElseAsync variants             |
| `ErrorOr.MatchTests.cs`        | Match transformation           |
| `ErrorOr.SwitchTests.cs`       | Switch side effects            |
| `ErrorOr.FailIfTests.cs`       | FailIf conditional failure     |
| `ErrorOr.OrExtensionsTests.cs` | OrNotFound, OrValidation, etc. |

### Error Types

| Test File                | Coverage                               |
|--------------------------|----------------------------------------|
| `ErrorTests.cs`          | Error factory methods                  |
| `Error.EqualityTests.cs` | Error struct equality                  |
| `TypedResultsTests.cs`   | Result.Success/Created/Updated/Deleted |

## Test Pattern

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

## Dependencies

| Package           | Purpose           |
|-------------------|-------------------|
| xunit.v3.mtp-v2   | xUnit v3 with MTP |
| AwesomeAssertions | Fluent assertions |
