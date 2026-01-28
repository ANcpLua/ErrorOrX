# Feature: ParameterFlags Simplification

## Motivation

`ParameterMeta` hat 20+ Boolean-Felder. Unübersichtlich, fehleranfällig, speicherintensiv.

## Lösung

Gruppierung in Bitflags und Enum für mutually-exclusive Types:

```csharp
[Flags]
internal enum ParameterFlags
{
    None = 0,
    FromServices = 1 << 0,
    FromKeyedServices = 1 << 1,
    FromBody = 1 << 2,
    FromRoute = 1 << 3,
    FromQuery = 1 << 4,
    FromHeader = 1 << 5,
    FromForm = 1 << 6,
    AsParameters = 1 << 7,
    Nullable = 1 << 8,
    NonNullableValueType = 1 << 9,
    Collection = 1 << 10,
    RequiresValidation = 1 << 11,
}

internal enum SpecialParameterKind
{
    None,
    HttpContext,
    CancellationToken,
    FormFile,
    FormFileCollection,
    FormCollection,
    Stream,
    PipeReader
}
```

## Implementierung

### Vereinfachtes ParameterMeta

```csharp
internal readonly record struct ParameterMeta(
    IParameterSymbol Symbol,
    string Name,
    string TypeFqn,
    RoutePrimitiveKind? RouteKind,
    ParameterFlags Flags,
    SpecialParameterKind SpecialKind,
    string? ServiceKey,
    string BoundName,
    string? CollectionItemTypeFqn,
    RoutePrimitiveKind? CollectionItemPrimitiveKind,
    CustomBindingMethod CustomBinding)
{
    public bool HasFromBody => Flags.HasFlag(ParameterFlags.FromBody);
    public bool HasFromRoute => Flags.HasFlag(ParameterFlags.FromRoute);
    public bool HasFromQuery => Flags.HasFlag(ParameterFlags.FromQuery);
    public bool HasFromHeader => Flags.HasFlag(ParameterFlags.FromHeader);
    public bool HasFromForm => Flags.HasFlag(ParameterFlags.FromForm);
    public bool HasFromServices => Flags.HasFlag(ParameterFlags.FromServices);
    public bool HasFromKeyedServices => Flags.HasFlag(ParameterFlags.FromKeyedServices);
    public bool HasAsParameters => Flags.HasFlag(ParameterFlags.AsParameters);
    public bool IsNullable => Flags.HasFlag(ParameterFlags.Nullable);
    public bool IsCollection => Flags.HasFlag(ParameterFlags.Collection);
    public bool RequiresValidation => Flags.HasFlag(ParameterFlags.RequiresValidation);

    public bool IsHttpContext => SpecialKind == SpecialParameterKind.HttpContext;
    public bool IsCancellationToken => SpecialKind == SpecialParameterKind.CancellationToken;
    public bool IsFormFile => SpecialKind == SpecialParameterKind.FormFile;
    public bool IsFormFileCollection => SpecialKind == SpecialParameterKind.FormFileCollection;
    public bool IsFormCollection => SpecialKind == SpecialParameterKind.FormCollection;
    public bool IsStream => SpecialKind == SpecialParameterKind.Stream;
    public bool IsPipeReader => SpecialKind == SpecialParameterKind.PipeReader;

    public bool HasExplicitBinding => (Flags & (
        ParameterFlags.FromBody | ParameterFlags.FromRoute | ParameterFlags.FromQuery |
        ParameterFlags.FromHeader | ParameterFlags.FromForm | ParameterFlags.FromServices |
        ParameterFlags.FromKeyedServices | ParameterFlags.AsParameters)) != 0;
}
```

### Flag-Builder

```csharp
private static ParameterFlags BuildFlags(IParameterSymbol parameter, ITypeSymbol type, ErrorOrContext context)
{
    var flags = ParameterFlags.None;

    if (HasAttribute(parameter, context.FromBody)) flags |= ParameterFlags.FromBody;
    if (HasAttribute(parameter, context.FromRoute)) flags |= ParameterFlags.FromRoute;
    if (HasAttribute(parameter, context.FromQuery)) flags |= ParameterFlags.FromQuery;
    if (HasAttribute(parameter, context.FromHeader)) flags |= ParameterFlags.FromHeader;
    if (HasAttribute(parameter, context.FromForm)) flags |= ParameterFlags.FromForm;
    if (HasAttribute(parameter, context.FromServices)) flags |= ParameterFlags.FromServices;
    if (HasAttribute(parameter, context.FromKeyedServices)) flags |= ParameterFlags.FromKeyedServices;
    if (HasAttribute(parameter, context.AsParameters)) flags |= ParameterFlags.AsParameters;

    if (IsNullable(type, parameter.NullableAnnotation)) flags |= ParameterFlags.Nullable;
    if (IsCollection(type, context)) flags |= ParameterFlags.Collection;
    if (context.RequiresValidation(type)) flags |= ParameterFlags.RequiresValidation;

    return flags;
}

private static SpecialParameterKind DetectSpecialKind(ITypeSymbol type, ErrorOrContext context)
{
    if (context.IsHttpContext(type)) return SpecialParameterKind.HttpContext;
    if (context.IsCancellationToken(type)) return SpecialParameterKind.CancellationToken;
    if (context.IsFormFile(type)) return SpecialParameterKind.FormFile;
    if (context.IsFormFileCollection(type)) return SpecialParameterKind.FormFileCollection;
    if (context.IsFormCollection(type)) return SpecialParameterKind.FormCollection;
    if (context.IsStream(type)) return SpecialParameterKind.Stream;
    if (context.IsPipeReader(type)) return SpecialParameterKind.PipeReader;
    return SpecialParameterKind.None;
}
```

## Unified BoundName

Die separaten Name-Felder werden zu einem `BoundName` zusammengefasst:

```csharp
// Alt: 4 separate Felder
string RouteName, QueryName, HeaderName, FormName

// Neu: 1 unified Feld
string BoundName  // Kontext ergibt sich aus Flags
```

## Inspiration

- `System.Reflection.BindingFlags`
- `System.IO.FileAttributes`

## Datei-Änderungen

| Datei | Änderung |
|-------|----------|
| `EndpointModels.cs` | `ParameterFlags` und `SpecialParameterKind` hinzufügen |
| `EndpointModels.cs` | `ParameterMeta` Record vereinfachen |
| `ParameterBinding.cs` | `CreateParameterMeta` auf Flag-Builder umstellen |
