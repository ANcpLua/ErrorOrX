# ErrorOrX

Modern ErrorOr implementation for .NET 10 with fluent API and source generators.

## Packages

| Package | Description |
|---------|-------------|
| `ErrorOr.Core` | Core ErrorOr discriminated union with fluent API |
| `ErrorOr.Endpoints` | Source generator for ASP.NET Core Minimal API endpoints |
| `ErrorOr.Endpoints.CodeFixes` | Roslyn code fixes for ErrorOr.Endpoints |

## Installation

```bash
dotnet add package ErrorOr.Core
dotnet add package ErrorOr.Endpoints
```

## Quick Start

```csharp
using ErrorOr.Core;

// Return success or error
ErrorOr<User> GetUser(int id)
{
    if (id <= 0)
        return Error.Validation("User.InvalidId", "User ID must be positive");

    return new User { Id = id, Name = "John" };
}

// Handle result fluently
var result = GetUser(1)
    .Then(user => UpdateUser(user))
    .Else(errors => HandleErrors(errors));
```

## License

MIT
