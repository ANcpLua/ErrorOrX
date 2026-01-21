using ANcpLua.Roslyn.Utilities.Testing;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ErrorOrX.Generators.Tests;

/// <summary>
///     Base class for analyzer tests, inheriting from the ANcpLua testing utilities.
/// </summary>
public abstract class AnalyzerTestBase<TAnalyzer> : AnalyzerTest<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new();
