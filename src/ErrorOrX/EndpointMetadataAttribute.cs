namespace ErrorOr;

/// <summary>
///     Adds custom metadata to an endpoint for use by transformers and middleware.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class EndpointMetadataAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance with the specified key and value.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    public EndpointMetadataAttribute(string key, string value)
    {
        Key = key;
        Value = value;
    }

    /// <summary>Gets the metadata key.</summary>
    public string Key { get; }

    /// <summary>Gets the metadata value.</summary>
    public string Value { get; }
}
