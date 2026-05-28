using ANcpLua.Roslyn.Utilities.Models;
using ErrorOr.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ErrorOr.Generators;

/// <summary>
///     Body-walking error inference: descends through a handler's syntax tree
///     (and through called same-assembly symbols) to collect every <c>Error.X()</c> and
///     <c>Error.Custom("code", ...)</c> factory invocation. Drives the union-type computation
///     and the <c>[ProducesError]</c> documentation diagnostics.
/// </summary>
public sealed partial class ErrorOrEndpointGenerator
{
    private static (EquatableArray<string> ErrorTypeNames, EquatableArray<CustomErrorInfo> CustomErrors)
        InferErrorTypesFromMethod(
            GeneratorAttributeSyntaxContext ctx,
            ISymbol method,
            ErrorOrContext context,
            ImmutableArray<DiagnosticInfo>.Builder diagnostics,
            bool hasExplicitProducesError)
    {
        // Endpoint handlers are static methods with bodies — neither abstract nor partial-without-impl
        // is valid (the [Get]/[Post]/etc. attributes only apply to static methods that compile to real
        // code). Expression-bodied handlers (=> svc.GetAll()) are caught by GetMethodBody. The only way
        // GetMethodBody returns null is when the handler ISN'T a valid endpoint shape — which is
        // already an EOE002 / EOE001 (handler must be static, invalid return type) error elsewhere.
        // Returning empty here is the right behavior: those other diagnostics will block the build, and
        // inference shouldn't double-report by raising its own diagnostic for the same root cause.
        if (GetMethodBody(method) is not { } body) return (default, default);

        var methodName = method.Name;
        var (errorTypeNames, customErrors) = CollectErrorTypes(ctx.SemanticModel, body, context, diagnostics,
            methodName,
            hasExplicitProducesError);
        return (ToSortedErrorArray(errorTypeNames), new EquatableArray<CustomErrorInfo>([.. customErrors]));
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
                diagnostics))
        {
            return;
        }

        // Check for interface/abstract method calls that return ErrorOr
        if (TryDetectUndocumentedInterfaceCall(
                semanticModel,
                child,
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

    private static bool TryHandleErrorFactoryInvocation(
        SemanticModel semanticModel,
        SyntaxNode node,
        ISet<string> errorTypeNames,
        ICollection<CustomErrorInfo> customErrors,
        ISet<string> seenCustomCodes,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        if (!IsErrorFactoryInvocation(semanticModel, node, out var factoryName, out var invocation))
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
