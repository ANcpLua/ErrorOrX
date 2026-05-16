using ANcpLua.Roslyn.Utilities.Models;
using ErrorOr.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ErrorOr.Generators;

/// <summary>
///     Partial class containing extraction and parameter binding logic.
/// </summary>
public sealed partial class ErrorOrEndpointGenerator
{
    /// <summary>
    ///     Extracts the ErrorOr return type information from a method's return type.
    ///     Returns null SuccessTypeFqn for invalid types (anonymous, inaccessible).
    /// </summary>
    private static ErrorOrReturnTypeInfo ExtractErrorOrReturnType(ITypeSymbol returnType, ErrorOrContext context)
    {
        var resultType = returnType.GetTaskResultType();
        var unwrapped = resultType ?? returnType;
        var isAsync = resultType is not null;

        if (!IsErrorOrType(unwrapped, context, out var errorOrType))
            return new ErrorOrReturnTypeInfo(null, false, false, null, SuccessKind.Payload);

        var innerType = errorOrType.TypeArguments[0];

        // EOE015: Anonymous types cannot be serialized
        if (innerType.IsAnonymousType)
            return new ErrorOrReturnTypeInfo(null, false, false, null, SuccessKind.Payload, null, true);

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

        if (TryUnwrapAsyncEnumerable(innerType, context, out var elementType))
        {
            if (TryUnwrapSseItem(elementType, context, out var sseDataType))
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
        return new ErrorOrReturnTypeInfo(successTypeFqn, isAsync, IsSse: false, SseItemTypeFqn: null, kind, idPropertyName);
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
        ErrorOrContext context,
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
        ErrorOrContext context,
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
        ErrorOrContext context,
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

    private static (EquatableArray<string> ErrorTypeNames, EquatableArray<CustomErrorInfo> CustomErrors)
        InferErrorTypesFromMethod(
            GeneratorAttributeSyntaxContext ctx,
            ISymbol method,
            ErrorOrContext context,
            ImmutableArray<DiagnosticInfo>.Builder diagnostics,
            bool hasExplicitProducesError)
    {
        if (GetMethodBody(method) is not { } body) return (default, default);

        var methodName = method.Name;
        var (errorTypeNames, customErrors) = CollectErrorTypes(ctx.SemanticModel, body, context, diagnostics,
            methodName,
            hasExplicitProducesError);
        return (ToSortedErrorArray(errorTypeNames), new EquatableArray<CustomErrorInfo>([.. customErrors]));
    }

    private static EquatableArray<ProducesErrorInfo> ExtractProducesErrorAttributes(
        ISymbol method,
        ErrorOrContext context)
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
    private static bool HasAcceptedResponseAttribute(ISymbol method, ErrorOrContext context)
    {
        return ErrorOrContext.HasAttribute(method, WellKnownTypes.AcceptedResponseAttribute);
    }

    private static SyntaxNode? GetMethodBody(ISymbol method)
    {
        var refs = method.DeclaringSyntaxReferences;
        if (refs.IsDefaultOrEmpty || refs.Length is 0) return null;

        var syntax = refs[0].GetSyntax();
        return syntax switch
        {
            MethodDeclarationSyntax m => (SyntaxNode?)m.Body ?? m.ExpressionBody,
            LocalFunctionStatementSyntax f => (SyntaxNode?)f.Body ?? f.ExpressionBody,
            _ => null
        };
    }

    private static (HashSet<string> ErrorTypeNames, List<CustomErrorInfo> CustomErrors) CollectErrorTypes(
        SemanticModel semanticModel,
        SyntaxNode body,
        ErrorOrContext context,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        string endpointMethodName,
        bool hasExplicitProducesError)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        var customErrors = new List<CustomErrorInfo>();
        var visitedSymbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        var seenCustomCodes = new HashSet<string>(StringComparer.Ordinal);
        CollectErrorTypesRecursive(semanticModel, body, set, customErrors, visitedSymbols, seenCustomCodes,
            context, diagnostics, endpointMethodName, hasExplicitProducesError);
        return (set, customErrors);
    }

    private static void CollectErrorTypesRecursive(
        SemanticModel semanticModel,
        SyntaxNode node,
        ISet<string> errorTypeNames,
        ICollection<CustomErrorInfo> customErrors,
        ISet<ISymbol> visitedSymbols,
        ISet<string> seenCustomCodes,
        ErrorOrContext context,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        string endpointMethodName,
        bool hasExplicitProducesError)
    {
        foreach (var child in node.DescendantNodes())
        {
            ProcessNode(semanticModel, child, errorTypeNames, customErrors, visitedSymbols, seenCustomCodes, context,
                diagnostics, endpointMethodName, hasExplicitProducesError);
        }
    }

    private static void ProcessNode(
        SemanticModel semanticModel,
        SyntaxNode child,
        ISet<string> errorTypeNames,
        ICollection<CustomErrorInfo> customErrors,
        ISet<ISymbol> visitedSymbols,
        ISet<string> seenCustomCodes,
        ErrorOrContext context,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        string endpointMethodName,
        bool hasExplicitProducesError)
    {
        if (TryHandleErrorFactoryInvocation(
                semanticModel,
                child,
                errorTypeNames,
                customErrors,
                seenCustomCodes,
                context,
                diagnostics))
        {
            return;
        }

        // Check for interface/abstract method calls that return ErrorOr
        if (TryDetectUndocumentedInterfaceCall(
                semanticModel,
                child,
                context,
                endpointMethodName,
                hasExplicitProducesError,
                diagnostics,
                errorTypeNames,
                customErrors,
                seenCustomCodes))
        {
            return;
        }

        if (!TryGetReferencedSymbol(semanticModel, child, visitedSymbols, out var symbol)) return;

        foreach (var reference in symbol.DeclaringSyntaxReferences)
        {
            var bodyToScan = GetBodyToScan(reference.GetSyntax());
            if (bodyToScan is not null)
            {
                CollectErrorTypesRecursive(semanticModel, bodyToScan, errorTypeNames, customErrors,
                    visitedSymbols, seenCustomCodes, context, diagnostics, endpointMethodName,
                    hasExplicitProducesError);
            }
        }
    }

    /// <summary>
    ///     Detects calls to interface/abstract methods returning ErrorOr.
    ///     If the interface method has [ReturnsError] attributes, extract them.
    ///     If not, and endpoint has no [ProducesError], emit ERROR (FAIL LOUD).
    /// </summary>
    private static bool TryDetectUndocumentedInterfaceCall(
        SemanticModel semanticModel,
        SyntaxNode node,
        ErrorOrContext context,
        string endpointMethodName,
        bool hasExplicitProducesError,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        ISet<string> errorTypeNames,
        ICollection<CustomErrorInfo> customErrors,
        ISet<string> seenCustomCodes)
    {
        // Only check invocation expressions
        if (node is not InvocationExpressionSyntax invocation) return false;

        var symbolInfo = semanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol) return false;

        // Check if method returns ErrorOr<T>
        if (!ReturnsErrorOr(methodSymbol, context)) return false;

        // Check if it's an interface or abstract method (no implementation to scan)
        var containingType = methodSymbol.ContainingType;
        var isInterfaceOrAbstract = containingType?.TypeKind == TypeKind.Interface ||
                                    methodSymbol.IsAbstract ||
                                    methodSymbol.IsVirtual;

        if (!isInterfaceOrAbstract) return false;

        // Try to extract [ReturnsError] attributes from the interface method
        var hasReturnsError = TryExtractReturnsErrorAttributes(
            methodSymbol, context, errorTypeNames, customErrors, seenCustomCodes);

        if (hasReturnsError) return true; // Successfully extracted errors from interface

        // If endpoint already has [ProducesError] attributes, assume developer knows what they're doing
        if (hasExplicitProducesError) return true; // No error, endpoint is explicitly documented

        // FAIL LOUD: Interface call without documentation
        var methodDisplayName = $"{containingType?.Name ?? "?"}.{methodSymbol.Name}";
        diagnostics.Add(DiagnosticInfo.Create(
            Descriptors.UndocumentedInterfaceCall,
            node.GetLocation(),
            endpointMethodName,
            methodDisplayName));

        return true;
    }

