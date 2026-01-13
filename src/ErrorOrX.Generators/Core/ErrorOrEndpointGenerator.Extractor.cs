using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using ANcpLua.Roslyn.Utilities;
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
        var idPropertyName = DetectIdProperty(innerType);
        return new ErrorOrReturnTypeInfo(successTypeFqn, isAsync, false, null, kind, idPropertyName);
    }

    /// <summary>
    ///     Detects a suitable Id property on the success type for Location header generation.
    ///     Looks for properties named Id, ID, id (case-insensitive), preferring exact "Id" match.
    /// </summary>
    private static string? DetectIdProperty(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType)
            return null;

        // Skip marker types and primitives
        if (type.SpecialType != SpecialType.None)
            return null;

        string? bestMatch = null;

        foreach (var member in namedType.GetMembers())
        {
            if (member is not IPropertySymbol property)
                continue;

            // Must be public, readable, and not write-only
            if (property.DeclaredAccessibility != Accessibility.Public)
                continue;
            if (property.GetMethod is null)
                continue;

            var name = property.Name;

            // Exact match "Id" is preferred
            if (name == "Id")
                return "Id";

            // Case-insensitive match for fallback
            if (string.Equals(name, "Id", StringComparison.OrdinalIgnoreCase))
                bestMatch ??= name;
        }

        return bestMatch;
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

    private static (EquatableArray<string> ErrorTypeNames, EquatableArray<CustomErrorInfo> CustomErrors)
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
            if (context.ProducesErrorAttribute is not null &&
                SymbolEqualityComparer.Default.Equals(attr.AttributeClass, context.ProducesErrorAttribute))
                if (attr.ConstructorArguments is [{ Value: int statusCode } _, ..])
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
            ProcessNode(semanticModel, child, errorTypeNames, customErrors, visitedSymbols, seenCustomCodes, context,
                diagnostics, endpointMethodName, hasExplicitProducesError);
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
            return;

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
            return;

        if (!TryGetReferencedSymbol(semanticModel, child, visitedSymbols, out var symbol))
            return;

        foreach (var reference in symbol.DeclaringSyntaxReferences)
        {
            var bodyToScan = GetBodyToScan(reference.GetSyntax());
            if (bodyToScan is not null)
                CollectErrorTypesRecursive(semanticModel, bodyToScan, errorTypeNames, customErrors,
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
        ISet<string> errorTypeNames,
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
            methodSymbol, context, errorTypeNames, customErrors, seenCustomCodes);

        if (hasReturnsError)
            return true; // Successfully extracted errors from interface

        // If endpoint already has [ProducesError] attributes, assume developer knows what they're doing
        if (hasExplicitProducesError)
            return true; // No error, endpoint is explicitly documented

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

            switch (args[0].Value)
            {
                case int when args[1].Value is string customErrorCode:
                {
                    // Custom error with status code
                    if (seenCustomCodes.Add(customErrorCode))
                        customErrors.Add(new CustomErrorInfo(customErrorCode));

                    foundAny = true;
                    break;
                }
                case int enumValue when args[1].Value is string:
                {
                    // Standard ErrorType - map enum int value to string name
                    var errorTypeName = MapEnumValueToName(enumValue);
                    if (errorTypeName is not null)
                        errorTypeNames.Add(errorTypeName);
                    foundAny = true;
                    break;
                }
            }
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
            if (customInfo is { } info && seenCustomCodes.Add(info.ErrorCode))
                customErrors.Add(info);
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
        // The 'code' parameter (second arg) is what we want for deduplication
        var args = invocation.ArgumentList.Arguments;
        if (args.Count < 2)
            return null;

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

        // Semantic fallback: resolve invoked method and ensure it's actually ErrorOr.Error.<X>
        if (semanticModel.GetSymbolInfo(inv).Symbol is not IMethodSymbol symbol || context.Error is null ||
            !SymbolEqualityComparer.Default.Equals(symbol.ContainingType, context.Error))
            return false;

        factoryName = symbol.Name;
        return true;
    }

    private static EquatableArray<string> ToSortedErrorArray(ICollection<string> set)
    {
        if (set.Count is 0)
            return default;

        var array = set.ToArray();
        Array.Sort(array, StringComparer.Ordinal);
        return new EquatableArray<string>([.. array]);
    }

    /// <summary>
    ///     Extracts middleware configuration from BCL attributes on the method.
    ///     Detects [Authorize], [AllowAnonymous], [EnableRateLimiting], [DisableRateLimiting],
    ///     [OutputCache], [EnableCors], [DisableCors].
    /// </summary>
    internal static MiddlewareInfo ExtractMiddlewareAttributes(ISymbol method, ErrorOrContext context)
    {
        var auth = default(AuthInfo);
        var rateLimit = default(RateLimitInfo);
        var cache = default(OutputCacheInfo);
        var cors = default(CorsInfo);

        foreach (var attr in method.GetAttributes())
        {
            var attrClass = attr.AttributeClass;
            if (attrClass is null)
                continue;

            auth = TryExtractAuth(attr, attrClass, context, auth);
            rateLimit = TryExtractRateLimit(attr, attrClass, context, rateLimit);
            cache = TryExtractOutputCache(attr, attrClass, context, cache);
            cors = TryExtractCors(attr, attrClass, context, cors);
        }

        return new MiddlewareInfo(
            auth.Required, auth.Policy, auth.AllowAnonymous,
            rateLimit.Enabled, rateLimit.Policy, rateLimit.Disabled,
            cache.Enabled, cache.Policy, cache.Duration,
            cors.Enabled, cors.Policy, cors.Disabled);
    }

    private static AuthInfo TryExtractAuth(
        AttributeData attr, INamedTypeSymbol attrClass, ErrorOrContext context, AuthInfo current)
    {
        if (context.AuthorizeAttribute is not null &&
            SymbolEqualityComparer.Default.Equals(attrClass, context.AuthorizeAttribute))
        {
            var policy = attr.ConstructorArguments is [{ Value: string p }] ? p : null;
            policy ??= attr.NamedArguments.FirstOrDefault(static a => a.Key == "Policy").Value.Value as string;
            return current with { Required = true, Policy = policy ?? current.Policy };
        }

        if (context.AllowAnonymousAttribute is not null &&
            SymbolEqualityComparer.Default.Equals(attrClass, context.AllowAnonymousAttribute))
            return current with { AllowAnonymous = true };

        return current;
    }

    private static RateLimitInfo TryExtractRateLimit(
        AttributeData attr, INamedTypeSymbol attrClass, ErrorOrContext context, RateLimitInfo current)
    {
        if (context.EnableRateLimitingAttribute is not null &&
            SymbolEqualityComparer.Default.Equals(attrClass, context.EnableRateLimitingAttribute))
        {
            var policy = attr.ConstructorArguments is [{ Value: string p }] ? p : null;
            return current with { Enabled = true, Policy = policy };
        }

        if (context.DisableRateLimitingAttribute is not null &&
            SymbolEqualityComparer.Default.Equals(attrClass, context.DisableRateLimitingAttribute))
            return current with { Disabled = true };

        return current;
    }

    private static OutputCacheInfo TryExtractOutputCache(
        AttributeData attr, INamedTypeSymbol attrClass, ErrorOrContext context, OutputCacheInfo current)
    {
        if (context.OutputCacheAttribute is null ||
            !SymbolEqualityComparer.Default.Equals(attrClass, context.OutputCacheAttribute))
            return current;

        var result = current with { Enabled = true };
        foreach (var namedArg in attr.NamedArguments)
        {
            if (namedArg is { Key: "PolicyName", Value.Value: string policy })
                result = result with { Policy = policy };
            if (namedArg is { Key: "Duration", Value.Value: int duration })
                result = result with { Duration = duration };
        }

        return result;
    }

    private static CorsInfo TryExtractCors(
        AttributeData attr, INamedTypeSymbol attrClass, ErrorOrContext context, CorsInfo current)
    {
        if (context.EnableCorsAttribute is not null &&
            SymbolEqualityComparer.Default.Equals(attrClass, context.EnableCorsAttribute))
        {
            var policy = attr.ConstructorArguments is [{ Value: string p }] ? p : null;
            return current with { Enabled = true, Policy = policy };
        }

        if (context.DisableCorsAttribute is not null &&
            SymbolEqualityComparer.Default.Equals(attrClass, context.DisableCorsAttribute))
            return current with { Disabled = true };

        return current;
    }

    // Helper records for middleware extraction
    private readonly record struct AuthInfo(bool Required, string? Policy, bool AllowAnonymous);

    private readonly record struct RateLimitInfo(bool Enabled, string? Policy, bool Disabled);

    private readonly record struct OutputCacheInfo(bool Enabled, string? Policy, int? Duration);

    private readonly record struct CorsInfo(bool Enabled, string? Policy, bool Disabled);
}