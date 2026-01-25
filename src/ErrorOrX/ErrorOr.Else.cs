namespace ErrorOr;

public readonly partial record struct ErrorOr<TValue>
{
    /// <summary>
    ///     If the state is error, the provided function <paramref name="onError" /> is executed and its result is returned.
    /// </summary>
    /// <param name="onError">The function to execute if the state is error.</param>
    /// <returns>
    ///     The result from calling <paramref name="onError" /> if state is error; otherwise the original
    ///     <see cref="Value" />.
    /// </returns>
    public ErrorOr<TValue> Else(Func<IReadOnlyList<Error>, Error> onError)
    {
        _ = Throw.IfNull(onError);

        return IsError ? onError(Errors) : Value;
    }

    /// <summary>
    ///     If the state is error, the provided function <paramref name="onError" /> is executed and its result is returned.
    /// </summary>
    /// <param name="onError">The function to execute if the state is error.</param>
    /// <returns>
    ///     The result from calling <paramref name="onError" /> if state is error; otherwise the original
    ///     <see cref="Value" />.
    /// </returns>
    public ErrorOr<TValue> Else(Func<IReadOnlyList<Error>, IReadOnlyList<Error>> onError)
    {
        _ = Throw.IfNull(onError);

        return IsError ? new ErrorOr<TValue>(onError(Errors)) : Value;
    }

    /// <summary>
    ///     If the state is error, the provided <paramref name="error" /> is returned.
    /// </summary>
    /// <param name="error">The error to return.</param>
    /// <returns>The given <paramref name="error" />.</returns>
    public ErrorOr<TValue> Else(in Error error) =>
        IsError ? error : Value;

    /// <summary>
    ///     If the state is error, the provided function <paramref name="onError" /> is executed and its result is returned.
    /// </summary>
    /// <param name="onError">The function to execute if the state is error.</param>
    /// <returns>
    ///     The result from calling <paramref name="onError" /> if state is error; otherwise the original
    ///     <see cref="Value" />.
    /// </returns>
    public ErrorOr<TValue> Else(Func<IReadOnlyList<Error>, TValue> onError)
    {
        _ = Throw.IfNull(onError);

        return IsError ? onError(Errors) : Value;
    }

    /// <summary>
    ///     If the state is error, the provided function <paramref name="onError" /> is executed and its result is returned.
    /// </summary>
    /// <param name="onError">The value to return if the state is error.</param>
    /// <returns>
    ///     The result from calling <paramref name="onError" /> if state is error; otherwise the original
    ///     <see cref="Value" />.
    /// </returns>
    public ErrorOr<TValue> Else(TValue onError) =>
        IsError ? onError : Value;

    /// <summary>
    ///     If the state is error, the provided function <paramref name="onError" /> is executed asynchronously and its result
    ///     is returned.
    /// </summary>
    /// <param name="onError">The function to execute if the state is error.</param>
    /// <returns>
    ///     The result from calling <paramref name="onError" /> if state is error; otherwise the original
    ///     <see cref="Value" />.
    /// </returns>
    public async Task<ErrorOr<TValue>> ElseAsync(Func<IReadOnlyList<Error>, Task<TValue>> onError)
    {
        _ = Throw.IfNull(onError);

        return IsError ? await onError(Errors).ConfigureAwait(false) : Value;
    }

    /// <summary>
    ///     If the state is error, the provided function <paramref name="onError" /> is executed asynchronously and its result
    ///     is returned.
    /// </summary>
    /// <param name="onError">The function to execute if the state is error.</param>
    /// <returns>
    ///     The result from calling <paramref name="onError" /> if state is error; otherwise the original
    ///     <see cref="Value" />.
    /// </returns>
    public async Task<ErrorOr<TValue>> ElseAsync(Func<IReadOnlyList<Error>, Task<Error>> onError)
    {
        _ = Throw.IfNull(onError);

        return IsError ? await onError(Errors).ConfigureAwait(false) : Value;
    }

    /// <summary>
    ///     If the state is error, the provided function <paramref name="onError" /> is executed asynchronously and its result
    ///     is returned.
    /// </summary>
    /// <param name="onError">The function to execute if the state is error.</param>
    /// <returns>
    ///     The result from calling <paramref name="onError" /> if state is error; otherwise the original
    ///     <see cref="Value" />.
    /// </returns>
    public async Task<ErrorOr<TValue>> ElseAsync(Func<IReadOnlyList<Error>, Task<IReadOnlyList<Error>>> onError)
    {
        _ = Throw.IfNull(onError);

        return IsError ? new ErrorOr<TValue>(await onError(Errors).ConfigureAwait(false)) : Value;
    }

    /// <summary>
    ///     If the state is error, the provided <paramref name="error" /> is awaited and returned.
    /// </summary>
    /// <param name="error">The error to return if the state is error.</param>
    /// <returns>The result from awaiting the given <paramref name="error" />.</returns>
    public async Task<ErrorOr<TValue>> ElseAsync(Task<Error> error) =>
        IsError ? await error.ConfigureAwait(false) : Value;

    /// <summary>
    ///     If the state is error, the provided function <paramref name="onError" /> is executed asynchronously and its result
    ///     is returned.
    /// </summary>
    /// <param name="onError">The function to execute if the state is error.</param>
    /// <returns>
    ///     The result from calling <paramref name="onError" /> if state is error; otherwise the original
    ///     <see cref="Value" />.
    /// </returns>
    public async Task<ErrorOr<TValue>> ElseAsync(Task<TValue> onError) =>
        IsError ? await onError.ConfigureAwait(false) : Value;

    /// <summary>
    ///     If the state is error, the provided action <paramref name="onError" /> is executed for side-effects.
    /// </summary>
    /// <param name="onError">The action to execute if the state is error.</param>
    /// <returns>The original <see cref="ErrorOr" /> instance.</returns>
    public ErrorOr<TValue> ElseDo(Action<IReadOnlyList<Error>> onError)
    {
        _ = Throw.IfNull(onError);

        if (IsError)
        {
            onError(Errors);
        }

        return this;
    }

    /// <summary>
    ///     If the state is error, the provided action <paramref name="onError" /> is executed asynchronously for side-effects.
    /// </summary>
    /// <param name="onError">The action to execute if the state is error.</param>
    /// <returns>The original <see cref="ErrorOr" /> instance.</returns>
    public async Task<ErrorOr<TValue>> ElseDoAsync(Func<IReadOnlyList<Error>, Task> onError)
    {
        _ = Throw.IfNull(onError);

        if (IsError)
        {
            await onError(Errors).ConfigureAwait(false);
        }

        return this;
    }
}