    /// <summary>
    ///     Extracts [ReturnsError] attributes from an interface/abstract method.
    ///     Returns true if any [ReturnsError] attributes were found.
    /// </summary>
    private static bool TryExtractReturnsErrorAttributes(
        ISymbol method,
        ErrorOrContext context,
        ISet<string> errorTypeNames,
        ICollection<CustomErrorInfo> customErrors,
        ISet<string> seenCustomCodes)
    {
        var foundAny = false;

        foreach (var attr in method.GetAttributes())
        {
            if (!ErrorOrContext.MatchesType(attr.AttributeClass, WellKnownTypes.ReturnsErrorAttribute)) continue;

            var args = attr.ConstructorArguments;
            if (args.Length < 2) continue;

            // Distinguish constructors by the first argument's type:
            // 1. ReturnsErrorAttribute(ErrorType errorType, string errorCode) — args[0].Type is enum
            // 2. ReturnsErrorAttribute(int statusCode, string errorCode) — args[0].Type is int
            if (args[0].Value is not int intValue || args[1].Value is not string errorCode) continue;

            if (args[0].Type is INamedTypeSymbol { TypeKind: TypeKind.Enum })
            {
                // Standard ErrorType — map enum int value to string name
                var errorTypeName = MapEnumValueToName(intValue);
                if (errorTypeName is not null) errorTypeNames.Add(errorTypeName);
            }
            else
            {
                // Custom error with explicit HTTP status code
                if (seenCustomCodes.Add(errorCode)) customErrors.Add(new CustomErrorInfo(errorCode));
            }

            foundAny = true;
        }

        return foundAny;
    }

