using ErrorOr.Core.Errors;

namespace ErrorOr.Core.ErrorOr;

public static partial class ErrorOrExtensions
{
    /// <summary>
    /// If the state is error, the provided function <paramref name="onError"/> is executed asynchronously and its result is returned.
    /// </summary>
    /// <typeparam name="TValue">The type of the underlying value in the <paramref name="errorOr"/>.</typeparam>
    /// <param name="errorOr">The <see cref="ErrorOr"/> instance.</param>
    /// <param name="onError">The function to execute if the state is error.</param>
    /// <returns>The result from calling <paramref name="onError"/> if state is error; otherwise the original value.</returns>
    public static async Task<ErrorOr<TValue>> Else<TValue>(this Task<ErrorOr<TValue>> errorOr, Func<IReadOnlyList<Error>, TValue> onError)
    {
        var result = await errorOr.ConfigureAwait(false);

        return result.Else(onError);
    }

    /// <summary>
    /// If the state is error, the provided function <paramref name="onError"/> is executed asynchronously and its result is returned.
    /// </summary>
    /// <typeparam name="TValue">The type of the underlying value in the <paramref name="errorOr"/>.</typeparam>
    /// <param name="errorOr">The <see cref="ErrorOr"/> instance.</param>
    /// <param name="onError">The function to execute if the state is error.</param>
    /// <returns>The result from calling <paramref name="onError"/> if state is error; otherwise the original value.</returns>
    public static async Task<ErrorOr<TValue>> Else<TValue>(this Task<ErrorOr<TValue>> errorOr, TValue onError)
    {
        var result = await errorOr.ConfigureAwait(false);

        return result.Else(onError);
    }

    /// <summary>
    /// If the state is error, the provided function <paramref name="onError"/> is executed asynchronously and its result is returned.
    /// </summary>
    /// <typeparam name="TValue">The type of the underlying value in the <paramref name="errorOr"/>.</typeparam>
    /// <param name="errorOr">The <see cref="ErrorOr"/> instance.</param>
    /// <param name="onError">The function to execute if the state is error.</param>
    /// <returns>The result from calling <paramref name="onError"/> if state is error; otherwise the original value.</returns>
    public static async Task<ErrorOr<TValue>> ElseAsync<TValue>(this Task<ErrorOr<TValue>> errorOr, Func<IReadOnlyList<Error>, Task<TValue>> onError)
    {
        var result = await errorOr.ConfigureAwait(false);

        return await result.ElseAsync(onError).ConfigureAwait(false);
    }

    /// <summary>
    /// If the state is error, the provided function <paramref name="onError"/> is executed asynchronously and its result is returned.
    /// </summary>
    /// <typeparam name="TValue">The type of the underlying value in the <paramref name="errorOr"/>.</typeparam>
    /// <param name="errorOr">The <see cref="ErrorOr"/> instance.</param>
    /// <param name="onError">The function to execute if the state is error.</param>
    /// <returns>The result from calling <paramref name="onError"/> if state is error; otherwise the original value.</returns>
    public static async Task<ErrorOr<TValue>> ElseAsync<TValue>(this Task<ErrorOr<TValue>> errorOr, Task<TValue> onError)
    {
        var result = await errorOr.ConfigureAwait(false);

        return await result.ElseAsync(onError).ConfigureAwait(false);
    }

    /// <summary>
    /// If the state is error, the provided function <paramref name="onError"/> is executed asynchronously and its result is returned.
    /// </summary>
    /// <typeparam name="TValue">The type of the underlying value in the <paramref name="errorOr"/>.</typeparam>
    /// <param name="errorOr">The <see cref="ErrorOr"/> instance.</param>
    /// <param name="onError">The function to execute if the state is error.</param>
    /// <returns>The result from calling <paramref name="onError"/> if state is error; otherwise the original value.</returns>
    public static async Task<ErrorOr<TValue>> Else<TValue>(this Task<ErrorOr<TValue>> errorOr, Func<IReadOnlyList<Error>, Error> onError)
    {
        var result = await errorOr.ConfigureAwait(false);

        return result.Else(onError);
    }

    /// <summary>
    /// If the state is error, the provided function <paramref name="onError"/> is executed asynchronously and its result is returned.
    /// </summary>
    /// <typeparam name="TValue">The type of the underlying value in the <paramref name="errorOr"/>.</typeparam>
    /// <param name="errorOr">The <see cref="ErrorOr"/> instance.</param>
    /// <param name="onError">The function to execute if the state is error.</param>
    /// <returns>The result from calling <paramref name="onError"/> if state is error; otherwise the original value.</returns>
    public static async Task<ErrorOr<TValue>> Else<TValue>(this Task<ErrorOr<TValue>> errorOr, Func<IReadOnlyList<Error>, IReadOnlyList<Error>> onError)
    {
        var result = await errorOr.ConfigureAwait(false);

        return result.Else(onError);
    }

