# Cross-assembly validation fails when DTOs are in a different assembly than endpoint mapping

## Summary

The `ValidationsGenerator` cannot discover validatable types when:
1. DTOs live in a referenced assembly (not the host)
2. Endpoint mapping uses generic extension methods (e.g., `app.MapCommand<TRequest>()`)

This forces users into three compounding workarounds that shouldn't be necessary.

## Reproduction

**Assembly A** — DTOs:
```csharp
// MyApp.Models/CreateItemRequest.cs
public sealed record CreateItemRequest(
    [Required] [StringLength(200, MinimumLength = 1)] string Name,
    [Required] [EmailAddress] string Email);
```

**Assembly B** — Endpoints:
```csharp
// MyApp.Api/ItemApi.cs
public static class ItemApi
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/items", (CreateItemRequest request) => Results.Ok());
    }
}
```

**Host**:
```csharp
builder.Services.AddValidation();
app.MapItemEndpoints();
```

**Result**: Validation does not run. Invalid requests return 200 OK.

### Making it work requires all three workarounds simultaneously

**Workaround 1** — Fake trigger method in the DTO assembly (forces generator to run):
```csharp
// MyApp.Models/ValidationCodegenTrigger.cs
internal static class ValidationCodegenTrigger
{
    // Never called. Exists solely to make the source generator emit a resolver for this assembly.
    public static IServiceCollection Trigger(this IServiceCollection services)
        => services.AddValidation();
}
```

**Workaround 2** — `[ValidatableType]` on every DTO (forces type discovery):
```csharp
[ValidatableType]
public sealed record CreateItemRequest(...);
```

**Workaround 3** — Reflection-based resolver aggregation in the host:
```csharp
#pragma warning disable ASP0029
builder.Services.AddValidation(options =>
{
    foreach (var resolver in assembly.GetTypes()
        .Where(t => typeof(IValidatableInfoResolver).IsAssignableFrom(t)
                 && !t.IsAbstract
                 && t.GetConstructor(Type.EmptyTypes) != null)
        .Select(t => (IValidatableInfoResolver)Activator.CreateInstance(t)!))
    {
        options.Resolvers.Add(resolver);
    }
});
#pragma warning restore ASP0029
```

All three are required. Missing any one means validation silently does nothing.

## Root Cause

Two independent failures in the generator pipeline:

### 1. Type discovery only sees the current assembly's handler signatures

`TypesParser.cs` resolves types from the handler delegate's formal parameters. When the delegate is in a different assembly, or when the mapping goes through a generic extension method like `MapCommand<TRequest>()`, the generator sees `ITypeParameterSymbol` (not the concrete type) and skips it because type parameters have `NotApplicable` accessibility:

```csharp
// TypesParser.cs:78-81
if (typeSymbol.DeclaredAccessibility is not Accessibility.Public)
{
    return false;  // Type parameters hit this path — silently skipped
}
```

### 2. Generator activation requires `AddValidation()` in each assembly

The `ValidationsGenerator` only emits an `IValidatableInfoResolver` for assemblies that contain an `AddValidation()` invocation. Feature assemblies that only define DTOs and endpoints never trigger generation — there is no resolver to discover, even with reflection.

## Evidence this is solvable

The [ErrorOrX](https://github.com/AncpLua/ErrorOrX) source generator handles this exact scenario without any workarounds. Its architecture:

1. **Discovers types from user method signatures**, not from endpoint delegate resolution. When a user writes `Create(CreateOrderRequest request, ...)`, the generator sees `CreateOrderRequest` as a concrete type — regardless of which assembly it lives in.

2. **Emits a single resolver** per compilation. No duplicate hint names, no per-assembly activation requirement.

3. **Cross-assembly just works**:
```
MyApp.Models/                    ← DTOs with [Required], [Range], etc.
  CreateOrderRequest.cs

MyApp.Api/                       ← Endpoints (different assembly)
  OrderApi.cs                    ← Create(CreateOrderRequest request, ...)
  Program.cs                     ← AddValidation() + MapErrorOrEndpoints()
```

No fake trigger methods. No `[ValidatableType]`. No reflection scanning.

## Proposed fix

### For type discovery (TypesParser.cs)

Resolve generic type arguments at invocation sites before the accessibility check:

```csharp
if (typeSymbol is ITypeParameterSymbol typeParam)
{
    var concreteType = ResolveTypeArgument(operation, typeParam);
    if (concreteType is not null)
        typeSymbol = concreteType;
    else
        return false;
}
```

### For per-assembly activation

Consider one of:
- **Option A**: Run the generator for any assembly containing types with `[ValidatableType]` or `DataAnnotations` attributes, without requiring an `AddValidation()` call
- **Option B**: Have the host's `AddValidation()` interceptor automatically discover and register resolvers from referenced assemblies

## Community impact

- [Stack Overflow: Built-in validation support for Minimal APIs](https://stackoverflow.com/q/79843261) (291 views)
- [Stack Overflow: Validation stops working when endpoint mapping is in different assembly](https://stackoverflow.com/q/79855653) (159 views)
- [#61971](https://github.com/dotnet/aspnetcore/issues/61971) — Duplicate hint name (fixed, but symptom of multi-emission)
- [#61388](https://github.com/dotnet/aspnetcore/issues/61388) — Generic type validation failures (fixed for some cases)
- [#62757](https://github.com/dotnet/aspnetcore/issues/62757) — Inaccessibility errors (fixed by skipping private types)

The closed issues fixed crashes but not the underlying discovery gap. Cross-assembly validation still requires all three workarounds.

## Environment

- .NET 10.0
- `Microsoft.Extensions.Validation` source generator
- Multi-project solution with shared DTO assemblies
