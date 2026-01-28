# Feature: Type-Safe ParameterSource Class

## Motivation

`EndpointParameterSource` als enum erfordert Switch-Statements überall im Code. Das ASP.NET Core `BindingSource` Pattern ist eleganter: Verhalten am Typ statt externe Logik.

## Lösung

Type-Safe Class mit Properties statt enum:

```csharp
internal sealed class ParameterSource : IEquatable<ParameterSource>
{
    public static readonly ParameterSource Route = new("Route", isFromRequest: true);
    public static readonly ParameterSource Body = new("Body", isFromRequest: true, requiresJsonContext: true);
    public static readonly ParameterSource Query = new("Query", isFromRequest: true);
    public static readonly ParameterSource Service = new("Service", isFromRequest: false);
    // ...

    public string Id { get; }
    public bool IsFromRequest { get; }
    public bool RequiresJsonContext { get; }
    public bool IsSpecialType { get; }
    public bool IsComposite { get; }
}
```

## Implementierung

```csharp
internal sealed class ParameterSource : IEquatable<ParameterSource>
{
    // Request-based sources
    public static readonly ParameterSource Route = new("Route",
        isFromRequest: true, requiresJsonContext: false, isSpecialType: false);
    public static readonly ParameterSource Body = new("Body",
        isFromRequest: true, requiresJsonContext: true, isSpecialType: false);
    public static readonly ParameterSource Query = new("Query",
        isFromRequest: true, requiresJsonContext: false, isSpecialType: false);
    public static readonly ParameterSource Header = new("Header",
        isFromRequest: true, requiresJsonContext: false, isSpecialType: false);
    public static readonly ParameterSource Form = new("Form",
        isFromRequest: true, requiresJsonContext: false, isSpecialType: false);
    public static readonly ParameterSource FormFile = new("FormFile",
        isFromRequest: true, requiresJsonContext: false, isSpecialType: false);
    public static readonly ParameterSource FormFiles = new("FormFiles",
        isFromRequest: true, requiresJsonContext: false, isSpecialType: false);
    public static readonly ParameterSource FormCollection = new("FormCollection",
        isFromRequest: true, requiresJsonContext: false, isSpecialType: false);
    public static readonly ParameterSource Stream = new("Stream",
        isFromRequest: true, requiresJsonContext: false, isSpecialType: false);
    public static readonly ParameterSource PipeReader = new("PipeReader",
        isFromRequest: true, requiresJsonContext: false, isSpecialType: false);

    // DI-based sources
    public static readonly ParameterSource Service = new("Service",
        isFromRequest: false, requiresJsonContext: false, isSpecialType: false);
    public static readonly ParameterSource KeyedService = new("KeyedService",
        isFromRequest: false, requiresJsonContext: false, isSpecialType: false);

    // Special types
    public static readonly ParameterSource HttpContext = new("HttpContext",
        isFromRequest: false, requiresJsonContext: false, isSpecialType: true);
    public static readonly ParameterSource CancellationToken = new("CancellationToken",
        isFromRequest: false, requiresJsonContext: false, isSpecialType: true);

    // Composite binding
    public static readonly ParameterSource AsParameters = new("AsParameters",
        isFromRequest: true, requiresJsonContext: false, isSpecialType: false, isComposite: true);

    public string Id { get; }
    public bool IsFromRequest { get; }
    public bool RequiresJsonContext { get; }
    public bool IsSpecialType { get; }
    public bool IsComposite { get; }

    private ParameterSource(string id, bool isFromRequest, bool requiresJsonContext,
        bool isSpecialType, bool isComposite = false)
    {
        Id = id;
        IsFromRequest = isFromRequest;
        RequiresJsonContext = requiresJsonContext;
        IsSpecialType = isSpecialType;
        IsComposite = isComposite;
    }

    public bool Equals(ParameterSource? other) => other?.Id == Id;
    public override bool Equals(object? obj) => Equals(obj as ParameterSource);
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(ParameterSource? left, ParameterSource? right)
        => left?.Equals(right) ?? right is null;
    public static bool operator !=(ParameterSource? left, ParameterSource? right)
        => !(left == right);
}
```

## Verwendung

```csharp
// Statt Switch-Statement
var needsJson = param.Source.RequiresJsonContext;

// OpenAPI: Services ausblenden
if (!param.Source.IsFromRequest) continue;

// Spezielle Typen erkennen
if (param.Source.IsSpecialType) { /* HttpContext, CancellationToken */ }
```

## Inspiration

- `Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource`

## Datei-Änderungen

| Datei | Änderung |
|-------|----------|
| `EndpointModels.cs` | `EndpointParameterSource` enum entfernen |
| `ParameterSource.cs` | Neue Datei mit Type-Safe Class |
| `ParameterBinding.cs` | Switch → Property-Abfragen |
| `Emitter.cs` | Switch → Property-Abfragen |
