using ANcpLua.Roslyn.Utilities.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using TypedResults = Microsoft.AspNetCore.Http.TypedResults;

namespace ErrorOrX.Generators.Tests;

public abstract class GeneratorTestBase
{
    protected static readonly Type[] RequiredTypes =
    [
        typeof(HttpContext),
        typeof(TypedResults),
        typeof(FromBodyAttribute),
        typeof(IServiceCollection),
        typeof(OpenApiServiceCollectionExtensions),
        typeof(Error)
    ];

    protected static async Task VerifyGeneratorAsync(string source)
    {
        using var scope = TestConfiguration.WithAdditionalReferences(RequiredTypes);
        using var result = await Test<ErrorOrEndpointGenerator>.Run(source);

        await Verify(new
        {
            GeneratedSources = result.Files
                .Select(static f => new { f.HintName, Source = f.Content })
                .OrderBy(static s => s.HintName),
            Diagnostics = result.Diagnostics
                .Select(static d => new { d.Id, Severity = d.Severity.ToString(), Message = d.GetMessage() })
                .OrderBy(static d => d.Id)
        }).UseDirectory("Snapshots");
    }

    protected static async Task VerifyGeneratorAsync(string source, params IIncrementalGenerator[] generators)
    {
        using var scope = TestConfiguration.WithAdditionalReferences(RequiredTypes);

        var engine = new GeneratorTestEngine<ErrorOrEndpointGenerator>();
        var compilation = await engine.CreateCompilationAsync(source);

        var parseOptions = new CSharpParseOptions(TestConfiguration.LanguageVersion);
        var sourceGenerators = generators.Select(static g => g.AsSourceGenerator());
        GeneratorDriver driver = CSharpGeneratorDriver.Create(sourceGenerators, parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        var runResult = driver.GetRunResult();

        await Verify(new
        {
            GeneratedSources = runResult.Results
                .SelectMany(static r => r.GeneratedSources)
                .Select(static s => new { s.HintName, Source = s.SourceText.ToString() })
                .OrderBy(static s => s.HintName),
            Diagnostics = runResult.Results
                .SelectMany(static r => r.Diagnostics)
                .Select(static d => new { d.Id, Severity = d.Severity.ToString(), Message = d.GetMessage() })
                .OrderBy(static d => d.Id)
        }).UseDirectory("Snapshots");
    }
}
