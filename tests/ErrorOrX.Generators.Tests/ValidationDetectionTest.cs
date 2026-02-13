namespace ErrorOrX.Generators.Tests;

public class ValidationDetectionTest : GeneratorTestBase
{
    [Fact]
    public async Task Class_With_Required_Property_Should_Detect_Validation()
    {
        const string Source = """
                              using ErrorOr;
                              using System.ComponentModel.DataAnnotations;
                              using Microsoft.AspNetCore.Http;
                              using Microsoft.AspNetCore.Mvc;

                              public class CreateRequest
                              {
                                  [Required]
                                  [StringLength(100)]
                                  public string Name { get; set; } = "";

                                  [EmailAddress]
                                  public string Email { get; set; } = "";
                              }
                              public record Response(int Id);

                              public static class Api
                              {
                                  [Post("/test")]
                                  public static ErrorOr<Response> Handler(CreateRequest req) => new Response(1);
                              }
                              """;

        using var result = await RunAsync(Source);

        var mappingsFile = result.Files.FirstOrDefault(static f => f.HintName == "ErrorOrEndpointMappings.cs");
        mappingsFile.Should().NotBeNull();
        mappingsFile.Content.Should().Contain("BCL Validation");
    }

    [Fact]
    public async Task Record_With_Validation_Attributes_On_Parameters_Should_Detect_Validation()
    {
        const string Source = """
                              using ErrorOr;
                              using System.ComponentModel.DataAnnotations;
                              using Microsoft.AspNetCore.Http;
                              using Microsoft.AspNetCore.Mvc;

                              public record CreateRequest([Required] [StringLength(100)] string Name, [EmailAddress] string Email);
                              public record Response(int Id);

                              public static class Api
                              {
                                  [Post("/test")]
                                  public static ErrorOr<Response> Handler(CreateRequest req) => new Response(1);
                              }
                              """;

        using var result = await RunAsync(Source);

        var mappingsFile = result.Files.FirstOrDefault(static f => f.HintName == "ErrorOrEndpointMappings.cs");
        mappingsFile.Should().NotBeNull();
        mappingsFile.Content.Should().Contain("BCL Validation");
    }
}
