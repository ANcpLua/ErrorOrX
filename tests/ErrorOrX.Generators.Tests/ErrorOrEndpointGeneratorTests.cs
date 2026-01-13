using System.Reflection;

namespace ErrorOrX.Generators.Tests;

public class ErrorOrEndpointGeneratorTests : GeneratorTestBase
{
    [Fact]
    public Task Generates_Simple_Get_Endpoint()
    {
        const string Source = """
                              using ErrorOr.Core.ErrorOr;
                              using ErrorOr.Endpoints;

                              namespace MyNamespace;

                              public static class MyEndpoints
                              {
                                  [Get("/test/{id}")]
                                  public static ErrorOr<string> GetUser(int id) => "user_" + id;
                              }
                              """;

        return VerifyGeneratorAsync(Source, new ErrorOrEndpointGenerator(), new OpenApiTransformerGenerator());
    }

    [Fact]
    public void ErrorType_Failure_Maps_To_500_InternalServerError()
    {
        // RFC 9110 §15.6.1: ErrorType.Failure MUST map to 500 Internal Server Error
        var assembly = typeof(ErrorOrEndpointGenerator).Assembly;
        var errorMappingType = assembly.GetType("ErrorOr.Generators.ErrorMapping", true)!;

        var method = errorMappingType.GetMethod(
            "Get",
            BindingFlags.Public | BindingFlags.Static,
            null,
            [typeof(string)],
            null);

        // This test verifies the critical RFC 9110 compliance fix
        // ErrorType.Failure = server error = 500, NOT 422
        var result = method!.Invoke(null, ["Failure"])!;

        var status = GetProperty<int>(result, "StatusCode");
        var typeFqn = GetProperty<string>(result, "TypeFqn");

        status.Should().Be(500, "ErrorType.Failure must map to 500 per RFC 9110 §15.6.1");
        typeFqn.Should().Contain("InternalServerError", "ErrorType.Failure must use InternalServerError result type");
        typeFqn.Should().NotContain("UnprocessableEntity",
            "422 is for semantic validation errors, not server failures");
    }

    [Fact]
    public void ErrorType_Unexpected_Maps_To_500_InternalServerError()
    {
        var assembly = typeof(ErrorOrEndpointGenerator).Assembly;
        var errorMappingType = assembly.GetType("ErrorOr.Generators.ErrorMapping", true)!;

        var method = errorMappingType.GetMethod(
            "Get",
            BindingFlags.Public | BindingFlags.Static,
            null,
            [typeof(string)],
            null);

        var result = method!.Invoke(null, ["Unexpected"])!;

        var status = GetProperty<int>(result, "StatusCode");
        status.Should().Be(500, "ErrorType.Unexpected must map to 500 per RFC 9110 §15.6.1");
    }

    [Theory]
    [InlineData("Validation", 400)]
    [InlineData("Unauthorized", 401)]
    [InlineData("Forbidden", 403)]
    [InlineData("NotFound", 404)]
    [InlineData("Conflict", 409)]
    [InlineData("Failure", 500)]
    [InlineData("Unexpected", 500)]
    public void ErrorType_Maps_To_Correct_HttpStatus_Per_RFC9110(string errorTypeName, int expectedStatus)
    {
        var assembly = typeof(ErrorOrEndpointGenerator).Assembly;
        var errorMappingType = assembly.GetType("ErrorOr.Generators.ErrorMapping", true)!;

        var method = errorMappingType.GetMethod(
            "Get",
            BindingFlags.Public | BindingFlags.Static,
            null,
            [typeof(string)],
            null);

        var result = method!.Invoke(null, [errorTypeName])!;

        var status = GetProperty<int>(result, "StatusCode");
        status.Should().Be(expectedStatus, $"ErrorType.{errorTypeName} must map to {expectedStatus}");
    }

