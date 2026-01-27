# Feature: Convention Builder Return

## Motivation

Das ursprüngliche `MapErrorOrEndpoints()` gab `void` zurück:

```csharp
// Alt
app.MapErrorOrEndpoints();

// Globale Konfiguration war nicht möglich:
// app.MapErrorOrEndpoints().RequireAuthorization(); // Kompilierfehler!
```

Benutzer konnten keine globalen Konventionen auf alle ErrorOr-Endpoints anwenden.

## Lösung

`MapErrorOrEndpoints()` gibt jetzt `IEndpointConventionBuilder` zurück:

```csharp
// Neu
app.MapErrorOrEndpoints()
    .RequireAuthorization()           // Alle Endpoints erfordern Auth
    .RequireRateLimiting("api")       // Rate Limiting für alle
    .WithGroupName("v1");             // OpenAPI Gruppierung
```

## Implementierung

### CompositeEndpointConventionBuilder

Da wir mehrere Endpoints registrieren, brauchen wir einen Composite-Builder der Konventionen an alle Endpoints weiterleitet:

```csharp
internal sealed class CompositeEndpointConventionBuilder : IEndpointConventionBuilder
{
    private readonly List<IEndpointConventionBuilder> _builders;

    public CompositeEndpointConventionBuilder(List<IEndpointConventionBuilder> builders)
    {
        _builders = builders;
    }

    public void Add(Action<EndpointBuilder> convention)
    {
        foreach (var builder in _builders)
        {
            builder.Add(convention);
        }
    }

    public void Finally(Action<EndpointBuilder> finallyConvention)
    {
        foreach (var builder in _builders)
        {
            builder.Finally(finallyConvention);
        }
    }
}
```

### Endpoint Sammlung

Jeder registrierte Endpoint wird zur Builder-Liste hinzugefügt:

```csharp
public static IEndpointConventionBuilder MapErrorOrEndpoints(this IEndpointRouteBuilder app)
{
    var __endpointBuilders = new List<IEndpointConventionBuilder>();

    // Für jeden Endpoint:
    var __ep0 = app.MapGet("/todos", Handler);
    __endpointBuilders.Add(__ep0);

    // Am Ende:
    return new CompositeEndpointConventionBuilder(__endpointBuilders);
}
```

## Anwendungsfälle

### Globale Authentifizierung

```csharp
app.MapErrorOrEndpoints()
    .RequireAuthorization();
```

### Rate Limiting

```csharp
app.MapErrorOrEndpoints()
    .RequireRateLimiting("api-policy");
```

### CORS

```csharp
app.MapErrorOrEndpoints()
    .RequireCors("AllowAll");
```

### OpenAPI Gruppierung

```csharp
app.MapErrorOrEndpoints()
    .WithGroupName("v1")
    .WithDescription("Todo API v1");
```

### Kombiniert

```csharp
app.MapErrorOrEndpoints()
    .RequireAuthorization("AdminPolicy")
    .RequireRateLimiting("strict")
    .RequireCors("Production")
    .WithGroupName("admin-api");
```

## Vorteile

| Aspekt | void Return | IEndpointConventionBuilder |
|--------|-------------|---------------------------|
| Globale Auth | Pro Endpoint oder Middleware | Fluent am Mapping |
| Rate Limiting | Pro Endpoint | Fluent am Mapping |
| Code-Duplizierung | Middleware oder Attribute | Einmal am Mapping |
| ASP.NET Konsistenz | Abweichend | Identisch mit MapRazorComponents etc. |

## Inspiration

- `Microsoft.AspNetCore.Builder.RazorComponentsEndpointConventionBuilder`
- `Microsoft.AspNetCore.Routing.RouteHandlerBuilder`

## Breaking Change

Diese Änderung ist breaking:
- Rückgabetyp von `void` zu `IEndpointConventionBuilder`

Da keine Benutzer die Library verwenden und `void` einfach ignoriert werden kann, ist der Impact minimal.
