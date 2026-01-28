namespace ErrorOr;

/// <summary>
///     Generic interface for <see cref="ErrorOr{TValue}" />.
/// </summary>
/// <typeparam name="TValue">The type of the success value.</typeparam>
public interface IErrorOr<out TValue> : IErrorOr
{
    /// <summary>
    ///     Gets the value.
    /// </summary>
    TValue Value { get; }
}

/// <summary>
///     Type-less interface for the <see cref="ErrorOr" /> object.
/// </summary>
/// <remarks>
///     This interface is intended for use when the underlying type of the <see cref="ErrorOr" /> object is unknown.
/// </remarks>
public interface IErrorOr
{
    /// <summary>
    ///     Gets the list of errors.
    /// </summary>
    IReadOnlyList<Error>? Errors { get; }

    /// <summary>
    ///     Gets the list of errors, or an empty list if no errors.
    /// </summary>
    IReadOnlyList<Error> ErrorsOrEmpty { get; }

    /// <summary>
    ///     Gets a value indicating whether the state is error.
    /// </summary>
    bool IsError { get; }

    /// <summary>
    ///     Gets the first error.
    /// </summary>
    Error FirstError { get; }
}