    /// <summary>
    /// If the state is error, the provided <paramref name="error"/> is returned.
    /// </summary>
    /// <typeparam name="TValue">The type of the underlying value in the <paramref name="errorOr"/>.</typeparam>
    /// <param name="errorOr">The <see cref="ErrorOr"/> instance.</param>
    /// <param name="error">The error to return.</param>
    /// <returns>The given <paramref name="error"/>.</returns>
    public static async Task<ErrorOr<TValue>> Else<TValue>(this Task<ErrorOr<TValue>> errorOr, Error error)
    {
        var result = await errorOr.ConfigureAwait(false);

        return result.Else(error);
    }

    /// <summary>
    /// If the state is error, the provided function <paramref name="onError"/> is executed asynchronously and its result is returned.
    /// </summary>
    /// <typeparam name="TValue">The type of the underlying value in the <paramref name="errorOr"/>.</typeparam>
    /// <param name="errorOr">The <see cref="ErrorOr"/> instance.</param>
    /// <param name="onError">The function to execute if the state is error.</param>
    /// <returns>The result from calling <paramref name="onError"/> if state is error; otherwise the original value.</returns>
    public static async Task<ErrorOr<TValue>> ElseAsync<TValue>(this Task<ErrorOr<TValue>> errorOr, Func<IReadOnlyList<Error>, Task<Error>> onError)
    {
        var result = await errorOr.ConfigureAwait(false);

        return await result.ElseAsync(onError).ConfigureAwait(false);
    }

    /// <summary>
    /// If the state is error, the provided function <paramref name="onError"/> is executed asynchronously and its result is returned.
    /// </summary>
    /// <typeparam name="TValue">The type of the underlying value in the <paramref name="errorOr"/>.</typeparam>
    /// <param name="errorOr">The <see cref="ErrorOr"/> instance.</param>
    /// <param name="onError">The function to execute if the state is error.</param>
    /// <returns>The result from calling <paramref name="onError"/> if state is error; otherwise the original value.</returns>
    public static async Task<ErrorOr<TValue>> ElseAsync<TValue>(this Task<ErrorOr<TValue>> errorOr, Func<IReadOnlyList<Error>, Task<IReadOnlyList<Error>>> onError)
    {
        var result = await errorOr.ConfigureAwait(false);

        return await result.ElseAsync(onError).ConfigureAwait(false);
    }

    /// <summary>
    /// If the state is error, the provided function <paramref name="onError"/> is executed asynchronously and its result is returned.
    /// </summary>
    /// <typeparam name="TValue">The type of the underlying value in the <paramref name="errorOr"/>.</typeparam>
    /// <param name="errorOr">The <see cref="ErrorOr"/> instance.</param>
    /// <param name="onError">The function to execute if the state is error.</param>
    /// <returns>The result from calling <paramref name="onError"/> if state is error; otherwise the original value.</returns>
    public static async Task<ErrorOr<TValue>> ElseAsync<TValue>(this Task<ErrorOr<TValue>> errorOr, Task<Error> onError)
    {
        var result = await errorOr.ConfigureAwait(false);

        return await result.ElseAsync(onError).ConfigureAwait(false);
    }

    /// <summary>
    /// If the state is error, the provided action <paramref name="onError"/> is executed for side-effects.
    /// </summary>
    /// <typeparam name="TValue">The type of the underlying value in the <paramref name="errorOr"/>.</typeparam>
    /// <param name="errorOr">The <see cref="ErrorOr"/> instance.</param>
    /// <param name="onError">The action to execute if the state is error.</param>
    /// <returns>The original <see cref="ErrorOr"/> instance.</returns>
    public static async Task<ErrorOr<TValue>> ElseDo<TValue>(this Task<ErrorOr<TValue>> errorOr, Action<IReadOnlyList<Error>> onError)
    {
        var result = await errorOr.ConfigureAwait(false);

        return result.ElseDo(onError);
    }

    /// <summary>
    /// If the state is error, the provided action <paramref name="onError"/> is executed asynchronously for side-effects.
    /// </summary>
    /// <typeparam name="TValue">The type of the underlying value in the <paramref name="errorOr"/>.</typeparam>
    /// <param name="errorOr">The <see cref="ErrorOr"/> instance.</param>
    /// <param name="onError">The action to execute if the state is error.</param>
    /// <returns>The original <see cref="ErrorOr"/> instance.</returns>
    public static async Task<ErrorOr<TValue>> ElseDoAsync<TValue>(this Task<ErrorOr<TValue>> errorOr, Func<IReadOnlyList<Error>, Task> onError)
    {
        var result = await errorOr.ConfigureAwait(false);

        return await result.ElseDoAsync(onError).ConfigureAwait(false);
    }
}
