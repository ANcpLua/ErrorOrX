namespace ErrorOr;

/// <summary>
///     Specifies how empty request bodies should be handled.
/// </summary>
public enum EmptyBodyBehavior
{
    /// <summary>
    ///     Framework default: Nullable allows empty, non-nullable rejects.
    /// </summary>
    Default,

    /// <summary>
    ///     Empty bodies are valid (null/default assigned).
    /// </summary>
    Allow,

    /// <summary>
    ///     Empty bodies are invalid (400 Bad Request).
    /// </summary>
    Disallow
}
