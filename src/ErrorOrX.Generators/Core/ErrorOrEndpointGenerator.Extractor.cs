using Microsoft.CodeAnalysis;

namespace ErrorOr.Generators;

/// <summary>
///     Partial class containing return-type extraction logic.
///     Determines the success type, async-ness, SSE shape, and inaccessibility / open-generic
///     diagnostics for a handler method's <c>ErrorOr&lt;T&gt;</c> return type.
///     Sibling extractor partials:
///     <list type="bullet">
///         <item><c>Extractor.ErrorInference.cs</c> — body-walking to collect <c>Error.X()</c> calls.</item>
///         <item><c>Extractor.InterfaceDetection.cs</c> — undocumented interface-call detection.</item>
///         <item><c>Extractor.Metadata.cs</c> — endpoint metadata (route, attributes, middleware).</item>
///     </list>
/// </summary>
public sealed partial class ErrorOrEndpointGenerator
{
    /// <summary>
    ///     Extracts the ErrorOr return type information from a method's return type.
    ///     Returns null SuccessTypeFqn for invalid types (anonymous, inaccessible).
    /// </summary>
    private static ErrorOrReturnTypeInfo ExtractErrorOrReturnType(ITypeSymbol returnType)
    {
        var resultType = returnType.GetTaskResultType();
        var unwrapped = resultType ?? returnType;
        var isAsync = resultType is not null;

        if (!IsErrorOrType(unwrapped, out var errorOrType))
            return new ErrorOrReturnTypeInfo(null, false, false, null, SuccessKind.Payload);

        var innerType = errorOrType.TypeArguments[0];

        // EOE015: ErrorOr<object> / ErrorOr<dynamic> — flag for warning, but keep a valid FQN
        // so generation proceeds (the user can ship reflection-based JSON with a registered
        // typeof(object) in their JsonSerializerContext). C# does not permit anonymous types in
        // generic type arguments, so the original IsAnonymousType check was unreachable; users
        // actually hit this by upcasting an anonymous expression: `ErrorOr<object> Get() => new { ... }`.
        // `dynamic` surfaces as SpecialType.System_Object too, so this catches both shapes.
        var isObjectReturn = innerType.SpecialType is SpecialType.System_Object;

        // EOE018: Private/protected types cannot be accessed by generated code
        if (innerType.DeclaredAccessibility is Accessibility.Private or Accessibility.Protected)
        {
            return new ErrorOrReturnTypeInfo(null, false, false, null, SuccessKind.Payload, null, false, true,
                innerType.ToDisplayString(), innerType.DeclaredAccessibility.ToString().ToLowerInvariant());
        }

        switch (innerType)
        {
            // EOE019: Type parameters (open generics) cannot be used
            case ITypeParameterSymbol typeParam:
                return new ErrorOrReturnTypeInfo(null, false, false, null, SuccessKind.Payload, null, false, false,
                    null, null, true, typeParam.Name);
            // Also check if the inner type contains type parameters (e.g., List<T>)
            case INamedTypeSymbol namedInner when
                namedInner.TypeArguments.Any(static t => t is ITypeParameterSymbol):
            {
                var firstTypeParam = namedInner.TypeArguments.First(static t => t is ITypeParameterSymbol);
                return new ErrorOrReturnTypeInfo(null, false, false, null, SuccessKind.Payload, null, false, false,
                    null, null, true, firstTypeParam.Name);
            }
        }

        var kind = SuccessKind.Payload;

        if (ErrorOrContext.MatchesType(innerType, WellKnownTypes.Success))
            kind = SuccessKind.Success;
        else if (ErrorOrContext.MatchesType(innerType, WellKnownTypes.CreatedMarker))
            kind = SuccessKind.Created;
        else if (ErrorOrContext.MatchesType(innerType, WellKnownTypes.Updated))
            kind = SuccessKind.Updated;
        else if (ErrorOrContext.MatchesType(innerType, WellKnownTypes.Deleted))
            kind = SuccessKind.Deleted;

        if (TryUnwrapAsyncEnumerable(innerType, out var elementType))
        {
            if (TryUnwrapSseItem(elementType, out var sseDataType))
            {
                var sseDataFqn = sseDataType.GetFullyQualifiedName();
                var asyncEnumFqn = innerType.GetFullyQualifiedName();
                return new ErrorOrReturnTypeInfo(asyncEnumFqn, isAsync, true, sseDataFqn, kind);
            }
            else
            {
                var elementFqn = elementType.GetFullyQualifiedName();
                var asyncEnumFqn = innerType.GetFullyQualifiedName();
                return new ErrorOrReturnTypeInfo(asyncEnumFqn, isAsync, true, elementFqn, kind);
            }
        }

        var successTypeFqn = innerType.GetFullyQualifiedName();
        var idPropertyName = DetectIdProperty(innerType);
        return new ErrorOrReturnTypeInfo(successTypeFqn, isAsync, false, null, kind, idPropertyName, isObjectReturn);
    }

