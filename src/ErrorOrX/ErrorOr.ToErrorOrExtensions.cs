namespace ErrorOr;

public static partial class ErrorOrExtensions
{
    /// <summary>
    ///     Creates an <see cref="ErrorOr{TValue}" /> instance with the given <paramref name="value" />.
    /// </summary>
    public static ErrorOr<TValue> ToErrorOr<TValue>(this TValue value) => value;

    /// <summary>
    ///     Creates an <see cref="ErrorOr{TValue}" /> instance with the given <paramref name="error" />.
    /// </summary>
    public static ErrorOr<TValue> ToErrorOr<TValue>(this in Error error) => error;

    /// <summary>
    ///     Creates an <see cref="ErrorOr{TValue}" /> instance with the given <paramref name="errors" />.
    /// </summary>
    public static ErrorOr<TValue> ToErrorOr<TValue>(this IReadOnlyList<Error> errors) => new(errors);

    /// <summary>
    ///     Creates an <see cref="ErrorOr{TValue}" /> instance with the given <paramref name="errors" />.
    /// </summary>
    public static ErrorOr<TValue> ToErrorOr<TValue>(this Error[] errors) => errors;
}
