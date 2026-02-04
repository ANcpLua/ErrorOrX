// Centralized JsonSerializerContext for all demo endpoints
// This satisfies EOE007 (Type not AOT-serializable) for all request/response types

using DiagnosticsDemos.Demos;
using DiagnosticsDemos.Demos.Eoe025;
using DiagnosticsDemos.Demos.Eoe026;

namespace DiagnosticsDemos;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
// Basic types
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(List<string>))]
// EOE006 types
[JsonSerializable(typeof(CreateRequest))]
// EOE008 types
[JsonSerializable(typeof(SearchCriteria))]
// EOE009 types
[JsonSerializable(typeof(EOE009_AcceptedOnReadOnlyMethod.ImportRequest))]
[JsonSerializable(typeof(EOE009_AcceptedOnReadOnlyMethod.BatchUpdateRequest))]
// EOE015 types
[JsonSerializable(typeof(TodoSummary))]
[JsonSerializable(typeof(EOE015_AnonymousReturnType.StatusResponse))]
[JsonSerializable(typeof(EOE015_AnonymousReturnType.PagedResult<TodoSummary>))]
[JsonSerializable(typeof(EOE015_AnonymousReturnType.ApiResponse<TodoSummary>))]
[JsonSerializable(typeof(List<TodoSummary>))]
// EOE018 types
[JsonSerializable(typeof(PublicData))]
[JsonSerializable(typeof(PublicRequest))]
// EOE019 types
[JsonSerializable(typeof(Item019))]
[JsonSerializable(typeof(User019))]
[JsonSerializable(typeof(Product019))]
[JsonSerializable(typeof(PagedResult019<Item019>))]
[JsonSerializable(typeof(List<Item019>))]
[JsonSerializable(typeof(List<User019>))]
[JsonSerializable(typeof(List<Product019>))]
[JsonSerializable(typeof(Dictionary<string, Item019>))]
// EOE021 types
[JsonSerializable(typeof(SearchFilter))]
// EOE022 types
[JsonSerializable(typeof(EOE022_TooManyResultTypes.CreateItemRequest))]
// EOE023 types
[JsonSerializable(typeof(EOE023_UnknownErrorFactory.CreateRequest), TypeInfoPropertyName = "Eoe023CreateRequest")]
// EOE024 types
[JsonSerializable(typeof(Eoe024TodoItem))]
[JsonSerializable(typeof(List<Eoe024TodoItem>))]
// EOE025 types (namespace Eoe025)
[JsonSerializable(typeof(PersonResponse))]
[JsonSerializable(typeof(List<PersonResponse>))]
// EOE026 types (namespace Eoe026)
[JsonSerializable(typeof(OrderRequest))]
[JsonSerializable(typeof(OrderResponse))]
[JsonSerializable(typeof(List<OrderResponse>))]
// EOE007 types
[JsonSerializable(typeof(ProductResponse))]
[JsonSerializable(typeof(CategoryResponse))]
[JsonSerializable(typeof(List<ProductResponse>))]
// EOE036 types
[JsonSerializable(typeof(DataModel))]
// EOE037 types
[JsonSerializable(typeof(DataItem))]
[JsonSerializable(typeof(List<DataItem>))]
// EOE038 types
[JsonSerializable(typeof(TypedData))]
[JsonSerializable(typeof(FlexibleData))]
// EOE039 types
[JsonSerializable(typeof(SimpleTodoRequest))]
[JsonSerializable(typeof(TodoItem))]
// EOE040 types
[JsonSerializable(typeof(Eoe040Response))]
// EOE041 types
[JsonSerializable(typeof(Eoe041Response))]
// Error response types
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
internal partial class AppJsonContext : JsonSerializerContext
{
}