    /// <summary>
    ///     Maps runtime ErrorType enum integer value to its name.
    ///     The enum values are: Failure=0, Unexpected=1, Validation=2, Conflict=3, NotFound=4, Unauthorized=5, Forbidden=6
    /// </summary>
    private static string? MapEnumValueToName(int enumValue)
    {
        return enumValue switch
        {
            0 => ErrorMapping.Failure,
            1 => ErrorMapping.Unexpected,
            2 => ErrorMapping.Validation,
            3 => ErrorMapping.Conflict,
            4 => ErrorMapping.NotFound,
            5 => ErrorMapping.Unauthorized,
            6 => ErrorMapping.Forbidden,
            _ => null
        };
    }

    private static bool ReturnsErrorOr(IMethodSymbol method, ErrorOrContext context)
    {
        // Reuse existing helpers - unwrap Task/ValueTask, then check for ErrorOr<T>
        var unwrapped = method.ReturnType.GetTaskResultType() ?? method.ReturnType;
        return IsErrorOrType(unwrapped, context, out _);
    }

    private static bool TryHandleErrorFactoryInvocation(
        SemanticModel semanticModel,
        SyntaxNode node,
        ISet<string> errorTypeNames,
        ICollection<CustomErrorInfo> customErrors,
        ISet<string> seenCustomCodes,
        ErrorOrContext context,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        if (!IsErrorFactoryInvocation(semanticModel, node, context, out var factoryName, out var invocation))
            return false;

        // Validate and return the factory name if it's a known ErrorType
        if (ErrorMapping.IsKnownErrorType(factoryName))
        {
            errorTypeNames.Add(factoryName);
            return true;
        }

        if (factoryName == "Custom" && invocation is not null)
        {
            var customInfo = ExtractCustomErrorInfo(semanticModel, invocation);
            if (customInfo is { } info && seenCustomCodes.Add(info.ErrorCode)) customErrors.Add(info);

            return true;
        }

        // Unknown factory method - report diagnostic
        // This fails loud instead of silently ignoring it or falling back to a default
        diagnostics.Add(DiagnosticInfo.Create(
            Descriptors.UnknownErrorFactory,
            node.GetLocation(),
            factoryName));

        return true;
    }

