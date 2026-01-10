using ErrorOr.Core.Errors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace ErrorOr.Endpoints.Tests;

public abstract class AnalyzerTestBase<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    protected static Task VerifyAnalyzerAsync(string source)
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, XUnitV3Verifier>
        {
            TestCode = source, ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        // Add ErrorOr.Core reference
        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(Error).Assembly.Location));

        // Add Microsoft.AspNetCore.Http metadata reference if possible
        // Since we are running on net10.0, we might have it in the app domain or we can rely on ReferenceAssemblies.Net.Net80
        // Minimal APIs are part of the ASP.NET Core framework.

        return test.RunAsync();
    }
}
