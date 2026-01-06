using System.Diagnostics.CodeAnalysis;
using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ErrorOr.MinimalApi;

/// <summary>
///     Partial class containing extraction and parameter binding logic.
/// </summary>
public sealed partial class ErrorOrEndpointGenerator
{
    /// <summary>
    ///     Extracts the ErrorOr return type information from a method's return type.
    /// </summary>
    internal static ErrorOrReturnTypeInfo ExtractErrorOrReturnType(ITypeSymbol returnType)
    {
        var (unwrapped, isAsync) = UnwrapAsyncType(returnType);

        if (!IsErrorOrType(unwrapped, out var errorOrType))
            return new ErrorOrReturnTypeInfo(null, false, false, null, false);

        var innerType = errorOrType.TypeArguments[0];

        if (TryUnwrapAsyncEnumerable(innerType, out var elementType))
        {
            if (TryUnwrapSseItem(elementType, out var sseDataType))
            {
                var sseDataFqn = sseDataType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var asyncEnumFqn = innerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                return new ErrorOrReturnTypeInfo(asyncEnumFqn, isAsync, true, sseDataFqn, true);
            }
            else
            {
                var elementFqn = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var asyncEnumFqn = innerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                return new ErrorOrReturnTypeInfo(asyncEnumFqn, isAsync, true, elementFqn, false);
            }
        }

        var successTypeFqn = innerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return new ErrorOrReturnTypeInfo(successTypeFqn, isAsync, false, null, false);
    }

    private static bool TryUnwrapAsyncEnumerable(
        ITypeSymbol type,
        [NotNullWhen(true)] out ITypeSymbol? elementType)
    {
        if (type is INamedTypeSymbol { IsGenericType: true } named &&
            named.ConstructedFrom.ToDisplayString() == WellKnownTypes.IAsyncEnumerableT)
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
            named.ConstructedFrom.ToDisplayString() == WellKnownTypes.SseItemT)
        {
            dataType = named.TypeArguments[0];
            return true;
        }

