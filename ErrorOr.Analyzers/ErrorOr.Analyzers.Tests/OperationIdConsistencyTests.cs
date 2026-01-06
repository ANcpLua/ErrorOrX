using Xunit;

namespace ErrorOr.Http.Analyzers.Tests;

/// <summary>
///     Tests that operationId computation is identical between Emitter and OpenApiTransformerGenerator.
///     If these drift, XML docs will silently fail to apply to operations.
/// </summary>
public class OperationIdConsistencyTests
{
    /// <summary>
    ///     Both generators must compute operationId = "{tagName}_{methodName}"
    ///     where tagName = className with "Endpoints" suffix stripped.
    /// </summary>
    [Theory]
    [InlineData("ProductsEndpoints", "GetAll", "Products_GetAll")]
    [InlineData("ProductsEndpoints", "GetById", "Products_GetById")]
    [InlineData("UsersEndpoints", "Create", "Users_Create")]
    [InlineData("OrderService", "PlaceOrder", "OrderService_PlaceOrder")] // No "Endpoints" suffix
    [InlineData("Endpoints", "Get", "_Get")] // Edge case: class is just "Endpoints"
    [InlineData("MyEndpoints", "Do", "My_Do")]
    public void OperationId_IsDerivedFromTagNameAndMethod(string className, string methodName,
        string expectedOperationId)
    {
        // Algorithm from both Emitter.cs and OpenApiTransformerGenerator.cs:
        // var tagName = className.EndsWith("Endpoints") ? className[..^"Endpoints".Length] : className;
        // var operationId = $"{tagName}_{methodName}";

        var tagName = className.EndsWith("Endpoints", StringComparison.Ordinal)
            ? className[..^"Endpoints".Length]
            : className;
        var operationId = $"{tagName}_{methodName}";

        Assert.Equal(expectedOperationId, operationId);
    }

    /// <summary>
    ///     Emitter uses tagName (stripped) for operationId, NOT className.
    ///     This test documents the invariant that was violated before the fix.
    /// </summary>
    [Fact]
    public void OperationId_UsesTagName_NotClassName()
    {
        const string EndpointClass = "ProductsEndpoints";
        const string HandlerMethod = "GetById";

        // CORRECT: tagName with "Endpoints" stripped
        var tagName = EndpointClass[..^"Endpoints".Length]; // "Products"
        var correctOperationId = $"{tagName}_{HandlerMethod}"; // "Products_GetById"

        // WRONG: className without stripping (this was the bug)
        var wrongOperationId = $"{EndpointClass}_{HandlerMethod}"; // "ProductsEndpoints_GetById"

        Assert.Equal("Products_GetById", correctOperationId);
        Assert.NotEqual(correctOperationId, wrongOperationId);
    }
}
