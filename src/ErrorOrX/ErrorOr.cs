// CA1000: Static From method on generic type is standard functional programming API design.
// CA1002: List<Error> is exposed intentionally for ergonomic API - users commonly work with List<T>.
#pragma warning disable CA1000, CA1002

using Microsoft.Shared.Diagnostics;

namespace ErrorOr;

/// <summary>
///     A discriminated union of errors or a value.
/// </summary>
/// <typeparam name="TValue">The type of the underlying <see cref="Value" />.</typeparam>
public readonly partial record struct ErrorOr<TValue> : IErrorOr<TValue>
{
    private readonly List<Error>? _errors = null;
    private readonly TValue? _value = default;

    /// <summary>
    ///     Prevents a default <see cref="ErrorOr" /> struct from being created.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when this method is called.</exception>
    public ErrorOr() =>
        throw new InvalidOperationException(
            "Default construction of ErrorOr<TValue> is invalid. Please use provided factory methods to instantiate.");

    private ErrorOr(in Error error) => _errors = [error];

    private ErrorOr(List<Error> errors)
    {
        _ = Throw.IfNull(errors);

        if (errors.Count is 0)
        {
            throw new ArgumentException(
                "Cannot create an ErrorOr<TValue> from an empty collection of errors. Provide at least one error.",
                nameof(errors));
        }

        _errors = errors;
    }

    private ErrorOr(TValue value) => _value = Throw.IfNull(value);

    /// <summary>
    ///     Gets a value indicating whether the state is success (no errors).
    /// </summary>
    [MemberNotNullWhen(false, nameof(_errors))]
    [MemberNotNullWhen(false, nameof(Errors))]
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(true, nameof(_value))]
    public bool IsSuccess => _errors is null;

    /// <summary>
    ///     Gets the list of errors.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no errors are present.</exception>
    public IReadOnlyList<Error> Errors => IsError
        ? _errors
        : throw new InvalidOperationException(
            "The Errors property cannot be accessed when no errors have been recorded. Check IsError before accessing Errors.");

    /// <summary>
    ///     Gets the list of errors. If the state is not error, the list will be empty.
    /// </summary>
    public IReadOnlyList<Error> ErrorsOrEmptyList => IsError ? _errors : EmptyErrors.Instance;

    /// <summary>
    ///     Gets a value indicating whether the state is error.
    /// </summary>
    [MemberNotNullWhen(true, nameof(_errors))]
    [MemberNotNullWhen(true, nameof(Errors))]
    [MemberNotNullWhen(false, nameof(Value))]
    [MemberNotNullWhen(false, nameof(_value))]
    public bool IsError => _errors is not null;

    /// <inheritdoc />
    IReadOnlyList<Error>? IErrorOr.Errors => IsError ? _errors : null;

    /// <summary>
    ///     Gets the value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no value is present.</exception>
    public TValue Value
    {
        get
        {
            if (IsError)
            {
                throw new InvalidOperationException(
                    "The Value property cannot be accessed when errors have been recorded. Check IsError before accessing Value.");
            }

            return _value;
        }
    }

    /// <summary>
    ///     Gets the first error.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no errors are present.</exception>
    public Error FirstError
    {
        get
        {
            if (!IsError)
            {
                throw new InvalidOperationException(
                    "The FirstError property cannot be accessed when no errors have been recorded. Check IsError before accessing FirstError.");
            }

            return _errors[0];
        }
    }

    /// <summary>
    ///     Creates an <see cref="ErrorOr{TValue}" /> from a list of errors.
    /// </summary>
    public static ErrorOr<TValue> From(List<Error> errors) => errors;

    /// <summary>
    ///     Returns a string representation of the ErrorOr instance.
    /// </summary>
    public override string ToString() =>
        IsError
            ? $"ErrorOr {{ IsError = True, FirstError = {FirstError} }}"
            : $"ErrorOr {{ IsError = False, Value = {_value} }}";
}
