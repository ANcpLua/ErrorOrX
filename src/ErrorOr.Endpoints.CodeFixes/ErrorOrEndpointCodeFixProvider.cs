using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ErrorOr.Endpoints.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ErrorOrEndpointCodeFixProvider))]
public sealed class ErrorOrEndpointCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("EOE025");

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var methodDeclaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
            .OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (methodDeclaration is null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Use expression body",
                c => ConvertToExpressionBodyAsync(context.Document, methodDeclaration, c),
                "UseExpressionBody"),
            diagnostic);
    }

    private static async Task<Document> ConvertToExpressionBodyAsync(Document document,
        MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
    {
        if (methodDeclaration.Body is not { Statements: { Count: 1 } statements } ||
            statements[0] is not ReturnStatementSyntax returnStatement ||
            returnStatement.Expression is null)
            return document;

        var expressionBody = SyntaxFactory.ArrowExpressionClause(returnStatement.Expression);
        var newMethodDeclaration = methodDeclaration
            .WithBody(null)
            .WithExpressionBody(expressionBody)
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            .WithTrailingTrivia(methodDeclaration.GetTrailingTrivia());

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null) return document;

        var newRoot = root.ReplaceNode(methodDeclaration, newMethodDeclaration);
        return document.WithSyntaxRoot(newRoot);
    }
}