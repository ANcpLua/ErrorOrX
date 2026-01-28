# Feature: EmptyBodyBehavior Support

## Motivation

ASP.NET Core behandelt leere Request Bodies basierend auf Nullability. Manchmal möchte man explizite Kontrolle:

- Nullable Parameter aber Body required
- Non-nullable aber Body optional (Default-Wert)

## Lösung

### EmptyBodyBehavior Enum

```csharp
public enum EmptyBodyBehavior
{
    /// <summary>Framework default: Nullable allows empty, non-nullable rejects.</summary>
    Default,

    /// <summary>Empty bodies are valid (null/default assigned).</summary>
    Allow,

    /// <summary>Empty bodies are invalid (400 Bad Request).</summary>
    Disallow
}
```

### AllowEmptyBody Attribut (Public API)

```csharp
namespace ErrorOr;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class AllowEmptyBodyAttribute : Attribute { }
```

### Verwendung

```csharp
[Post("/items")]
ErrorOr<Item> Create([FromBody][AllowEmptyBody] CreateItemRequest request) => ...
```

## Implementierung

### EndpointParameter Erweiterung

```csharp
internal readonly record struct EndpointParameter(
    // ...existing...
    EmptyBodyBehavior EmptyBodyBehavior = EmptyBodyBehavior.Default);
```

### Attribut-Erkennung

```csharp
private static EmptyBodyBehavior DetectEmptyBodyBehavior(IParameterSymbol parameter, ErrorOrContext context)
{
    foreach (var attr in parameter.GetAttributes())
    {
        var name = attr.AttributeClass?.Name;
        if (name is "AllowEmptyBodyAttribute" or "AllowEmptyBody")
            return EmptyBodyBehavior.Allow;
    }
    return EmptyBodyBehavior.Default;
}
```

### Code-Generierung

```csharp
private static string EmitBodyBinding(EndpointParameter param)
{
    return param.EmptyBodyBehavior switch
    {
        EmptyBodyBehavior.Allow => $$"""
            var {{param.Name}} = ctx.Request.ContentLength > 0
                ? await ctx.Request.ReadFromJsonAsync<{{param.TypeFqn}}>(ctx.RequestAborted)
                : default;
            """,

        EmptyBodyBehavior.Disallow => $$"""
            if (ctx.Request.ContentLength is null or 0)
                return TypedResults.BadRequest<ProblemDetails>(new ProblemDetails
                {
                    Title = "Request body is required",
                    Status = 400
                });
            var {{param.Name}} = await ctx.Request.ReadFromJsonAsync<{{param.TypeFqn}}>(ctx.RequestAborted)!;
            """,

        _ => param.IsNullable
            ? EmitBodyBinding(param with { EmptyBodyBehavior = EmptyBodyBehavior.Allow })
            : EmitBodyBinding(param with { EmptyBodyBehavior = EmptyBodyBehavior.Disallow })
    };
}
```

## Inspiration

- `Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior`

## Datei-Änderungen

| Datei | Änderung |
|-------|----------|
| `EmptyBodyBehavior.cs` | Neues public Enum |
| `AllowEmptyBodyAttribute.cs` | Neues public Attribut |
| `EndpointModels.cs` | `EndpointParameter` erweitern |
| `ParameterBinding.cs` | `DetectEmptyBodyBehavior` |
| `Emitter.cs` | Body-Binding-Logik |
