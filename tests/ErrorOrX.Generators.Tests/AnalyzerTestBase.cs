using ANcpLua.Roslyn.Utilities.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace ErrorOrX.Generators.Tests;

public abstract class AnalyzerTestBase<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    protected static Task VerifyAnalyzerAsync(string source)
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, XUnitV3Verifier>
        {
            TestCode = source,
            ReferenceAssemblies = TestConfiguration.ReferenceAssemblies
        };

        test.TestState.AdditionalReferences.Add(
            MetadataReference.CreateFromFile(typeof(Error).Assembly.Location));

        return test.RunAsync();
    }
}
