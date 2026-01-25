namespace ErrorOr;

public readonly partial record struct ErrorOr<TValue>
{
    /// <summary>
    ///     If the state is a value, the provided function <paramref name="onValue" /> is executed and its result is returned.
    /// </summary>
    /// <typeparam name="TNextValue">The type of the result.</typeparam>
    /// <param name="onValue">The function to execute if the state is a value.</param>
    /// <returns>
    ///     The result from calling <paramref name="onValue" /> if state is value; otherwise the original
    ///     <see cref="Errors" />.
    /// </returns>
    public ErrorOr<TNextValue> Then<TNextValue>(Func<TValue, ErrorOr<TNextValue>> onValue)
    {
        _ = Throw.IfNull(onValue);

        return IsError ? new ErrorOr<TNextValue>(_errors) : onValue(Value);
    }

    /// <summary>
    ///     If the state is a value, the provided <paramref name="action" /> is invoked.
    /// </summary>
    /// <param name="action">The action to execute if the state is a value.</param>
    /// <returns>The original <see cref="ErrorOr" /> instance.</returns>
    public ErrorOr<TValue> ThenDo(Action<TValue> action)
    {
        _ = Throw.IfNull(action);

        if (IsError)
        {
            return new ErrorOr<TValue>(_errors);
        }

        action(Value);

        return this;
    }

    /// <summary>
    ///     If the state is a value, the provided function <paramref name="onValue" /> is executed and its result is returned.
    /// </summary>
    /// <typeparam name="TNextValue">The type of the result.</typeparam>
    /// <param name="onValue">The function to execute if the state is a value.</param>
    /// <returns>
    ///     The result from calling <paramref name="onValue" /> if state is value; otherwise the original
    ///     <see cref="Errors" />.
    /// </returns>
    public ErrorOr<TNextValue> Then<TNextValue>(Func<TValue, TNextValue> onValue)
    {
        _ = Throw.IfNull(onValue);

        if (IsError)
        {
            return new ErrorOr<TNextValue>(_errors);
        }

        return onValue(Value);
    }

    /// <summary>
    ///     If the state is a value, the provided function <paramref name="onValue" /> is executed asynchronously and its
    ///     result is returned.
    /// </summary>
    /// <typeparam name="TNextValue">The type of the result.</typeparam>
    /// <param name="onValue">The function to execute if the state is a value.</param>
    /// <returns>
    ///     The result from calling <paramref name="onValue" /> if state is value; otherwise the original
    ///     <see cref="Errors" />.
    /// </returns>
    public async Task<ErrorOr<TNextValue>> ThenAsync<TNextValue>(Func<TValue, Task<ErrorOr<TNextValue>>> onValue)
    {
        _ = Throw.IfNull(onValue);

        if (IsError)
        {
            return new ErrorOr<TNextValue>(_errors);
        }

        return await onValue(Value).ConfigureAwait(false);
    }

    /// <summary>
    ///     If the state is a value, the provided <paramref name="action" /> is invoked asynchronously.
    /// </summary>
    /// <param name="action">The action to execute if the state is a value.</param>
    /// <returns>The original <see cref="ErrorOr" /> instance.</returns>
    public async Task<ErrorOr<TValue>> ThenDoAsync(Func<TValue, Task> action)
    {
        _ = Throw.IfNull(action);

        if (IsError)
        {
            return new ErrorOr<TValue>(_errors);
        }

        await action(Value).ConfigureAwait(false);

        return this;
    }

    /// <summary>
    ///     If the state is a value, the provided function <paramref name="onValue" /> is executed asynchronously and its
    ///     result is returned.
    /// </summary>
    /// <typeparam name="TNextValue">The type of the result.</typeparam>
    /// <param name="onValue">The function to execute if the state is a value.</param>
    /// <returns>
    ///     The result from calling <paramref name="onValue" /> if state is value; otherwise the original
    ///     <see cref="Errors" />.
    /// </returns>
    public async Task<ErrorOr<TNextValue>> ThenAsync<TNextValue>(Func<TValue, Task<TNextValue>> onValue)
    {
        _ = Throw.IfNull(onValue);

        if (IsError)
        {
            return new ErrorOr<TNextValue>(_errors);
        }

        return await onValue(Value).ConfigureAwait(false);
    }
}
