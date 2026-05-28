using System.Collections.Immutable;

namespace ErrorOr;

/// <summary>
///     A discriminated union of errors or a value.
/// </summary>
/// <typeparam name="TValue">The type of the underlying <see cref="Value" />.</typeparam>
public readonly partial record struct ErrorOr<TValue> : IErrorOr<TValue>
{
#pragma warning disable EPS11 // ImmutableArray is correctly handled via IsDefaultOrEmpty checks
    private readonly ImmutableArray<Error> _errors;
#pragma warning restore EPS11
    private readonly TValue? _value = default;

    /// <summary>
    ///     Prevents a default <see cref="ErrorOr" /> struct from being created.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when this method is called.</exception>
    public ErrorOr()
    {
        throw new InvalidOperationException(
            "Default construction of ErrorOr<TValue> is invalid. Please use provided factory methods to instantiate.");
    }

    private ErrorOr(in Error error)
    {
        _errors = [error];
    }

    internal ErrorOr(IReadOnlyList<Error> errors)
    {
        _ = Guard.NotNull(errors);

        if (errors.Count is 0)
        {
            throw new ArgumentException(
                "Cannot create an ErrorOr<TValue> from an empty collection of errors. Provide at least one error.",
                nameof(errors));
        }

        _errors = [.. errors];
    }

    private ErrorOr(TValue value)
    {
        _value = Guard.NotNull(value);
        // Mark as initialized — distinguishes a real success-state ErrorOr from default(ErrorOr<T>).
        // Without this, default(ErrorOr<T>) silently looks like a success-state value of default(TValue),
        // and IsSuccess returns true while Value access produces 0/null without warning.
        _errors = ImmutableArray<Error>.Empty;
    }

    /// <summary>
    ///     Gets a value indicating whether the state is success (no errors).
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the instance is <c>default(ErrorOr&lt;TValue&gt;)</c>. Such instances are not
    ///     valid — construct via factory methods or implicit conversion from <typeparamref name="TValue"/>
    ///     or <see cref="Error"/>.
    /// </exception>
    [MemberNotNullWhen(false, nameof(_errors))]
    [MemberNotNullWhen(false, nameof(Errors))]
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(true, nameof(_value))]
    public bool IsSuccess
    {
        get
        {
            ThrowIfDefault();
            return _errors.Length is 0;
        }
    }

    /// <summary>
    ///     Gets the list of errors.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the instance is <c>default(ErrorOr&lt;TValue&gt;)</c>, or when no errors are present.
    /// </exception>
    public IReadOnlyList<Error> Errors
    {
        get
        {
            ThrowIfDefault();
            return _errors.Length > 0
                ? _errors
                : throw new InvalidOperationException(
                    "The Errors property cannot be accessed when no errors have been recorded. Check IsError before accessing Errors.");
        }
    }

    /// <summary>
    ///     Gets the list of errors. If the state is not error, the list will be empty.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the instance is <c>default(ErrorOr&lt;TValue&gt;)</c>.
    /// </exception>
    public IReadOnlyList<Error> ErrorsOrEmpty
    {
        get
        {
            ThrowIfDefault();
            return _errors.Length > 0 ? _errors : [];
        }
    }

    /// <summary>
    ///     Gets a value indicating whether the state is error.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the instance is <c>default(ErrorOr&lt;TValue&gt;)</c>.
    /// </exception>
    [MemberNotNullWhen(true, nameof(_errors))]
    [MemberNotNullWhen(true, nameof(Errors))]
    [MemberNotNullWhen(false, nameof(Value))]
    [MemberNotNullWhen(false, nameof(_value))]
    public bool IsError
    {
        get
        {
            ThrowIfDefault();
            return _errors.Length > 0;
        }
    }

    /// <inheritdoc />
    IReadOnlyList<Error>? IErrorOr.Errors => IsError ? _errors : null;

    /// <inheritdoc />
    IReadOnlyList<Error> IErrorOr.ErrorsOrEmpty => ErrorsOrEmpty;

    /// <summary>
    ///     Gets the value.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the instance is <c>default(ErrorOr&lt;TValue&gt;)</c>, or when errors are present.
    /// </exception>
    public TValue Value
    {
        get
        {
            ThrowIfDefault();
            if (_errors.Length > 0)
            {
                throw new InvalidOperationException(
                    "The Value property cannot be accessed when errors have been recorded. Check IsError before accessing Value.");
            }

            // _value is guaranteed non-null here: the value constructor uses Guard.NotNull, and
            // ThrowIfDefault above eliminated the only path where _value could be default(TValue?).
            return _value!;
        }
    }

    /// <summary>
    ///     Gets the first error.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the instance is <c>default(ErrorOr&lt;TValue&gt;)</c>, or when no errors are present.
    /// </exception>
    public Error FirstError
    {
        get
        {
            ThrowIfDefault();
            if (_errors.Length is 0)
            {
                throw new InvalidOperationException(
                    "The FirstError property cannot be accessed when no errors have been recorded. Check IsError before accessing FirstError.");
            }

            return _errors[0];
        }
    }

    /// <summary>
    ///     Returns a string representation of the ErrorOr instance. Safe to call on
    ///     <c>default(ErrorOr&lt;TValue&gt;)</c> — returns a sentinel string instead of throwing,
    ///     so debug-time inspection of uninitialized instances is possible.
    /// </summary>
    public override string ToString()
    {
        if (_errors.IsDefault)
            return $"ErrorOr<{typeof(TValue).Name}> {{ uninitialized — default(ErrorOr<TValue>) }}";

        return _errors.Length > 0
            ? $"ErrorOr {{ IsError = True, FirstError = {_errors[0]} }}"
            : $"ErrorOr {{ IsError = False, Value = {_value} }}";
    }

    /// <summary>
    ///     Guards against access on <c>default(ErrorOr&lt;TValue&gt;)</c> — a state where the
    ///     struct memory is zero-initialized and no constructor ever ran. Distinguished from
    ///     <see cref="ImmutableArray{T}.Empty"/> via <see cref="ImmutableArray{T}.IsDefault"/>.
    ///     The value constructor sets <c>_errors</c> to <c>Empty</c> precisely so this guard
    ///     can distinguish "intentionally success" from "never constructed".
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when <c>_errors.IsDefault</c> is true (i.e., the instance is
    ///     <c>default(ErrorOr&lt;TValue&gt;)</c>).
    /// </exception>
    private void ThrowIfDefault()
    {
        if (_errors.IsDefault)
        {
            throw new InvalidOperationException(
                $"Access on default(ErrorOr<{typeof(TValue).Name}>) is invalid. " +
                "An uninitialized ErrorOr<TValue> struct cannot be inspected. " +
                "Construct via implicit conversion from TValue/Error, or via factory methods. " +
                "Common cause: a struct field declared without initialization, e.g. `private ErrorOr<int> _result;` — assign before reading.");
        }
    }
}
