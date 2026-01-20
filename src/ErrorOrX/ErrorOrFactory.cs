namespace ErrorOr;

/// <summary>
///     Provides factory methods for creating instances of <see cref="ErrorOr{TValue}" />.
/// </summary>
public static class ErrorOrFactory
{
    /// <summary>
    ///     Creates a new instance of <see cref="ErrorOr{TValue}" /> with a value.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="value">The value to wrap.</param>
    /// <returns>An instance of <see cref="ErrorOr{TValue}" /> containing the provided value.</returns>
    public static ErrorOr<TValue> From<TValue>(TValue value) => value;

    /// <summary>
    ///     Creates a new instance of <see cref="ErrorOr{TValue}" /> with an error.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="error">The error to wrap.</param>
    /// <returns>An instance of <see cref="ErrorOr{TValue}" /> containing the provided error.</returns>
    public static ErrorOr<TValue> From<TValue>(Error error) => error;

    /// <summary>
    ///     Creates a new instance of <see cref="ErrorOr{TValue}" /> with a list of errors.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="errors">The list of errors to wrap.</param>
    /// <returns>An instance of <see cref="ErrorOr{TValue}" /> containing the provided list of errors.</returns>
    public static ErrorOr<TValue> From<TValue>(IReadOnlyList<Error> errors) => new(errors);
}
