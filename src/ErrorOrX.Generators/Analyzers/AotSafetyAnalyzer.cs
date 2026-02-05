using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ErrorOr.Analyzers;

/// <summary>
///     Roslyn analyzer that detects AOT-unsafe patterns in the codebase.
///     Warns about reflection, dynamic, and runtime code generation that
///     are not compatible with NativeAOT compilation.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AotSafetyAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    ///     Reflection methods on Type that are not AOT-safe, mapped to their DynamicallyAccessedMemberTypes.
    /// </summary>
    private static readonly ImmutableDictionary<string, string> ReflectionMethodsToMemberTypes =
        ImmutableDictionary.CreateRange<string, string>(StringComparer.Ordinal,
        [
            new KeyValuePair<string, string>("GetProperties", "DynamicallyAccessedMemberTypes.PublicProperties"),
            new KeyValuePair<string, string>("GetProperty", "DynamicallyAccessedMemberTypes.PublicProperties"),
            new KeyValuePair<string, string>("GetMethods", "DynamicallyAccessedMemberTypes.PublicMethods"),
            new KeyValuePair<string, string>("GetMethod", "DynamicallyAccessedMemberTypes.PublicMethods"),
            new KeyValuePair<string, string>("GetFields", "DynamicallyAccessedMemberTypes.PublicFields"),
            new KeyValuePair<string, string>("GetField", "DynamicallyAccessedMemberTypes.PublicFields"),
            new KeyValuePair<string, string>("GetMembers", "DynamicallyAccessedMemberTypes.All"),
            new KeyValuePair<string, string>("GetMember", "DynamicallyAccessedMemberTypes.All"),
            new KeyValuePair<string, string>("GetEvents", "DynamicallyAccessedMemberTypes.PublicEvents"),
            new KeyValuePair<string, string>("GetEvent", "DynamicallyAccessedMemberTypes.PublicEvents"),
            new KeyValuePair<string, string>("GetConstructors", "DynamicallyAccessedMemberTypes.PublicConstructors"),
            new KeyValuePair<string, string>("GetConstructor", "DynamicallyAccessedMemberTypes.PublicConstructors"),
            new KeyValuePair<string, string>("InvokeMember", "DynamicallyAccessedMemberTypes.All"),
            new KeyValuePair<string, string>("GetCustomAttributes", "DynamicallyAccessedMemberTypes.All"),
            new KeyValuePair<string, string>("GetCustomAttribute", "DynamicallyAccessedMemberTypes.All")
        ]);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        Descriptors.ActivatorCreateInstance,
        Descriptors.TypeGetType,
        Descriptors.ReflectionOverMembers,
        Descriptors.ExpressionCompile,
        Descriptors.DynamicKeyword
    ];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Analyze invocation expressions for method calls
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);

        // Analyze identifier names for 'dynamic' keyword
        context.RegisterSyntaxNodeAction(AnalyzeDynamic, SyntaxKind.IdentifierName);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation)
            return;

        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol method)
            return;

        // Check for Activator.CreateInstance
        if (IsActivatorCreateInstance(method))
        {
            var typeName = GetTypeArgument(method, invocation);
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.ActivatorCreateInstance,
                invocation.GetLocation(),
                typeName));
            return;
        }

        // Check for Type.GetType(string) - only warn if dynamic or case-insensitive
        if (IsTypeGetType(method) && ShouldWarnForTypeGetType(invocation, out var warningReason))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.TypeGetType,
                invocation.GetLocation(),
                warningReason));
            return;
        }

        // Check for reflection methods on Type
        if (IsSystemType(method.ContainingType) &&
            TryGetReflectionMemberType(method.Name, out var memberType))
        {
            var typeName = GetReceiverTypeName(invocation, context.SemanticModel);
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.ReflectionOverMembers,
                invocation.GetLocation(),
                typeName ?? "T",
                method.Name,
                memberType));
            return;
        }

        // Check for Expression.Compile()
        if (IsExpressionCompile(method))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.ExpressionCompile,
                invocation.GetLocation()));
        }
    }

    private static void AnalyzeDynamic(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not IdentifierNameSyntax identifier)
            return;

        // Check if this is the 'dynamic' keyword used as a type
        if (identifier.Identifier.Text != "dynamic")
            return;

        var typeInfo = context.SemanticModel.GetTypeInfo(identifier, context.CancellationToken);
        if (typeInfo.Type is IDynamicTypeSymbol)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.DynamicKeyword,
                identifier.GetLocation()));
        }
    }

    private static bool IsActivatorCreateInstance(ISymbol method)
    {
        return method.ContainingType?.ToDisplayString() == "System.Activator" &&
               method.Name == "CreateInstance";
    }

    private static bool IsTypeGetType(IMethodSymbol method)
    {
        return IsSystemType(method.ContainingType) &&
               method is
               {
                   Name: "GetType",
                   Parameters.Length: > 0
               } &&
               method.Parameters[0].Type.SpecialType == SpecialType.System_String;
    }

    /// <summary>
    ///     Determines if Type.GetType should trigger a warning.
    ///     Per Microsoft docs, Type.GetType is safe when:
    ///     - The first argument is a string literal (analyzable at compile-time)
    ///     - Case-insensitive search is NOT requested (ignoreCase parameter is not true)
    /// </summary>
    private static bool ShouldWarnForTypeGetType(InvocationExpressionSyntax invocation, out string warningReason)
    {
        warningReason = string.Empty;
        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count is 0)
            return false;

        var firstArg = arguments[0].Expression;

        // Check if the first argument is a string literal - if not, it's dynamic and should warn
        var isStringLiteral = firstArg is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression };
        if (!isStringLiteral)
        {
            warningReason = "a dynamic type name";
            return true;
        }

        // Check for case-insensitive overloads:
        // - Type.GetType(string, bool throwOnError, bool ignoreCase)
        // The ignoreCase parameter is the 3rd parameter (index 2)
        if (arguments.Count >= 3)
        {
            var ignoreCaseArg = arguments[2].Expression;
            // If ignoreCase is explicitly true, warn
            if (ignoreCaseArg is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.TrueLiteralExpression })
            {
                warningReason = "case-insensitive search (ignoreCase: true)";
                return true;
            }

            // If ignoreCase is a variable (not a literal false), be conservative and warn
            if (ignoreCaseArg is not LiteralExpressionSyntax { RawKind: (int)SyntaxKind.FalseLiteralExpression })
            {
                warningReason = "a potentially case-insensitive search";
                return true;
            }
        }

        // String literal without case-insensitive search is safe
        return false;
    }

    private static bool TryGetReflectionMemberType(string methodName, out string memberType)
    {
        return ReflectionMethodsToMemberTypes.TryGetValue(methodName, out memberType!);
    }

    private static bool IsSystemType(ISymbol? type)
    {
        return type?.ToDisplayString() == "System.Type";
    }

    private static bool IsExpressionCompile(ISymbol method)
    {
        if (method.Name != "Compile")
            return false;

        if (method.ContainingType is not { } containingType)
            return false;

        // Check if it's on LambdaExpression or Expression<TDelegate>
        var typeName = containingType.ToDisplayString();
        return typeName.StartsWithOrdinal("System.Linq.Expressions.Expression") ||
               typeName == "System.Linq.Expressions.LambdaExpression";
    }

    private static string GetTypeArgument(IMethodSymbol method, InvocationExpressionSyntax invocation)
    {
        // Generic type argument: Activator.CreateInstance<T>()
        if (method is
            {
                IsGenericMethod: true,
                TypeArguments.Length: > 0
            })
        {
            return method.TypeArguments[0].ToDisplayString();
        }

        // Runtime type argument: Activator.CreateInstance(typeof(T))
        if (invocation.ArgumentList.Arguments.Any())
        {
            var firstArg = invocation.ArgumentList.Arguments[0].Expression;
            if (firstArg is TypeOfExpressionSyntax typeOf)
                return typeOf.Type.ToString();
        }

        return "T";
    }

    private static string? GetFirstStringArgument(InvocationExpressionSyntax invocation)
    {
        if (invocation.ArgumentList.Arguments.Count is 0)
            return null;

        var firstArg = invocation.ArgumentList.Arguments[0].Expression;
        return firstArg is LiteralExpressionSyntax { Token.Value: string value } ? value : firstArg.ToString();
    }

    private static string? GetReceiverTypeName(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return null;

        // typeof(T).GetProperties() - extract T
        if (memberAccess.Expression is TypeOfExpressionSyntax typeOf)
            return typeOf.Type.ToString();

        // variable.GetType().GetProperties() - get the type from semantic model
        var receiverType = semanticModel.GetTypeInfo(memberAccess.Expression).Type;
        return receiverType?.ToDisplayString() == "System.Type"
            ?
            // It's a Type variable, try to find what it represents
            "T"
            : receiverType?.ToDisplayString();
    }
}
