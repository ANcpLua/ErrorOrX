# Feature: Endpoint Metadata Extensibility

## Motivation

`EndpointDescriptor` hat 19 fest definierte Felder. Für zukünftige Features oder Custom Transformers gibt es keinen Erweiterungsmechanismus.

## Lösung

### Metadata auf EndpointDescriptor

```csharp
internal readonly record struct EndpointDescriptor(
    // ...existing 18 fields...
    EquatableArray<KeyValuePair<string, string>> Metadata = default)
{
    public string? GetMetadata(string key) =>
        Metadata.AsImmutableArray().FirstOrDefault(kv => kv.Key == key).Value;

    public bool HasMetadata(string key) =>
        Metadata.AsImmutableArray().Any(kv => kv.Key == key);
}
```

### EndpointMetadata Attribut (Public API)

```csharp
namespace ErrorOr;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class EndpointMetadataAttribute : Attribute
{
    public string Key { get; }
    public string Value { get; }

    public EndpointMetadataAttribute(string key, string value)
    {
        Key = key;
        Value = value;
    }
}
```

### Verwendung

```csharp
[Get("/items/{id}")]
[EndpointMetadata("audit:action", "read")]
[EndpointMetadata("cache:ttl", "3600")]
public static ErrorOr<Item> GetById(int id) => ...
```

## Runtime Types: Error und ErrorOr Verbesserungen

### Error.NumericType entfernen

```csharp
// Alt
public readonly record struct Error
{
    public ErrorType Type { get; }
    public int NumericType { get; }  // Redundant
}

// Neu
public readonly record struct Error
{
    public ErrorType Type { get; }
    // NumericType entfernt - (int)Type wenn nötig
}
```

### Immutable Metadata auf Error

```csharp
// Alt
public Dictionary<string, object>? Metadata { get; }

// Neu
public IReadOnlyDictionary<string, object>? Metadata { get; }

// Intern: FrozenDictionary
private readonly FrozenDictionary<string, object>? _metadata;
```

### ImmutableArray für Errors in ErrorOr

```csharp
// Alt
private readonly List<Error>? _errors;

// Neu
private readonly ImmutableArray<Error> _errors;

public bool IsError => !_errors.IsDefaultOrEmpty;
public IReadOnlyList<Error> Errors => IsError
    ? _errors
    : throw new InvalidOperationException("No errors recorded.");
public IReadOnlyList<Error> ErrorsOrEmpty => IsError ? _errors : [];
```

### Konsistente IErrorOr Interface

```csharp
public interface IErrorOr
{
    IReadOnlyList<Error> Errors { get; }      // Throws if !IsError
    IReadOnlyList<Error> ErrorsOrEmpty { get; }  // Never throws
    bool IsError { get; }
    Error FirstError { get; }
}
```

## Implementierung

### Metadata Extraktion

```csharp
private static EquatableArray<KeyValuePair<string, string>> ExtractMetadata(IMethodSymbol method)
{
    var metadata = ImmutableArray.CreateBuilder<KeyValuePair<string, string>>();

    // [Obsolete] → deprecated
    var obsolete = method.GetAttributes()
        .FirstOrDefault(a => a.AttributeClass?.Name == "ObsoleteAttribute");
    if (obsolete is not null)
    {
        metadata.Add(new("erroror:deprecated", "true"));
        if (obsolete.ConstructorArguments is [{ Value: string msg }, ..])
            metadata.Add(new("erroror:deprecated-message", msg));
    }

    // [EndpointMetadata]
    foreach (var attr in method.GetAttributes())
    {
        if (attr.AttributeClass?.Name is "EndpointMetadataAttribute"
            && attr.ConstructorArguments is [{ Value: string key }, { Value: string value }])
        {
            metadata.Add(new(key, value));
        }
    }

    return new(metadata.ToImmutable());
}
```

### Verwendung im Emitter

```csharp
if (endpoint.HasMetadata("erroror:deprecated"))
{
    sb.AppendLine(".WithMetadata(new ObsoleteAttribute())");
}
```

## Metadata Keys (Konventionen)

```csharp
internal static class MetadataKeys
{
    public const string Deprecated = "erroror:deprecated";
    public const string DeprecatedMessage = "erroror:deprecated-message";
    public const string OpenApiExtension = "openapi:x-";
    public const string CustomTag = "openapi:tag";
}
```

## Inspiration

- `Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor.Properties`
- `Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescription.Properties`

## Datei-Änderungen

| Datei | Änderung |
|-------|----------|
| `EndpointModels.cs` | `Metadata` Feld und Helper-Methods |
| `EndpointMetadataAttribute.cs` | Neues public Attribut |
| `Extractor.cs` | `ExtractMetadata` Methode |
| `Emitter.cs` | Metadata in Generated Code |
| `Error.cs` | `NumericType` entfernen, `FrozenDictionary` |
| `ErrorOr.cs` | `ImmutableArray<Error>` statt `List<Error>` |
| `IErrorOr.cs` | `ErrorsOrEmpty` hinzufügen |
| `EmptyErrors.cs` | Kann entfernt werden |
