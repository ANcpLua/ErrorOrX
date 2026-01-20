using System.Net;
using System.Net.Http.Json;

namespace ErrorOrX.Integration.Tests;

public sealed class MinimalApiParityTests : IntegrationTestBase
{
    public MinimalApiParityTests(IntegrationTestAppFactory factory)
        : base(factory)
    { }

    [Fact]
    public async Task Missing_Required_Query_Returns_400()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await Client.GetAsync("/parity/query-required", ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>(ct);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(400);
    }

    [Fact]
    public async Task Route_Mismatch_Returns_404()
    {
        var response = await Client.GetAsync("/parity/non-existent-route", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Query_Parse_Failure_Returns_400()
    {
        var response = await Client.GetAsync("/parity/query-int?id=bad", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Body_Wrong_ContentType_Returns_415()
    {
        using var content = new StringContent("not-json", System.Text.Encoding.UTF8, "text/plain");
        var response = await Client.PostAsync("/parity/body-json", content, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task Form_Wrong_ContentType_Returns_415()
    {
        using var content = new StringContent("not-form", System.Text.Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/parity/form", content, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task Returning_T_From_GET_Returns_200()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await Client.GetAsync("/parity/return-t", ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync(ct);
        content.Should().Be("\"success\"");
    }

    [Fact]
    public async Task Returning_T_From_POST_Returns_200()
    {
        var response = await Client.PostAsync("/parity/return-t-post", null, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Explicit_Created_TypedResult_Returns_201()
    {
        var response = await Client.PostAsync("/parity/created", null, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Cookie_Auth_Known_API_Endpoint_Returns_401_No_Redirect()
    {
        var response = await Client.GetAsync("/parity/auth/protected", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.RequestMessage!.RequestUri!.AbsolutePath.Should().Be("/parity/auth/protected");
    }
}
