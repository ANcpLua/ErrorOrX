namespace DiagnosticsDemos.Demos;

/// <summary>
/// EOE018: Inaccessible type in endpoint — Private or protected types cannot be used in endpoint signatures
/// because generated code cannot access them.
/// </summary>
/// <remarks>
/// The source generator creates code in a different class/namespace that needs
/// to access your types. Private and protected types are not accessible.
/// </remarks>
public static class EOE018_InaccessibleType
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE018: Private class used as return type
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // private class SecretData
    // {
    //     public string Value { get; set; } = string.Empty;
    // }
    //
    // [Get("/secret")]
    // public static ErrorOr<SecretData> GetSecret() => new SecretData { Value = "secret" };

    // -------------------------------------------------------------------------
    // TRIGGERS EOE018: Private record used as parameter
    // -------------------------------------------------------------------------
    // Uncomment to see the diagnostic:
    //
    // private record PrivateRequest(string Name);
    //
    // [Post("/private")]
    // public static ErrorOr<string> CreatePrivate([FromBody] PrivateRequest request)
    //     => $"Created: {request.Name}";

    // -------------------------------------------------------------------------
    // FIXED: Use public types
    // -------------------------------------------------------------------------
    [Get("/api/eoe018/public")]
    public static ErrorOr<PublicData> GetPublic()
    {
        return new PublicData { Value = "public" };
    }

    [Post("/api/eoe018/create")]
    public static ErrorOr<string> Create([FromBody] PublicRequest request)
    {
        return $"Created: {request.Name}";
    }

    // -------------------------------------------------------------------------
    // NOTE: Internal types work within the same assembly but public is preferred
    // -------------------------------------------------------------------------
    // For demonstration purposes, we use public types here.
    // Internal types would work but require the generated code to be in the same assembly.

    // -------------------------------------------------------------------------
    // TIP: Define types at namespace level, not nested in a class
    // -------------------------------------------------------------------------
}

// -------------------------------------------------------------------------
// FIXED: Public types accessible to generated code
// -------------------------------------------------------------------------
public class PublicData
{
    public string Value { get; set; } = string.Empty;
}

public record PublicRequest(string Name);

// -------------------------------------------------------------------------
// Internal types work within the same assembly
// -------------------------------------------------------------------------
// Note: Changed to public for simpler demo - internal would also work
// internal record InternalData(string Value);
