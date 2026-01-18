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
    ///     Reflection methods on Type that are not AOT-safe.
    /// </summary>
    private static readonly ImmutableHashSet<string> ReflectionMethods = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "GetProperties",
        "GetProperty",
        "GetMethods",
        "GetMethod",
        "GetFields",
        "GetField",
        "GetMembers",
        "GetMember",
        "GetEvents",
        "GetEvent",
        "GetConstructors",
        "GetConstructor",
        "InvokeMember",
        "GetCustomAttributes",
        "GetCustomAttribute");

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

        // Check for Type.GetType(string)
        if (IsTypeGetType(method))
        {
            var typeString = GetFirstStringArgument(invocation);
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.TypeGetType,
                invocation.GetLocation(),
                typeString ?? "..."));
            return;
        }

        // Check for reflection methods on Type
        if (IsReflectionMethod(method))
        {
            var typeName = GetReceiverTypeName(invocation, context.SemanticModel);
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.ReflectionOverMembers,
                invocation.GetLocation(),
                typeName ?? "T",
                method.Name));
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

    private static bool IsActivatorCreateInstance(IMethodSymbol method)
    {
        return method.ContainingType?.ToDisplayString() == "System.Activator" &&
               method.Name == "CreateInstance";
    }

    private static bool IsTypeGetType(IMethodSymbol method)
    {
        return IsSystemType(method.ContainingType) &&
               method.Name == "GetType" &&
               method.Parameters.Length > 0 &&
               method.Parameters[0].Type.SpecialType == SpecialType.System_String;
    }

    private static bool IsReflectionMethod(IMethodSymbol method)
    {
        if (!IsSystemType(method.ContainingType))
            return false;

        return ReflectionMethods.Contains(method.Name);
    }

    private static bool IsSystemType(INamedTypeSymbol? type)
    {
        return type?.ToDisplayString() == "System.Type";
    }

    private static bool IsExpressionCompile(IMethodSymbol method)
    {
        if (method.Name != "Compile")
            return false;

        var containingType = method.ContainingType;
        if (containingType is null)
            return false;

        // Check if it's on LambdaExpression or Expression<TDelegate>
        var typeName = containingType.ToDisplayString();
        return typeName.StartsWith("System.Linq.Expressions.Expression", StringComparison.Ordinal) ||
               typeName == "System.Linq.Expressions.LambdaExpression";
    }

    private static string GetTypeArgument(IMethodSymbol method, InvocationExpressionSyntax invocation)
    {
        // Generic type argument: Activator.CreateInstance<T>()
        if (method.IsGenericMethod && method.TypeArguments.Length > 0)
            return method.TypeArguments[0].ToDisplayString();

        // Runtime type argument: Activator.CreateInstance(typeof(T))
        if (invocation.ArgumentList.Arguments.Count > 0)
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
        if (firstArg is LiteralExpressionSyntax { Token.Value: string value })
            return value;

        return firstArg.ToString();
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
        if (receiverType?.ToDisplayString() == "System.Type")
        {
            // It's a Type variable, try to find what it represents
            return "T";
        }

        return receiverType?.ToDisplayString();
    }
}
