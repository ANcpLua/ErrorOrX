using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using ANcpLua.Roslyn.Utilities;
using ANcpLua.Roslyn.Utilities.Models;
using ErrorOr.Endpoints.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ErrorOr.Endpoints.Generators;

/// <summary>
///     Partial class containing extraction and parameter binding logic.
/// </summary>
public sealed partial class ErrorOrEndpointGenerator
{
    /// <summary>
    ///     Extracts the ErrorOr return type information from a method's return type.
    /// </summary>
    internal static ErrorOrReturnTypeInfo ExtractErrorOrReturnType(ITypeSymbol returnType, ErrorOrContext context)
    {
        var (unwrapped, isAsync) = UnwrapAsyncType(returnType, context);

        if (!IsErrorOrType(unwrapped, context, out var errorOrType))
            return new ErrorOrReturnTypeInfo(null, false, false, null, SuccessKind.Payload);

        var innerType = errorOrType.TypeArguments[0];
        var kind = SuccessKind.Payload;

        if (context.SuccessMarker is not null &&
            SymbolEqualityComparer.Default.Equals(innerType, context.SuccessMarker))
            kind = SuccessKind.Success;
        else if (context.CreatedMarker is not null &&
                 SymbolEqualityComparer.Default.Equals(innerType, context.CreatedMarker))
            kind = SuccessKind.Created;
        else if (context.UpdatedMarker is not null &&
                 SymbolEqualityComparer.Default.Equals(innerType, context.UpdatedMarker))
            kind = SuccessKind.Updated;
        else if (context.DeletedMarker is not null &&
                 SymbolEqualityComparer.Default.Equals(innerType, context.DeletedMarker))
            kind = SuccessKind.Deleted;

        if (TryUnwrapAsyncEnumerable(innerType, context, out var elementType))
        {
            if (TryUnwrapSseItem(elementType, context, out var sseDataType))
            {
                var sseDataFqn = sseDataType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var asyncEnumFqn = innerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                return new ErrorOrReturnTypeInfo(asyncEnumFqn, isAsync, true, sseDataFqn, kind);
            }
            else
            {
                var elementFqn = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var asyncEnumFqn = innerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                return new ErrorOrReturnTypeInfo(asyncEnumFqn, isAsync, true, elementFqn, kind);
            }
        }

        var successTypeFqn = innerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return new ErrorOrReturnTypeInfo(successTypeFqn, isAsync, false, null, kind);
    }

    private static bool TryUnwrapAsyncEnumerable(
        ITypeSymbol type,
        ErrorOrContext context,
        [NotNullWhen(true)] out ITypeSymbol? elementType)
    {
        if (type is INamedTypeSymbol { IsGenericType: true } named &&
            context.IAsyncEnumerableOfT is not null &&
            SymbolEqualityComparer.Default.Equals(named.ConstructedFrom, context.IAsyncEnumerableOfT))
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
            context.SseItemOfT is not null &&
            SymbolEqualityComparer.Default.Equals(named.ConstructedFrom, context.SseItemOfT))
        {
            dataType = named.TypeArguments[0];
            return true;
        }

