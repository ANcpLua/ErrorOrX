using System.Collections.Frozen;

namespace ErrorOr;

/// <summary>
///     Represents an error.
/// </summary>
public readonly record struct Error
{
    private readonly FrozenDictionary<string, object>? _metadata;

    private Error(string code, string description, ErrorType type, IReadOnlyDictionary<string, object>? metadata)
    {
        Code = code;
        Description = description;
        Type = type;
        _metadata = metadata?.ToFrozenDictionary();
    }

    /// <summary>
    ///     Gets the unique error code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    ///     Gets the error description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    ///     Gets the error type.
    /// </summary>
    public ErrorType Type { get; }

    /// <summary>
    ///     Gets the metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata => _metadata;

    /// <inheritdoc />
    public bool Equals(Error other)
    {
        if (Type != other.Type || Code != other.Code || Description != other.Description)
        {
            return false;
        }

        if (_metadata is null)
        {
            return other._metadata is null;
        }

        return other._metadata is not null && CompareMetadata(_metadata, other._metadata);
    }

    /// <summary>
    ///     Creates an <see cref="Error" /> of type <see cref="ErrorType.Failure" /> from a code and description.
    /// </summary>
    /// <param name="code">The unique error code.</param>
    /// <param name="description">The error description.</param>
    /// <param name="metadata">A dictionary which provides optional space for information.</param>
    public static Error Failure(
        string code = "General.Failure",
        string description = "A failure has occurred.",
        IReadOnlyDictionary<string, object>? metadata = null) =>
        new(code, description, ErrorType.Failure, metadata);

    /// <summary>
    ///     Creates an <see cref="Error" /> of type <see cref="ErrorType.Unexpected" /> from a code and description.
    /// </summary>
    /// <param name="code">The unique error code.</param>
    /// <param name="description">The error description.</param>
    /// <param name="metadata">A dictionary which provides optional space for information.</param>
    public static Error Unexpected(
        string code = "General.Unexpected",
        string description = "An unexpected error has occurred.",
        IReadOnlyDictionary<string, object>? metadata = null) =>
        new(code, description, ErrorType.Unexpected, metadata);

    /// <summary>
    ///     Creates an <see cref="Error" /> of type <see cref="ErrorType.Validation" /> from a code and description.
    /// </summary>
    /// <param name="code">The unique error code.</param>
    /// <param name="description">The error description.</param>
    /// <param name="metadata">A dictionary which provides optional space for information.</param>
    public static Error Validation(
        string code = "General.Validation",
        string description = "A validation error has occurred.",
        IReadOnlyDictionary<string, object>? metadata = null) =>
        new(code, description, ErrorType.Validation, metadata);

    /// <summary>
    ///     Creates an <see cref="Error" /> of type <see cref="ErrorType.Conflict" /> from a code and description.
    /// </summary>
    /// <param name="code">The unique error code.</param>
    /// <param name="description">The error description.</param>
    /// <param name="metadata">A dictionary which provides optional space for information.</param>
    public static Error Conflict(
        string code = "General.Conflict",
        string description = "A conflict error has occurred.",
        IReadOnlyDictionary<string, object>? metadata = null) =>
        new(code, description, ErrorType.Conflict, metadata);

    /// <summary>
    ///     Creates an <see cref="Error" /> of type <see cref="ErrorType.NotFound" /> from a code and description.
    /// </summary>
    /// <param name="code">The unique error code.</param>
    /// <param name="description">The error description.</param>
    /// <param name="metadata">A dictionary which provides optional space for information.</param>
    public static Error NotFound(
        string code = "General.NotFound",
        string description = "A 'Not Found' error has occurred.",
        IReadOnlyDictionary<string, object>? metadata = null) =>
        new(code, description, ErrorType.NotFound, metadata);

    /// <summary>
    ///     Creates an <see cref="Error" /> of type <see cref="ErrorType.Unauthorized" /> from a code and description.
    /// </summary>
    /// <param name="code">The unique error code.</param>
    /// <param name="description">The error description.</param>
    /// <param name="metadata">A dictionary which provides optional space for information.</param>
    public static Error Unauthorized(
        string code = "General.Unauthorized",
        string description = "An 'Unauthorized' error has occurred.",
        IReadOnlyDictionary<string, object>? metadata = null) =>
        new(code, description, ErrorType.Unauthorized, metadata);

    /// <summary>
    ///     Creates an <see cref="Error" /> of type <see cref="ErrorType.Forbidden" /> from a code and description.
    /// </summary>
    /// <param name="code">The unique error code.</param>
    /// <param name="description">The error description.</param>
    /// <param name="metadata">A dictionary which provides optional space for information.</param>
    public static Error Forbidden(
        string code = "General.Forbidden",
        string description = "A 'Forbidden' error has occurred.",
        IReadOnlyDictionary<string, object>? metadata = null) =>
        new(code, description, ErrorType.Forbidden, metadata);

    /// <summary>
    ///     Creates an <see cref="Error" /> with the given numeric <paramref name="type" />,
    ///     <paramref name="code" />, and <paramref name="description" />.
    /// </summary>
    /// <param name="type">An integer value which represents the type of error that occurred.</param>
    /// <param name="code">The unique error code.</param>
    /// <param name="description">The error description.</param>
    /// <param name="metadata">A dictionary which provides optional space for information.</param>
    public static Error Custom(
        int type,
        string code,
        string description,
        IReadOnlyDictionary<string, object>? metadata = null) =>
        new(code, description, (ErrorType)type, metadata);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        if (_metadata is null)
        {
            return HashCode.Combine(Code, Description, Type);
        }

        var hashCode = new HashCode();
        hashCode.Add(Code);
        hashCode.Add(Description);
        hashCode.Add(Type);

        foreach (var kvp in _metadata)
        {
            hashCode.Add(kvp.Key);
            hashCode.Add(kvp.Value);
        }

        return hashCode.ToHashCode();
    }

    private static bool CompareMetadata(
        FrozenDictionary<string, object> metadata,
        FrozenDictionary<string, object> otherMetadata)
    {
        if (ReferenceEquals(metadata, otherMetadata))
        {
            return true;
        }

        if (metadata.Count != otherMetadata.Count)
        {
            return false;
        }

        foreach (var kvp in metadata)
        {
            if (!otherMetadata.TryGetValue(kvp.Key, out var otherValue) ||
                !Equals(kvp.Value, otherValue))
            {
                return false;
            }
        }

        return true;
    }
}
