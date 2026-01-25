namespace ErrorOr;

public readonly partial record struct ErrorOr<TValue>
{
    /// <summary>
    ///     Creates an <see cref="ErrorOr{TValue}" /> from a value.
    /// </summary>
    public static implicit operator ErrorOr<TValue>(TValue value) => new(value);

    /// <summary>
    ///     Creates an <see cref="ErrorOr{TValue}" /> from an error.
    /// </summary>
    public static implicit operator ErrorOr<TValue>(Error error) => new(error);

    /// <summary>
    ///     Creates an <see cref="ErrorOr{TValue}" /> from an array of errors.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="errors" /> is an empty array.</exception>
    public static implicit operator ErrorOr<TValue>(Error[] errors) =>
        new([.. Throw.IfNull(errors)]);
}