        dataType = null;
        return false;
    }

    private static (ITypeSymbol Type, bool IsAsync) UnwrapAsyncType(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol { IsGenericType: true } named)
            return (type, false);

        var constructedFrom = named.ConstructedFrom.ToDisplayString();
        return constructedFrom is WellKnownTypes.TaskT or WellKnownTypes.ValueTaskT
            ? (named.TypeArguments[0], true)
            : (type, false);
    }

    private static bool IsErrorOrType(
        ITypeSymbol type,
        [NotNullWhen(true)] out INamedTypeSymbol? errorOrType)
    {
        if (type is INamedTypeSymbol { IsGenericType: true } named &&
            named.ConstructedFrom.ToDisplayString() == WellKnownTypes.ErrorOrT)
        {
            errorOrType = named;
            return true;
        }

        errorOrType = null;
        return false;
    }

    internal static (EquatableArray<int> ErrorTypes, EquatableArray<CustomErrorInfo> CustomErrors)
        InferErrorTypesFromMethod(GeneratorAttributeSyntaxContext ctx, IMethodSymbol method)
    {
        var body = GetMethodBody(method);
        if (body is null)
            return (default, default);

        var (errorTypes, customErrors) = CollectErrorTypes(ctx.SemanticModel, body);
        return (ToSortedErrorArray(errorTypes), new EquatableArray<CustomErrorInfo>([.. customErrors]));
    }

    internal static EquatableArray<ProducesErrorInfo> ExtractProducesErrorAttributes(IMethodSymbol method)
    {
        var results = new List<ProducesErrorInfo>();

        foreach (var attr in method.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() != WellKnownTypes.ProducesErrorAttribute)
                continue;

            if (attr.ConstructorArguments is { Length: >= 2 } args &&
                args[0].Value is int statusCode &&
                args[1].Value is string errorCode)
                results.Add(new ProducesErrorInfo(statusCode, errorCode));
        }

        return results.Count > 0
            ? new EquatableArray<ProducesErrorInfo>([.. results])
            : default;
    }

    /// <summary>
    ///     Checks if the method has the [AcceptedResponse] attribute for 202 Accepted responses.
    /// </summary>
    internal static bool HasAcceptedResponseAttribute(IMethodSymbol method)
    {
        foreach (var attr in method.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == WellKnownTypes.AcceptedResponseAttribute)
                return true;
        }

        return false;
    }

    private static SyntaxNode? GetMethodBody(IMethodSymbol method)
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

    private static (HashSet<int> ErrorTypes, List<CustomErrorInfo> CustomErrors) CollectErrorTypes(
        SemanticModel semanticModel, SyntaxNode body)
    {
        var set = new HashSet<int>();
        var customErrors = new List<CustomErrorInfo>();
        var visitedSymbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        var seenCustomCodes = new HashSet<string>(StringComparer.Ordinal);
        CollectRecursive(body, visitedSymbols);
        return (set, customErrors);

        void CollectRecursive(SyntaxNode node, HashSet<ISymbol> visited)
        {
            foreach (var child in node.DescendantNodes())
            {
                if (IsErrorFactoryInvocation(child, out var factoryName, out var invocation))
                {
                    var errorType = MapErrorFactoryToType(factoryName);
                    if (errorType >= 0)
                        set.Add(errorType);
                    else if (factoryName == "Custom" && invocation is not null)
                    {
                        // Extract custom error info
                        var customInfo = ExtractCustomErrorInfo(semanticModel, invocation);
                        if (customInfo is not null && seenCustomCodes.Add(customInfo.Value.ErrorCode))
                            customErrors.Add(customInfo.Value);
                    }

                    continue;
                }

                if (child is IdentifierNameSyntax or MemberAccessExpressionSyntax)
                {
                    var symbol = semanticModel.GetSymbolInfo(child).Symbol;
                    if (symbol is null || !visited.Add(symbol))
                        continue;

                    if (!SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly,
                            semanticModel.Compilation.Assembly))
                        continue;

                    if (symbol is IPropertySymbol or IFieldSymbol or ILocalSymbol or IMethodSymbol)
                    {
                        foreach (var reference in symbol.DeclaringSyntaxReferences)
                        {
                            var syntax = reference.GetSyntax();
                            var bodyToScan = syntax switch
                            {
                                PropertyDeclarationSyntax p => (SyntaxNode?)p.ExpressionBody ?? p.AccessorList,
                                MethodDeclarationSyntax m => (SyntaxNode?)m.Body ?? m.ExpressionBody,
                                VariableDeclaratorSyntax v => v.Initializer,
                                _ => syntax
                            };

                            if (bodyToScan != null)
                                CollectRecursive(bodyToScan, visited);
                        }
                    }
                }
            }
        }
    }

    private static CustomErrorInfo? ExtractCustomErrorInfo(SemanticModel semanticModel,
        InvocationExpressionSyntax invocation)
    {
        // Error.Custom(int type, string code, string description, Dictionary<string, object>? metadata = null)
        // The 'type' parameter is the custom error type (numeric), and 'code' is what we want
        var args = invocation.ArgumentList.Arguments;
        if (args.Count < 2)
            return null;

        // Try to extract the 'code' (second argument)
        var codeArg = args[1].Expression;
        string? errorCode = null;

        // Try constant folding
        var constantValue = semanticModel.GetConstantValue(codeArg);
        if (constantValue.HasValue && constantValue.Value is string codeStr)
            errorCode = codeStr;
        else if (codeArg is LiteralExpressionSyntax literal && literal.Token.Value is string literalStr)
            errorCode = literalStr;

        // Pattern matching establishes non-null for compiler
        if (errorCode is not { Length: > 0 } code)
            return null;

        // Try to extract the numeric type (first argument) to suggest a status code
        var typeArg = args[0].Expression;
        var suggestedStatus = 500; // Default to 500 for custom errors

        var typeConstant = semanticModel.GetConstantValue(typeArg);
        if (typeConstant.HasValue && typeConstant.Value is int typeValue)
        {
            // Map common custom type values to HTTP status codes
            // Users can define their own mappings, but we suggest based on common patterns
            suggestedStatus = typeValue switch
            {
                >= 400 and < 600 => typeValue, // If it's already an HTTP status code
                _ => 500 // Default to 500 for unknown custom types
            };
        }

        return new CustomErrorInfo(code, suggestedStatus);
    }

    private static bool IsErrorFactoryInvocation(SyntaxNode node, out string factoryName,
        out InvocationExpressionSyntax? invocation)
    {
        factoryName = string.Empty;
        invocation = null;

        if (node is not InvocationExpressionSyntax inv)
            return false;

        if (inv.Expression is not MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax { Identifier.Text: "Error" },
                Name: IdentifierNameSyntax { Identifier.Text: var name }
            })
            return false;

        factoryName = name;
        invocation = inv;
        return true;
    }

    private static int MapErrorFactoryToType(string factoryName)
    {
        return factoryName switch
        {
            "Failure" => 0,
            "Unexpected" => 1,
            "Validation" => 2,
            "Conflict" => 3,
            "NotFound" => 4,
            "Unauthorized" => 5,
            "Forbidden" => 6,
            _ => -1
        };
    }

    private static EquatableArray<int> ToSortedErrorArray(HashSet<int> set)
    {
        if (set.Count == 0)
            return default;

        var array = set.ToArray();
        Array.Sort(array);
        return new EquatableArray<int>([.. array]);
    }

    internal static (bool IsObsolete, string? Message, bool IsError) GetObsoleteInfo(IMethodSymbol method,
        ErrorOrContext context)
    {
        AttributeData? attr = null;
        foreach (var a in method.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(a.AttributeClass, context.ObsoleteAttribute))
            {
                attr = a;
                break;
            }
        }

        if (attr is null) return (false, null, false);

        var message = attr.ConstructorArguments.Length > 0 ? attr.ConstructorArguments[0].Value as string : null;
        var isError = attr.ConstructorArguments.Length > 1 &&
                      attr.ConstructorArguments[1].Value is true; // Safe pattern match for bool
        return (true, message, isError);
    }

    /// <summary>
    ///     Result of extracting the ErrorOr return type, including SSE detection.
    /// </summary>
    internal readonly record struct ErrorOrReturnTypeInfo(
        string? SuccessTypeFqn,
        bool IsAsync,
        bool IsSse,
        string? SseItemTypeFqn,
        bool UsesSseItem);
}
