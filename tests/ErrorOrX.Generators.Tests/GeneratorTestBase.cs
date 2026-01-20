using ANcpLua.Roslyn.Utilities.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ErrorOrX.Generators.Tests;

public abstract class GeneratorTestBase
{
    protected static readonly Type[] RequiredTypes =
    [
        typeof(HttpContext),
        typeof(TypedResults),
        typeof(FromBodyAttribute),
        typeof(IEndpointNameMetadata),
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
                .Select(static f => new
                {
                    f.HintName, Source = f.Content
                })
                .OrderBy(static s => s.HintName),
            Diagnostics = result.Diagnostics
                .Select(static d => new
                {
                    d.Id, Severity = d.Severity.ToString(), Message = d.GetMessage()
                })
                .OrderBy(static d => d.Id)
        }).UseDirectory("Snapshots");
    }
}
