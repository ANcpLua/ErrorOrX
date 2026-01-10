using System.Text.Json;
using ErrorOr.Core.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace ErrorOr.Endpoints.Tests;

public abstract class GeneratorTestBase
{
    static GeneratorTestBase()
    {
        // Force load some assemblies to ensure they are in the AppDomain
        var types = new[]
        {
            typeof(object), typeof(JsonSerializer), typeof(HttpContext), typeof(TypedResults),
            typeof(IQueryCollection), typeof(JsonOptions), typeof(IFeatureCollection), typeof(FromBodyAttribute),
            typeof(IServiceCollection), typeof(OpenApiServiceCollectionExtensions),
            typeof(RoutingEndpointConventionBuilderExtensions), typeof(OpenApiOptions), typeof(Error)
        };

        foreach (var type in types)
        {
            _ = type.Assembly;
        }
    }

    protected static Compilation CreateCompilation(string source) =>
        CSharpCompilation.Create(
            "test",
            new[] { CSharpSyntaxTree.ParseText(source) },
            GetMetadataReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    protected static IEnumerable<MetadataReference> GetMetadataReferences() =>
        // Get all loaded assemblies to ensure we have ASP.NET Core and ErrorOr.Core
        AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location));

    protected static async Task VerifyGeneratorAsync(Compilation compilation, params IIncrementalGenerator[] generators)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generators);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var runResult = driver.GetRunResult();

        // Extract just the generated sources for verification (avoids ImmutableArray serialization issues)
        var generatedSources = runResult.Results
            .SelectMany(r => r.GeneratedSources)
            .Select(s => new
            {
                HintName = s.HintName,
                Source = s.SourceText.ToString()
            })
            .OrderBy(s => s.HintName)
            .ToArray();

        // Also include any diagnostics
        var allDiagnostics = runResult.Results
            .SelectMany(r => r.Diagnostics)
            .Select(d => new
            {
                Id = d.Id,
                Severity = d.Severity.ToString(),
                Message = d.GetMessage()
            })
            .OrderBy(d => d.Id)
            .ToArray();

        var result = new
        {
            GeneratedSources = generatedSources,
            Diagnostics = allDiagnostics
        };

        await Verify(result)
            .UseDirectory("Snapshots");
    }
}
