# Feature: Builder Interface Pattern

## Motivation

Das ursprüngliche API verwendete einen Action-Callback für die Konfiguration:

```csharp
// Alt (Callback-basiert)
builder.Services.AddErrorOrEndpoints(options => options
    .UseJsonContext<AppJsonSerializerContext>()
    .WithCamelCase());
```

Dieses Pattern hat mehrere Nachteile:
1. **Verschachtelte Lambdas** - Bei komplexerer Konfiguration wird der Code schwer lesbar
2. **Keine Erweiterbarkeit** - Externe Libraries können keine Extension Methods hinzufügen
3. **Inkonsistent mit ASP.NET Core** - Die meisten ASP.NET Core APIs verwenden Builder-Pattern

## Lösung

Wir übernehmen das ASP.NET Core Builder-Pattern (wie `IRazorComponentsBuilder`):

```csharp
// Neu (Builder-basiert)
builder.Services.AddErrorOrEndpoints()
    .UseJsonContext<AppJsonSerializerContext>()
    .WithCamelCase()
    .WithIgnoreNulls();
```

## Implementierung

### Interface Definition

```csharp
public interface IErrorOrEndpointsBuilder
{
    IServiceCollection Services { get; }
}
```

### Extension Methods

Alle Konfigurationsmethoden sind Extension Methods auf dem Interface:

```csharp
public static class ErrorOrEndpointsBuilderExtensions
{
    public static IErrorOrEndpointsBuilder UseJsonContext<TContext>(this IErrorOrEndpointsBuilder builder)
        where TContext : JsonSerializerContext, new()
    {
        builder.Services.ConfigureHttpJsonOptions(options => ...);
        return builder;
    }
}
```

## Vorteile

| Aspekt | Callback-Pattern | Builder-Pattern |
|--------|------------------|-----------------|
| Erweiterbarkeit | Nur interne Methoden | Externe Extension Methods möglich |
| Lesbarkeit | Verschachtelte Lambdas | Flache Methodenkette |
| ASP.NET Konsistenz | Abweichend | Identisch mit AddRazorComponents etc. |
| Testbarkeit | Mock des Options-Objekts | Mock des Interfaces |

## Inspiration

- `Microsoft.AspNetCore.Components.Endpoints/IRazorComponentsBuilder`
- `Microsoft.Extensions.DependencyInjection` Patterns

## Breaking Change

Diese Änderung ist breaking:
- `AddErrorOrEndpoints(Action<ErrorOrEndpointOptions>)` existiert nicht mehr
- Rückgabetyp ist jetzt `IErrorOrEndpointsBuilder` statt `IServiceCollection`

Da keine Benutzer die Library verwenden, ist dies akzeptabel.