    /// <summary>
    ///     Detects a suitable Id property on the success type for Location header generation.
    ///     Looks for properties named Id, ID, id (case-insensitive), preferring exact "Id" match.
    ///     Searches through base types to find inherited Id properties.
    /// </summary>
    private static string? DetectIdProperty(ITypeSymbol type)
    {
        // Skip marker types and primitives
        if (type.SpecialType != SpecialType.None) return null;

        string? bestMatch = null;

        for (var current = type as INamedTypeSymbol; current is not null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                // Pattern-as-spec: public readable property
                if (member is not IPropertySymbol
                    {
                        DeclaredAccessibility: Accessibility.Public, GetMethod: not null
                    } property)
                {
                    continue;
                }

                // Exact match "Id" is preferred - return immediately
                if (property.Name == "Id") return "Id";

                // Case-insensitive match for fallback
                if (string.Equals(property.Name, "Id", StringComparison.OrdinalIgnoreCase)) bestMatch ??= property.Name;
            }
        }

        return bestMatch;
    }

    private static bool TryUnwrapAsyncEnumerable(
        ITypeSymbol type,
        [NotNullWhen(true)] out ITypeSymbol? elementType)
    {
        if (type is INamedTypeSymbol { IsGenericType: true } named &&
            ErrorOrContext.MatchesConstructedFrom(named.ConstructedFrom, WellKnownTypes.IAsyncEnumerableT))
        {
            elementType = named.TypeArguments[0];
            return true;
        }

        elementType = null;
        return false;
    }

    private static bool TryUnwrapSseItem(
        ITypeSymbol type,
        [NotNullWhen(true)] out ITypeSymbol? dataType)
    {
        if (type is INamedTypeSymbol { IsGenericType: true } named &&
            ErrorOrContext.MatchesConstructedFrom(named.ConstructedFrom, WellKnownTypes.SseItemT))
        {
            dataType = named.TypeArguments[0];
            return true;
        }

        dataType = null;
        return false;
    }

    private static bool IsErrorOrType(
        ITypeSymbol type,
        [NotNullWhen(true)] out INamedTypeSymbol? errorOrType)
    {
        if (type is INamedTypeSymbol { IsGenericType: true } named &&
            ErrorOrContext.MatchesConstructedFrom(named.ConstructedFrom, WellKnownTypes.ErrorOrT))
        {
            errorOrType = named;
            return true;
        }

        errorOrType = null;
        return false;
    }

    private static EquatableArray<ProducesErrorInfo> ExtractProducesErrorAttributes(
        ISymbol method)
    {
        var results = new List<ProducesErrorInfo>();

        foreach (var attr in method.GetAttributes())
        {
            if (ErrorOrContext.MatchesType(attr.AttributeClass, WellKnownTypes.ProducesErrorAttribute) &&
                attr.ConstructorArguments is [{ Value: int statusCode }, ..])
            {
                results.Add(new ProducesErrorInfo(statusCode));
            }
        }

        return results.Count > 0
            ? new EquatableArray<ProducesErrorInfo>([.. results])
            : default;
    }

    /// <summary>
    ///     Checks if the method has the [AcceptedResponse] attribute for 202 Accepted responses.
    /// </summary>
    private static bool HasAcceptedResponseAttribute(ISymbol method)
    {
        return ErrorOrContext.HasAttribute(method, WellKnownTypes.AcceptedResponseAttribute);
    }
}
