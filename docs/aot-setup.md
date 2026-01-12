# Native AOT Support

ErrorOr v2.0.0 includes automatic Native AOT support. No configuration required.

## Overview

When you publish with `PublishAot=true`, ErrorOr automatically generates an `ErrorOrJsonContext` containing all the JSON serialization metadata needed for your endpoints. This happens at compile time through source generation.

Key points:
- AOT support is automatic in v2.0.0
- No `ERROROR_JSON` compiler define needed (that was for v1.x)
- Works out of the box with minimal API endpoints

## What Gets Generated

The source generator creates JSON serialization metadata for:

- All request and response types used in your `MapErrorOr<TRequest, TResponse>` endpoints
- `ProblemDetails` and `HttpValidationProblemDetails` for error responses
- Array and List variants of all discovered types

The generated context is named `ErrorOrJsonContext` and is automatically registered when you call `AddErrorOrEndpoints()`.

## Using Your Own JsonSerializerContext

If you have custom types that need JSON serialization outside of ErrorOr endpoints, create your own context with only those types:

```csharp
[JsonSerializable(typeof(MyCustomType))]
[JsonSerializable(typeof(AnotherType))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
```

Then register both contexts:

```csharp
builder.Services.AddErrorOrEndpointJson<AppJsonSerializerContext>();
```

This chains your context with the auto-generated `ErrorOrJsonContext`, so both are available for serialization.

## Opting Out of Auto-Generation

If you need full manual control over JSON serialization, disable the auto-generated context:

```xml
<PropertyGroup>
  <ErrorOrGenerateJsonContext>false</ErrorOrGenerateJsonContext>
</PropertyGroup>
```

When disabled, you must provide your own `JsonSerializerContext` that includes all types used by your endpoints, plus `ProblemDetails` and `HttpValidationProblemDetails`.

## Publishing

Publish your application with AOT:

```bash
dotnet publish -c Release /p:PublishAot=true
```

Or set it permanently in your project file:

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
</PropertyGroup>
```

## Troubleshooting

### JSON serialization errors at runtime

If you see errors like "Metadata for type X was not provided", ensure:

1. Custom types used in HTTP responses are included in a `JsonSerializerContext`
2. You registered your context with `AddErrorOrEndpointJson<YourContext>()`
3. Types returned outside of ErrorOr endpoints have their own serialization metadata

### Trimming warnings during publish

AOT publishing may show trimming warnings for reflection-heavy code. ErrorOr endpoints are designed to be trim-safe, but third-party libraries may not be. Review warnings and suppress only after confirming they are safe.
