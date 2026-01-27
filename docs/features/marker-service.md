# Feature: Marker Service Validation

## Motivation

Ohne Validierung kann ein Benutzer `MapErrorOrEndpoints()` aufrufen ohne vorher `AddErrorOrEndpoints()` registriert zu haben. Dies führt zu kryptischen Fehlern zur Laufzeit:

```
System.InvalidOperationException: No service for type 'SomeInternalType' has been registered.
```

Der Benutzer versteht nicht sofort, dass er die Service-Registrierung vergessen hat.

## Lösung

Wir verwenden das ASP.NET Core Marker Service Pattern:

1. `AddErrorOrEndpoints()` registriert einen leeren Marker Service
2. `MapErrorOrEndpoints()` prüft ob dieser Service existiert
3. Falls nicht, wird eine klare Fehlermeldung ausgegeben

## Implementierung

### Marker Service

```csharp
internal sealed class ErrorOrEndpointsMarkerService { }
```

### Registrierung

```csharp
public static IErrorOrEndpointsBuilder AddErrorOrEndpoints(this IServiceCollection services)
{
    services.AddSingleton<ErrorOrEndpointsMarkerService>();
    return new ErrorOrEndpointsBuilder(services);
}
```

### Validierung

```csharp
public static IEndpointConventionBuilder MapErrorOrEndpoints(this IEndpointRouteBuilder app)
{
    var marker = app.ServiceProvider.GetService<ErrorOrEndpointsMarkerService>();
    if (marker is null)
    {
        throw new InvalidOperationException(
            "Unable to find the required services. " +
            "Please add all the required services by calling 'IServiceCollection.AddErrorOrEndpoints()' " +
            "in the application startup code.");
    }

    // ... endpoint registration
}
```

## Fehlermeldung

Die Fehlermeldung ist bewusst ähnlich zu ASP.NET Core formuliert:

```
Unable to find the required services. Please add all the required services
by calling 'IServiceCollection.AddErrorOrEndpoints()' in the application startup code.
```

## Vorteile

| Ohne Marker Service | Mit Marker Service |
|---------------------|-------------------|
| Kryptische DI-Fehler | Klare, actionable Fehlermeldung |
| Fehler bei erstem Request | Fehler beim App-Start |
| Debugging erforderlich | Sofortige Lösung erkennbar |

## Inspiration

- `Microsoft.AspNetCore.Components.Endpoints/RazorComponentsMarkerService`
- `Microsoft.AspNetCore.Mvc/MvcMarkerService`

## Kein Breaking Change

Diese Änderung fügt nur Validierung hinzu und bricht keine bestehende Funktionalität.
