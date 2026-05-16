using ANcpLua.Roslyn.Utilities.Models;
using ErrorOr.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ErrorOr.Generators;

/// <summary>
///     Detects calls to interface or abstract methods that return <c>ErrorOr&lt;T&gt;</c>.
///     If the called declaration carries <c>[ReturnsError]</c>, the errors flow into the
///     union-type computation. Otherwise, if the endpoint itself isn't documented with
///     <c>[ProducesError]</c>, the EOE024 diagnostic fires (fail-loud rather than
///     silently producing a 500-only Results union).
/// </summary>
public sealed partial class ErrorOrEndpointGenerator
{
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
}
