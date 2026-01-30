using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
        var (unwrapped, isAsync) = UnwrapAsyncType(returnType, context);

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

        if (context.SuccessMarker is not null &&
            innerType.IsEqualTo(context.SuccessMarker))
        {
            kind = SuccessKind.Success;
        }
        else if (context.CreatedMarker is not null &&
                         innerType.IsEqualTo(context.CreatedMarker))
        {
            kind = SuccessKind.Created;
        }
        else if (context.UpdatedMarker is not null &&
                         innerType.IsEqualTo(context.UpdatedMarker))
        {
            kind = SuccessKind.Updated;
        }
        else if (context.DeletedMarker is not null &&
                         innerType.IsEqualTo(context.DeletedMarker))
        {
            kind = SuccessKind.Deleted;
        }

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
        return new ErrorOrReturnTypeInfo(successTypeFqn, isAsync, false, null, kind, idPropertyName);
    }

    /// <summary>
    ///     Detects a suitable Id property on the success type for Location header generation.
    ///     Looks for properties named Id, ID, id (case-insensitive), preferring exact "Id" match.
    ///     Searches through base types to find inherited Id properties.
    /// </summary>
    private static string? DetectIdProperty(ITypeSymbol type)
    {
        // Skip marker types and primitives
        if (type.SpecialType != SpecialType.None)
            return null;

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
                if (property.Name == "Id")
                    return "Id";

                // Case-insensitive match for fallback
                if (string.Equals(property.Name, "Id", StringComparison.OrdinalIgnoreCase))
                    bestMatch ??= property.Name;
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
            context.IAsyncEnumerableOfT is not null &&
            named.ConstructedFrom.IsEqualTo(context.IAsyncEnumerableOfT))
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
            named.ConstructedFrom.IsEqualTo(context.SseItemOfT))
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

        if ((context.TaskOfT is not null && constructed.IsEqualTo(context.TaskOfT)) ||
            (context.ValueTaskOfT is not null &&
             constructed.IsEqualTo(context.ValueTaskOfT)))
        {
            return (named.TypeArguments[0], true);
        }

        return (type, false);
    }

    private static bool IsErrorOrType(
        ITypeSymbol type,
        ErrorOrContext context,
        [NotNullWhen(true)] out INamedTypeSymbol? errorOrType)
    {
        if (type is INamedTypeSymbol { IsGenericType: true } named &&
            context.ErrorOrOfT is not null &&
            named.ConstructedFrom.IsEqualTo(context.ErrorOrOfT))
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
        if (GetMethodBody(method) is not { } body)
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

        // AL0029: Need to extract constructor arguments, not just check existence
#pragma warning disable AL0029
        foreach (var attr in method.GetAttributes())
        {
            if (context.ProducesErrorAttribute is not null &&
                attr.AttributeClass?.IsEqualTo(context.ProducesErrorAttribute) == true &&
                attr.ConstructorArguments is [{ Value: int statusCode }, ..])
            {
                results.Add(new ProducesErrorInfo(statusCode));
            }
        }
#pragma warning restore AL0029

        return results.Count > 0
            ? new EquatableArray<ProducesErrorInfo>([.. results])
            : default;
    }

    /// <summary>
    ///     Checks if the method has the [AcceptedResponse] attribute for 202 Accepted responses.
    /// </summary>
    private static bool HasAcceptedResponseAttribute(ISymbol method, ErrorOrContext context)
    {
        return context.AcceptedResponseAttribute is { } attr
            ? method.HasAttribute(attr)
            : method.HasAttribute(WellKnownTypes.AcceptedResponseAttribute);
    }

    private static SyntaxNode? GetMethodBody(ISymbol method)
    {
        var refs = method.DeclaringSyntaxReferences;
        if (refs.IsDefaultOrEmpty || refs.Length is 0)
            return null;

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

        if (!TryGetReferencedSymbol(semanticModel, child, visitedSymbols, out var symbol))
            return;

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

        // AL0029: Need to extract constructor arguments, not just check existence
#pragma warning disable AL0029
        foreach (var attr in method.GetAttributes())
        {
            if (attr.AttributeClass?.IsEqualTo(context.ReturnsErrorAttribute) != true) continue;

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
#pragma warning restore AL0029

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
        var (unwrapped, _) = UnwrapAsyncType(method.ReturnType, context);
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
            !symbol.ContainingType.IsEqualTo(context.Error))
        {
            return false;
        }

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
    private static MiddlewareInfo ExtractMiddlewareAttributes(ISymbol method, ErrorOrContext context)
    {
        var auth = default(AuthInfo);
        var rateLimit = default(RateLimitInfo);
        var cache = default(OutputCacheInfo);
        var cors = default(CorsInfo);

        foreach (var attr in method.GetAttributes())
        {
            if (attr.AttributeClass is not { } attrClass)
                continue;

            auth = TryExtractAuth(attr, attrClass, context, auth);
            rateLimit = TryExtractRateLimit(attr, attrClass, context, rateLimit);
            cache = TryExtractOutputCache(attr, attrClass, context, cache);
            cors = TryExtractCors(attr, attrClass, context, cors);
        }

        return new MiddlewareInfo(
            auth.Required, new EquatableArray<string>(auth.Policies), auth.AllowAnonymous,
            rateLimit.Enabled, rateLimit.Policy, rateLimit.Disabled,
            cache.Enabled, cache.Policy, cache.Duration,
            cors.Enabled, cors.Policy, cors.Disabled);
    }

    private static AuthInfo TryExtractAuth(
        AttributeData attr, ISymbol attrClass, ErrorOrContext context, AuthInfo current)
    {
        if (context.AuthorizeAttribute is not null &&
            attrClass.IsEqualTo(context.AuthorizeAttribute))
        {
            var policy = attr.ConstructorArguments is [{ Value: string p }] ? p : null;
            policy ??= attr.NamedArguments.FirstOrDefault(static a => a.Key == "Policy").Value.Value as string;

            // Accumulate policies rather than overwriting
            var policies = policy is not null
                ? current.Policies.IsDefault
                    ? [policy]
                    : current.Policies.Add(policy)
                : current.Policies;

            return current with
            {
                Required = true,
                Policies = policies
            };
        }

        if (context.AllowAnonymousAttribute is not null &&
            attrClass.IsEqualTo(context.AllowAnonymousAttribute))
        {
            return current with
            {
                AllowAnonymous = true
            };
        }

        return current;
    }

    private static RateLimitInfo TryExtractRateLimit(
        AttributeData attr, ISymbol attrClass, ErrorOrContext context, RateLimitInfo current)
    {
        if (context.EnableRateLimitingAttribute is not null &&
            attrClass.IsEqualTo(context.EnableRateLimitingAttribute))
        {
            var policy = attr.ConstructorArguments is [{ Value: string p }] ? p : null;
            return current with
            {
                Enabled = true,
                Policy = policy
            };
        }

        if (context.DisableRateLimitingAttribute is not null &&
            attrClass.IsEqualTo(context.DisableRateLimitingAttribute))
        {
            return current with
            {
                Disabled = true
            };
        }

        return current;
    }

    private static OutputCacheInfo TryExtractOutputCache(
        AttributeData attr, ISymbol attrClass, ErrorOrContext context, OutputCacheInfo current)
    {
        if (context.OutputCacheAttribute is null ||
            !attrClass.IsEqualTo(context.OutputCacheAttribute))
        {
            return current;
        }

        var result = current with
        {
            Enabled = true
        };
        foreach (var namedArg in attr.NamedArguments)
        {
            if (namedArg is { Key: "PolicyName", Value.Value: string policy })
            {
                result = result with
                {
                    Policy = policy
                };
            }

            if (namedArg is { Key: "Duration", Value.Value: int duration })
            {
                result = result with
                {
                    Duration = duration
                };
            }
        }

        return result;
    }

    private static CorsInfo TryExtractCors(
        AttributeData attr, ISymbol attrClass, ErrorOrContext context, CorsInfo current)
    {
        if (context.EnableCorsAttribute is not null &&
            attrClass.IsEqualTo(context.EnableCorsAttribute))
        {
            var policy = attr.ConstructorArguments is [{ Value: string p }] ? p : null;
            return current with
            {
                Enabled = true,
                Policy = policy
            };
        }

        if (context.DisableCorsAttribute is not null &&
            attrClass.IsEqualTo(context.DisableCorsAttribute))
        {
            return current with
            {
                Disabled = true
            };
        }

        return current;
    }

    /// <summary>
    ///     Extracts API versioning configuration from the method and its containing type.
    ///     Looks for [ApiVersion], [MapToApiVersion], and [ApiVersionNeutral] attributes.
    /// </summary>
    private static VersioningInfo ExtractVersioningAttributes(ISymbol method, ErrorOrContext context)
    {
        // If Asp.Versioning is not referenced, return empty
        if (!context.HasApiVersioningSupport)
            return default;

        var supportedVersions = new List<ApiVersionInfo>();
        var mappedVersions = new List<ApiVersionInfo>();
        var isVersionNeutral = false;

        // Extract from containing type first (class-level versioning)
        if (method.ContainingType is { } containingType)
            ExtractVersioningFromSymbol(containingType, context, supportedVersions, ref isVersionNeutral);

        // Extract from method (method-level overrides or additions)
        ExtractVersioningFromSymbol(method, context, supportedVersions, ref isVersionNeutral);

        // Extract [MapToApiVersion] separately (only applies to method)
        ExtractMappedVersions(method, context, mappedVersions);

        return new VersioningInfo(
            supportedVersions.Count > 0
                ? new EquatableArray<ApiVersionInfo>([.. supportedVersions.Distinct()])
                : default,
            mappedVersions.Count > 0
                ? new EquatableArray<ApiVersionInfo>([.. mappedVersions.Distinct()])
                : default,
            isVersionNeutral);
    }

    private static void ExtractVersioningFromSymbol(
        ISymbol symbol,
        ErrorOrContext context,
        ICollection<ApiVersionInfo> supportedVersions,
        ref bool isVersionNeutral)
    {
        // AL0029: Need to extract constructor arguments and check multiple attribute types
#pragma warning disable AL0029
        foreach (var attr in symbol.GetAttributes())
        {
            if (attr.AttributeClass is not { } attrClass)
                continue;

            // Check for [ApiVersionNeutral]
            if (context.ApiVersionNeutralAttribute is not null &&
                attrClass.IsEqualTo(context.ApiVersionNeutralAttribute))
            {
                isVersionNeutral = true;
                continue;
            }

            // Check for [ApiVersion(...)]
            if (context.ApiVersionAttribute is not null &&
                attrClass.IsEqualTo(context.ApiVersionAttribute))
            {
                var versionInfo = ParseApiVersionAttribute(attr);
                if (versionInfo.HasValue)
                    supportedVersions.Add(versionInfo.Value);
            }
        }
#pragma warning restore AL0029
    }

    private static void ExtractMappedVersions(
        ISymbol method,
        ErrorOrContext context,
        ICollection<ApiVersionInfo> mappedVersions)
    {
        // AL0029: Need to extract constructor arguments from attribute data
#pragma warning disable AL0029
        foreach (var attr in method.GetAttributes())
        {
            if (attr.AttributeClass is not { } attrClass)
                continue;

            // Check for [MapToApiVersion(...)]
            if (context.MapToApiVersionAttribute is not null &&
                attrClass.IsEqualTo(context.MapToApiVersionAttribute))
            {
                var versionInfo = ParseApiVersionAttribute(attr);
                if (versionInfo.HasValue)
                    mappedVersions.Add(versionInfo.Value);
            }
        }
#pragma warning restore AL0029
    }

    /// <summary>
    ///     Parses version info from [ApiVersion] or [MapToApiVersion] attribute.
    ///     Supports multiple constructor overloads:
    ///     - ApiVersion(string version) - e.g., "1.0", "2", "1.0-beta"
    ///     - ApiVersion(int majorVersion, int minorVersion)
    ///     - ApiVersion(double version) - e.g., 1.0
    /// </summary>
    private static ApiVersionInfo? ParseApiVersionAttribute(AttributeData attr)
    {
        var args = attr.ConstructorArguments;

        if (args.Length is 0)
            return null;

        // Check for Deprecated named argument
        var isDeprecated = attr.NamedArguments
            .Any(static na => na is
            {
                Key: "Deprecated",
                Value.Value: true
            });

        switch (args)
        {
            // ApiVersion(string version) - most common
            case [{ Value: string versionString }]:
                return ParseVersionString(versionString, isDeprecated);
            // ApiVersion(int majorVersion, int minorVersion)
            case [{ Value: int major }, { Value: int minor }]:
                return new ApiVersionInfo(major, minor, null, isDeprecated);
            // ApiVersion(double version) - e.g., 1.0
            case [{ Value: double doubleVersion }]:
            {
                var majorPart = (int)doubleVersion;
                var minorPart = (int)((doubleVersion - majorPart) * 10);
                return new ApiVersionInfo(majorPart, minorPart > 0 ? minorPart : null, null, isDeprecated);
            }
            default:
                return null;
        }
    }

    /// <summary>
    ///     Parses a version string like "1.0", "2", "1.0-beta" into ApiVersionInfo.
    /// </summary>
    private static ApiVersionInfo? ParseVersionString(string versionString, bool isDeprecated)
    {
        if (string.IsNullOrWhiteSpace(versionString))
            return null;

        // Handle status suffix (e.g., "1.0-beta")
        string? status = null;
        var dashIndex = versionString.IndexOf('-');
        if (dashIndex > 0)
        {
            status = versionString[(dashIndex + 1)..];
            versionString = versionString[..dashIndex];
        }

        // Parse major.minor
        var parts = versionString.Split('.');

        if (!int.TryParse(parts[0], out var major))
            return null;

        int? minor = null;
        if (parts.Length > 1 && int.TryParse(parts[1], out var minorValue))
            minor = minorValue;

        return new ApiVersionInfo(major, minor, status, isDeprecated);
    }

    /// <summary>
    ///     Extracts raw version strings from [ApiVersion] attributes on the containing type.
    ///     Used for format validation (EOE031).
    /// </summary>
    private static ImmutableArray<string> ExtractRawClassVersionStrings(ISymbol method, ErrorOrContext context)
    {
        if (!context.HasApiVersioningSupport || method.ContainingType is not { } containingType)
            return ImmutableArray<string>.Empty;

        var versions = ImmutableArray.CreateBuilder<string>();

        // AL0029: Need to extract constructor arguments
#pragma warning disable AL0029
        foreach (var attr in containingType.GetAttributes())
        {
            if (attr.AttributeClass is not { } attrClass)
                continue;

            if (context.ApiVersionAttribute is not null &&
                attrClass.IsEqualTo(context.ApiVersionAttribute) &&
                attr.ConstructorArguments is [{ Value: string versionString }])
            {
                versions.Add(versionString);
            }
        }
#pragma warning restore AL0029

        return versions.ToImmutable();
    }

    /// <summary>
    ///     Extracts raw version strings from [MapToApiVersion] attributes on the method.
    ///     Used for format validation (EOE031).
    /// </summary>
    private static ImmutableArray<string> ExtractRawMethodVersionStrings(ISymbol method, ErrorOrContext context)
    {
        if (!context.HasApiVersioningSupport)
            return ImmutableArray<string>.Empty;

        var versions = ImmutableArray.CreateBuilder<string>();

        // AL0029: Need to extract constructor arguments
#pragma warning disable AL0029
        foreach (var attr in method.GetAttributes())
        {
            if (attr.AttributeClass is not { } attrClass)
                continue;

            if (context.MapToApiVersionAttribute is not null &&
                attrClass.IsEqualTo(context.MapToApiVersionAttribute) &&
                attr.ConstructorArguments is [{ Value: string versionString }])
            {
                versions.Add(versionString);
            }
        }
#pragma warning restore AL0029

        return versions.ToImmutable();
    }

    /// <summary>
    ///     Extracts route group configuration from the containing type's [RouteGroup] attribute.
    ///     This enables eShop-style route grouping with NewVersionedApi() and MapGroup().
    /// </summary>
    private static RouteGroupInfo ExtractRouteGroupInfo(ISymbol method, ErrorOrContext context)
    {
        // RouteGroup is only applied at class level
        if (method.ContainingType is not { } containingType)
            return default;

        // If RouteGroupAttribute is not yet emitted/available, return default
        if (context.RouteGroupAttribute is null)
            return default;

        // AL0029: Need to extract constructor arguments and named arguments
#pragma warning disable AL0029
        var attrs = containingType.GetAttributes();
        foreach (var attr in attrs)
        {
            if (attr.AttributeClass is not { } attrClass)
                continue;

            if (!attrClass.IsEqualTo(context.RouteGroupAttribute))
                continue;

            // [RouteGroup(string path)]
            // Optional: ApiName named argument
            var args = attr.ConstructorArguments;
            if (args is not [{ Value: string groupPath }])
                continue;

            // Extract optional ApiName from named arguments
            string? apiName = null;
            foreach (var namedArg in attr.NamedArguments)
            {
                if (namedArg is
                    {
                        Key: "ApiName",
                        Value.Value: string name
                    })
                {
                    apiName = name;
                }
            }

            return new RouteGroupInfo(groupPath, apiName);
        }
#pragma warning restore AL0029

        return default;
    }

    /// <summary>
    ///     Extracts metadata from [EndpointMetadata] attributes and [Obsolete] attribute.
    /// </summary>
    private static EquatableArray<MetadataEntry> ExtractMetadata(ISymbol method)
    {
        var metadata = ImmutableArray.CreateBuilder<MetadataEntry>();

        // AL0029: Need to extract constructor arguments and process multiple attribute types
#pragma warning disable AL0029
        foreach (var attr in method.GetAttributes())
        {
            if (attr.AttributeClass is not { } attrClass)
                continue;

            switch (attrClass.Name)
            {
                // [Obsolete] → deprecated metadata
                case "ObsoleteAttribute":
                {
                    metadata.Add(new MetadataEntry(MetadataKeys.Deprecated, "true"));
                    if (attr.ConstructorArguments is [{ Value: string msg }, ..])
                        metadata.Add(new MetadataEntry(MetadataKeys.DeprecatedMessage, msg));
                    continue;
                }
                // [EndpointMetadata(key, value)]
                case "EndpointMetadataAttribute" when
                    attr.ConstructorArguments is [{ Value: string key }, { Value: string value }]:
                    metadata.Add(new MetadataEntry(key, value));
                    break;
            }
        }
#pragma warning restore AL0029

        return metadata.Count > 0
            ? new EquatableArray<MetadataEntry>(metadata.ToImmutable())
            : default;
    }

    // Helper records for middleware extraction
    private readonly record struct AuthInfo(bool Required, ImmutableArray<string> Policies, bool AllowAnonymous);

    private readonly record struct RateLimitInfo(bool Enabled, string? Policy, bool Disabled);

    private readonly record struct OutputCacheInfo(bool Enabled, string? Policy, int? Duration);

    private readonly record struct CorsInfo(bool Enabled, string? Policy, bool Disabled);
}
