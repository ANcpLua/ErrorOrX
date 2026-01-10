using ErrorOr.Core.Errors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace ErrorOr.Endpoints.Tests;

public abstract class CodeFixTestBase<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    protected static Task VerifyCodeFixAsync(string source, string fixedSource)
    {
        var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, XUnitV3Verifier>
        {
            TestCode = source,
            FixedCode = fixedSource,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            MarkupOptions = MarkupOptions.UseFirstDescriptor
        };

        // Add ErrorOr.Core reference
        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(Error).Assembly.Location));

        return test.RunAsync();
    }
}