        dataType = null;
        return false;
    }

    private static (ITypeSymbol Type, bool IsAsync) UnwrapAsyncType(ITypeSymbol type, ErrorOrContext context)
    {
        if (type is not INamedTypeSymbol { IsGenericType: true } named)
            return (type, false);

        var constructed = named.ConstructedFrom;

        if (context.TaskOfT is not null && SymbolEqualityComparer.Default.Equals(constructed, context.TaskOfT))
            return (named.TypeArguments[0], true);

        if (context.ValueTaskOfT is not null &&
            SymbolEqualityComparer.Default.Equals(constructed, context.ValueTaskOfT))
            return (named.TypeArguments[0], true);

        return (type, false);
    }

    private static bool IsErrorOrType(
        ITypeSymbol type,
        ErrorOrContext context,
        [NotNullWhen(true)] out INamedTypeSymbol? errorOrType)
    {
        if (type is INamedTypeSymbol { IsGenericType: true } named &&
            context.ErrorOrOfT is not null &&
            SymbolEqualityComparer.Default.Equals(named.ConstructedFrom, context.ErrorOrOfT))
        {
            errorOrType = named;
            return true;
        }

        errorOrType = null;
        return false;
    }

    private static (EquatableArray<int> ErrorTypes, EquatableArray<CustomErrorInfo> CustomErrors)
        InferErrorTypesFromMethod(
            GeneratorAttributeSyntaxContext ctx,
            ISymbol method,
            ErrorOrContext context,
            ImmutableArray<DiagnosticInfo>.Builder diagnostics,
            bool hasExplicitProducesError)
    {
        var body = GetMethodBody(method);
        if (body is null)
            return (default, default);

        var methodName = method.Name;
        var (errorTypes, customErrors) = CollectErrorTypes(ctx.SemanticModel, body, context, diagnostics, methodName,
            hasExplicitProducesError);
        return (ToSortedErrorArray(errorTypes), new EquatableArray<CustomErrorInfo>([.. customErrors]));
    }

    private static EquatableArray<ProducesErrorInfo> ExtractProducesErrorAttributes(
        ISymbol method,
        ErrorOrContext context)
    {
        var results = new List<ProducesErrorInfo>();

        foreach (var attr in method.GetAttributes())
            if (context.ProducesErrorAttribute is not null &&
                SymbolEqualityComparer.Default.Equals(attr.AttributeClass, context.ProducesErrorAttribute))
                if (attr.ConstructorArguments.Length >= 1 &&
                    attr.ConstructorArguments[0].Value is int statusCode)
                    results.Add(new ProducesErrorInfo(statusCode));

        return results.Count > 0
            ? new EquatableArray<ProducesErrorInfo>([.. results])
            : default;
    }

    /// <summary>
    ///     Checks if the method has the [AcceptedResponse] attribute for 202 Accepted responses.
    /// </summary>
    private static bool HasAcceptedResponseAttribute(ISymbol method, ErrorOrContext context)
    {
        foreach (var attr in method.GetAttributes())
            if (context.AcceptedResponseAttribute is not null &&
                SymbolEqualityComparer.Default.Equals(attr.AttributeClass, context.AcceptedResponseAttribute))
                return true;

        return false;
    }

    private static SyntaxNode? GetMethodBody(ISymbol method)
    {
        if (method.DeclaringSyntaxReferences.IsDefaultOrEmpty)
            return null;

        var syntax = method.DeclaringSyntaxReferences[0].GetSyntax();
        return syntax switch
        {
            MethodDeclarationSyntax m => (SyntaxNode?)m.Body ?? m.ExpressionBody,
            LocalFunctionStatementSyntax f => (SyntaxNode?)f.Body ?? f.ExpressionBody,
            _ => null
        };
    }

    private static (HashSet<ErrorType> ErrorTypes, List<CustomErrorInfo> CustomErrors) CollectErrorTypes(
        SemanticModel semanticModel,
        SyntaxNode body,
        ErrorOrContext context,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        string endpointMethodName,
        bool hasExplicitProducesError)
    {
        var set = new HashSet<ErrorType>();
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
        ISet<ErrorType> errorTypes,
        ICollection<CustomErrorInfo> customErrors,
        ISet<ISymbol> visitedSymbols,
        ISet<string> seenCustomCodes,
        ErrorOrContext context,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        string endpointMethodName,
        bool hasExplicitProducesError)
    {
        foreach (var child in node.DescendantNodes())
            ProcessNode(semanticModel, child, errorTypes, customErrors, visitedSymbols, seenCustomCodes, context,
                diagnostics, endpointMethodName, hasExplicitProducesError);
    }

    private static void ProcessNode(
        SemanticModel semanticModel,
        SyntaxNode child,
        ISet<ErrorType> errorTypes,
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
                errorTypes,
                customErrors,
                seenCustomCodes,
                context,
                diagnostics))
            return;

        // Check for interface/abstract method calls that return ErrorOr
        if (TryDetectUndocumentedInterfaceCall(
                semanticModel,
                child,
                context,
                endpointMethodName,
                hasExplicitProducesError,
                diagnostics,
                errorTypes,
                customErrors,
                seenCustomCodes))
            return;

        if (!TryGetReferencedSymbol(semanticModel, child, visitedSymbols, out var symbol))
            return;

        foreach (var reference in symbol.DeclaringSyntaxReferences)
        {
            var bodyToScan = GetBodyToScan(reference.GetSyntax());
            if (bodyToScan is not null)
                CollectErrorTypesRecursive(semanticModel, bodyToScan, errorTypes, customErrors,
                    visitedSymbols, seenCustomCodes, context, diagnostics, endpointMethodName,
                    hasExplicitProducesError);
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
        ISet<ErrorType> errorTypes,
        ICollection<CustomErrorInfo> customErrors,
        ISet<string> seenCustomCodes)
    {
        // Only check invocation expressions
        if (node is not InvocationExpressionSyntax invocation)
            return false;

        var symbolInfo = semanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return false;

        // Check if method returns ErrorOr<T>
        if (!ReturnsErrorOr(methodSymbol, context))
            return false;

        // Check if it's an interface or abstract method (no implementation to scan)
        var containingType = methodSymbol.ContainingType;
        var isInterfaceOrAbstract = containingType?.TypeKind == TypeKind.Interface ||
                                    methodSymbol.IsAbstract ||
                                    methodSymbol.IsVirtual;

        if (!isInterfaceOrAbstract)
            return false;

        // Try to extract [ReturnsError] attributes from the interface method
        var hasReturnsError = TryExtractReturnsErrorAttributes(
            methodSymbol, context, errorTypes, customErrors, seenCustomCodes);

        if (hasReturnsError)
            return true; // Successfully extracted errors from interface

        // If endpoint already has [ProducesError] attributes, assume developer knows what they're doing
        if (hasExplicitProducesError)
            return true; // No error, endpoint is explicitly documented

        // FAIL LOUD: Interface call without documentation
        var methodDisplayName = $"{containingType?.Name ?? "?"}.{methodSymbol.Name}";
        diagnostics.Add(DiagnosticInfo.Create(
            Descriptors.UndocumentedInterfaceCall,
            node,
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
        ISet<ErrorType> errorTypes,
        ICollection<CustomErrorInfo> customErrors,
        ISet<string> seenCustomCodes)
    {
        if (context.ReturnsErrorAttribute is null)
            return false;

        var foundAny = false;

        foreach (var attr in method.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, context.ReturnsErrorAttribute))
                continue;

            var args = attr.ConstructorArguments;
            if (args.Length < 2)
                continue;

            // Check which constructor was used:
            // 1. ReturnsErrorAttribute(ErrorType errorType, string errorCode)
            // 2. ReturnsErrorAttribute(int statusCode, string errorCode)

            if (args[0].Value is int statusCode && args[1].Value is string customErrorCode)
            {
                // Custom error with status code
                if (seenCustomCodes.Add(customErrorCode))
                    customErrors.Add(new CustomErrorInfo(statusCode, customErrorCode));

                foundAny = true;
            }
            else if (args[0].Value is int enumValue && args[1].Value is string)
            {
                // Standard ErrorType
                var errorType = (ErrorType)enumValue;
                errorTypes.Add(errorType);
                foundAny = true;
            }
        }

        return foundAny;
    }

    private static bool ReturnsErrorOr(IMethodSymbol method, ErrorOrContext context)
    {
        var returnType = method.ReturnType;

        // Handle Task<ErrorOr<T>> and ValueTask<ErrorOr<T>>
        if (returnType is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            var typeName = namedType.ConstructedFrom.ToDisplayString();
            if (typeName.StartsWith("System.Threading.Tasks.Task<") ||
                typeName.StartsWith("System.Threading.Tasks.ValueTask<"))
                returnType = namedType.TypeArguments[0];
        }

        // Check if it's ErrorOr<T>
        return returnType is INamedTypeSymbol { IsGenericType: true } errorOrType &&
               context.ErrorOrOfT is not null &&
               SymbolEqualityComparer.Default.Equals(errorOrType.ConstructedFrom, context.ErrorOrOfT);
    }

    private static bool TryHandleErrorFactoryInvocation(
        SemanticModel semanticModel,
        SyntaxNode node,
        ISet<ErrorType> errorTypes,
        ICollection<CustomErrorInfo> customErrors,
        ISet<string> seenCustomCodes,
        ErrorOrContext context,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        if (!IsErrorFactoryInvocation(semanticModel, node, context, out var factoryName, out var invocation))
            return false;

        var errorType = MapErrorFactoryToType(factoryName);
        if (errorType.HasValue)
        {
            errorTypes.Add(errorType.Value);
            return true;
        }

        if (factoryName == "Custom" && invocation is not null)
        {
            var customInfo = ExtractCustomErrorInfo(semanticModel, invocation);
            if (customInfo is { } info && seenCustomCodes.Add(info.ErrorCode))
                customErrors.Add(info);
            return true;
        }

        // Unknown factory method - report diagnostic
        // This fails loud instead of silently ignoring it or falling back to a default
        diagnostics.Add(DiagnosticInfo.Create(
            Descriptors.UnknownErrorFactory,
            node,
            factoryName));

        return true;
    }

    private static bool TryGetReferencedSymbol(
        SemanticModel semanticModel,
        SyntaxNode node,
        ISet<ISymbol> visitedSymbols,
        [NotNullWhen(true)] out ISymbol? symbol)
    {
        symbol = null;
        if (node is not IdentifierNameSyntax and not MemberAccessExpressionSyntax)
            return false;

        symbol = semanticModel.GetSymbolInfo(node).Symbol;
        if (symbol is null || !visitedSymbols.Add(symbol))
            return false;

        if (!SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, semanticModel.Compilation.Assembly))
            return false;

        return symbol is IPropertySymbol or IFieldSymbol or ILocalSymbol or IMethodSymbol;
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
        // The 'type' parameter is the custom error type (numeric), and 'code' is what we want
        var args = invocation.ArgumentList.Arguments;
        if (args.Count < 2)
            return null;

        // Extract status code (first argument)
        var statusCode = 500; // Default fallback
        var typeArg = args[0].Expression;
        var typeConstant = semanticModel.GetConstantValue(typeArg);
        if (typeConstant is { HasValue: true, Value: int typeValue })
            statusCode = typeValue;
        else if (typeArg is LiteralExpressionSyntax { Token.Value: int literalType })
            statusCode = literalType;

        // Try to extract the 'code' (second argument)
        var codeArg = args[1].Expression;
        string? errorCode = null;

        // Try constant folding
        var constantValue = semanticModel.GetConstantValue(codeArg);
        if (constantValue is { HasValue: true, Value: string codeStr })
            errorCode = codeStr;
        else if (codeArg is LiteralExpressionSyntax { Token.Value: string literalStr })
            errorCode = literalStr;

        // Pattern matching establishes non-null for compiler
        if (errorCode is not { Length: > 0 } code)
            return null;

        return new CustomErrorInfo(statusCode, code);
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

        if (node is not InvocationExpressionSyntax inv)
            return false;

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

        // Semantic fallback: resolve invoked method and ensure it's actually ErrorOr.Core.Errors.Error.<X>
        var symbol = semanticModel.GetSymbolInfo(inv).Symbol as IMethodSymbol;
        if (symbol is null || context.Error is null ||
            !SymbolEqualityComparer.Default.Equals(symbol.ContainingType, context.Error))
            return false;

        factoryName = symbol.Name;
        return true;
    }

    private static ErrorType? MapErrorFactoryToType(string factoryName)
    {
        return factoryName switch
        {
            "Failure" => ErrorType.Failure,
            "Unexpected" => ErrorType.Unexpected,
            "Validation" => ErrorType.Validation,
            "Conflict" => ErrorType.Conflict,
            "NotFound" => ErrorType.NotFound,
            "Unauthorized" => ErrorType.Unauthorized,
            "Forbidden" => ErrorType.Forbidden,
            _ => null
        };
    }

    private static EquatableArray<int> ToSortedErrorArray(ICollection<ErrorType> set)
    {
        if (set.Count is 0)
            return default;

        // Cast enums to int and sort
        var array = set.Select(static e => (int)e).ToArray();
        Array.Sort(array);
        return new EquatableArray<int>([.. array]);
    }

    // ReSharper disable once UnusedMember.Local
    private static (bool IsObsolete, string? Message, bool IsError) GetObsoleteInfo(ISymbol method,
        ErrorOrContext context)
    {
        AttributeData? attr = null;
        foreach (var a in method.GetAttributes())
            if (SymbolEqualityComparer.Default.Equals(a.AttributeClass, context.ObsoleteAttribute))
            {
                attr = a;
                break;
            }

        if (attr is null) return (false, null, false);

        var message = attr.ConstructorArguments.Length > 0 ? attr.ConstructorArguments[0].Value as string : null;
        var isError = attr.ConstructorArguments.Length > 1 &&
                      attr.ConstructorArguments[1].Value is true; // Safe pattern match for bool
        return (true, message, isError);
    }
}