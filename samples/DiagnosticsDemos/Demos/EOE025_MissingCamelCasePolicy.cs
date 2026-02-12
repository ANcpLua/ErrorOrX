namespace DiagnosticsDemos.Demos.Eoe025;

public record PersonResponse(int Id, string FirstName, string LastName);

// -------------------------------------------------------------------------
// TRIGGERS EOE025: JsonSerializerContext without CamelCase policy
// -------------------------------------------------------------------------
// Uncomment to see the diagnostic (warning):
//
// [JsonSerializable(typeof(PersonResponse))]
// internal partial class NoCamelCaseContext : JsonSerializerContext { }

// -------------------------------------------------------------------------
// FIXED: Add CamelCase naming policy
// -------------------------------------------------------------------------
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(PersonResponse))]
[JsonSerializable(typeof(List<PersonResponse>))]
internal partial class EOE025JsonContext : JsonSerializerContext
{
}

/// <summary>
/// EOE025: Missing CamelCase policy — JsonSerializerContext is missing CamelCase property naming policy.
/// </summary>
/// <remarks>
/// Web APIs typically use camelCase for JSON properties (firstName, lastName).
/// C# uses PascalCase (FirstName, LastName). Without a naming policy,
/// your JSON will use PascalCase which is unusual for web APIs.
/// </remarks>
public static class EOE025_MissingCamelCasePolicy
{
    // -------------------------------------------------------------------------
    // With CamelCase policy, PersonResponse serializes as:
    // { "id": 1, "firstName": "John", "lastName": "Doe" }
    //
    // Without CamelCase policy, it would serialize as:
    // { "Id": 1, "FirstName": "John", "LastName": "Doe" }
    // -------------------------------------------------------------------------

    [Get("/api/eoe025/person/{id}")]
    public static ErrorOr<PersonResponse> GetPerson(int id)
    {
        return new PersonResponse(id, "John", "Doe");
    }

    [Get("/api/eoe025/people")]
    public static ErrorOr<List<PersonResponse>> GetPeople()
    {
        return new List<PersonResponse>
        {
            new(1, "John", "Doe"),
            new(2, "Jane", "Smith")
        };
    }
}

// -------------------------------------------------------------------------
// TIP: Best practices for JsonSerializerContext
// -------------------------------------------------------------------------
//
// [JsonSourceGenerationOptions(
//     PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
//     DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
// [JsonSerializable(typeof(YourType))]
// [JsonSerializable(typeof(List<YourType>))]
// [JsonSerializable(typeof(ProblemDetails))]            // For error responses
// [JsonSerializable(typeof(HttpValidationProblemDetails))] // For validation errors
// internal partial class AppJsonContext : JsonSerializerContext { }