    [Fact]
    public void Generates_Result_Markers_With_Distinct_Statuses()
    {
        var assembly = typeof(ErrorOrEndpointGenerator).Assembly;
        var builderType = assembly.GetType("ErrorOr.Generators.ResultsUnionTypeBuilder", true);
        var successKindType = assembly.GetType("ErrorOr.Generators.SuccessKind", true)!;

        var method = builderType!.GetMethod(
            "GetSuccessResponseInfo",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();

        // Get SuccessKind enum values
        var successKind = Enum.Parse(successKindType, "Success");
        var createdKind = Enum.Parse(successKindType, "Created");
        var updatedKind = Enum.Parse(successKindType, "Updated");
        var deletedKind = Enum.Parse(successKindType, "Deleted");
        var payloadKind = Enum.Parse(successKindType, "Payload");

        // Test Success marker -> 200 OK (no body)
        var successInfo = InvokeSuccessInfo(method!, "global::ErrorOr.Core.Results.Success", successKind, "GET");
        successInfo.ResultTypeFqn.Should().Be("global::Microsoft.AspNetCore.Http.HttpResults.Ok");
        successInfo.StatusCode.Should().Be(200);
        successInfo.HasBody.Should().BeFalse();

        // Test Created marker -> 201 Created (no body)
        var createdInfo = InvokeSuccessInfo(method!, "global::ErrorOr.Core.Results.Created", createdKind, "POST");
        createdInfo.ResultTypeFqn.Should().Be("global::Microsoft.AspNetCore.Http.HttpResults.Created");
        createdInfo.StatusCode.Should().Be(201);
        createdInfo.HasBody.Should().BeFalse();

        // Test Updated marker -> 204 No Content (no body)
        var updatedInfo = InvokeSuccessInfo(method!, "global::ErrorOr.Core.Results.Updated", updatedKind, "PUT");
        updatedInfo.ResultTypeFqn.Should().Be("global::Microsoft.AspNetCore.Http.HttpResults.NoContent");
        updatedInfo.StatusCode.Should().Be(204);
        updatedInfo.HasBody.Should().BeFalse();

        // Test Deleted marker -> 204 No Content (no body)
        var deletedInfo = InvokeSuccessInfo(method!, "global::ErrorOr.Core.Results.Deleted", deletedKind, "DELETE");
        deletedInfo.ResultTypeFqn.Should().Be("global::Microsoft.AspNetCore.Http.HttpResults.NoContent");
        deletedInfo.StatusCode.Should().Be(204);
        deletedInfo.HasBody.Should().BeFalse();

        // Test user type with "Created" suffix -> should NOT be treated as marker
        var fooCreatedInfo = InvokeSuccessInfo(method!, "global::MyNamespace.FooCreated", payloadKind, "GET");
        fooCreatedInfo.ResultTypeFqn.Should()
            .Be("global::Microsoft.AspNetCore.Http.HttpResults.Ok<global::MyNamespace.FooCreated>");
        fooCreatedInfo.StatusCode.Should().Be(200);
        fooCreatedInfo.HasBody.Should().BeTrue();

        // Test user type with "Updated" suffix -> should NOT be treated as marker
        var myUpdatedInfo = InvokeSuccessInfo(method!, "global::MyNamespace.MyUpdated", payloadKind, "PUT");
        myUpdatedInfo.ResultTypeFqn.Should()
            .Be("global::Microsoft.AspNetCore.Http.HttpResults.Ok<global::MyNamespace.MyUpdated>");
        myUpdatedInfo.StatusCode.Should().Be(200);
        myUpdatedInfo.HasBody.Should().BeTrue();
    }

    private static SuccessInfo InvokeSuccessInfo(MethodBase method, string typeFqn, object successKind,
        string httpMethod)
    {
        var info = method.Invoke(null, [typeFqn, successKind, httpMethod, false])!;
        return new SuccessInfo(
            GetProperty<string>(info, "ResultTypeFqn"),
            GetProperty<int>(info, "StatusCode"),
            GetProperty<bool>(info, "HasBody"));
    }

    private static T GetProperty<T>(object instance, string name)
    {
        var value = instance.GetType().GetProperty(name)!.GetValue(instance);
        return (T)value!;
    }

    private readonly record struct SuccessInfo(
        string ResultTypeFqn,
        int StatusCode,
        bool HasBody);
}