    private static bool TryGetReferencedSymbol(
        SemanticModel semanticModel,
        SyntaxNode node,
        ISet<ISymbol> visitedSymbols,
        [NotNullWhen(true)] out ISymbol? symbol)
    {
        // Conditional assignment: only resolve symbol for relevant syntax nodes
        symbol = node is IdentifierNameSyntax or MemberAccessExpressionSyntax
            ? semanticModel.GetSymbolInfo(node).Symbol
            : null;

        // Chained guards with short-circuit evaluation:
        // 1. Type check (also handles null)
        // 2. Same-assembly check (avoid external symbols)
        //    - ILocalSymbol has no ContainingAssembly but is always in scope (local to current method)
        // 3. Add to visited (side-effect only if we'll use it, returns false if duplicate)
        return symbol is IPropertySymbol or IFieldSymbol or ILocalSymbol or IMethodSymbol &&
               (symbol is ILocalSymbol ||
                symbol.ContainingAssembly?.IsEqualTo(semanticModel.Compilation.Assembly) == true) &&
               visitedSymbols.Add(symbol);
    }

    private static SyntaxNode? GetBodyToScan(SyntaxNode syntax)
    {
        return syntax switch
        {
            PropertyDeclarationSyntax p => (SyntaxNode?)p.ExpressionBody ?? p.AccessorList,
            MethodDeclarationSyntax m => (SyntaxNode?)m.Body ?? m.ExpressionBody,
            VariableDeclaratorSyntax v => v.Initializer,
            _ => syntax
        };
    }

    private static CustomErrorInfo? ExtractCustomErrorInfo(SemanticModel semanticModel,
        InvocationExpressionSyntax invocation)
    {
        // Error.Custom(int type, string code, string description, Dictionary<string, object>? metadata = null)
        // The 'code' parameter (second arg) is what we want for deduplication
        var args = invocation.ArgumentList.Arguments;
        if (args.Count < 2) return null;

        // Try to extract the 'code' (second argument)
        var codeArg = args[1].Expression;
        string? errorCode = null;

        // Try constant folding
        var constantValue = semanticModel.GetConstantValue(codeArg);
        if (constantValue is { HasValue: true, Value: string codeStr })
            errorCode = codeStr;
        else if (codeArg is LiteralExpressionSyntax { Token.Value: string literalStr }) errorCode = literalStr;

        // Pattern matching establishes non-null for compiler
        if (errorCode is not { Length: > 0 } code) return null;

        return new CustomErrorInfo(code);
    }

    private static bool IsErrorFactoryInvocation(
        SemanticModel semanticModel,
        SyntaxNode node,
        ErrorOrContext context,
        out string factoryName,
        out InvocationExpressionSyntax? invocation)
    {
        factoryName = string.Empty;
        invocation = null;

        if (node is not InvocationExpressionSyntax inv) return false;

        invocation = inv;

        // Fast-path: Error.X(...) where Error is a simple identifier
        if (inv.Expression is MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax { Identifier.Text: "Error" },
                Name: IdentifierNameSyntax { Identifier.Text: var name }
            })
        {
            factoryName = name;
            return true;
        }

        // Semantic fallback: resolve invoked method and ensure it's actually ErrorOr.Error.<X>
        if (semanticModel.GetSymbolInfo(inv).Symbol is not IMethodSymbol symbol ||
            !ErrorOrContext.MatchesType(symbol.ContainingType, WellKnownTypes.ErrorStruct))
        {
            return false;
        }

        factoryName = symbol.Name;
        return true;
    }

    private static EquatableArray<string> ToSortedErrorArray(HashSet<string> set)
    {
        if (set.Count is 0) return default;

        var array = set.ToArray();
        Array.Sort(array, StringComparer.Ordinal);
        return new EquatableArray<string>([.. array]);
    }

}
