using System.Collections.Immutable;

namespace ErrorOr;

public readonly partial record struct ErrorOr<TValue>
{
    /// <inheritdoc />
    /// <remarks>
    ///     Tolerates <c>default(ErrorOr&lt;TValue&gt;)</c> on either side — two default instances are equal,
    ///     default vs non-default is not equal. Equals/GetHashCode must not throw on the default-struct
    ///     state because dictionaries/sets call them on whatever key is given. The regular accessors
    ///     (<see cref="ErrorOr{TValue}.IsError"/>, <see cref="ErrorOr{TValue}.Value"/>, etc.) still throw on
    ///     <c>default</c> — those are the load-bearing surfaces, not equality.
    /// </remarks>
    public bool Equals(ErrorOr<TValue> other)
    {
        // Default-state handling: avoid IsError (which throws on default) by checking _errors.IsDefault directly.
        var thisDefault = _errors.IsDefault;
        var otherDefault = other._errors.IsDefault;
        if (thisDefault || otherDefault) return thisDefault && otherDefault;

        if (_errors.Length is 0)
            return other._errors.Length is 0 && EqualityComparer<TValue>.Default.Equals(_value, other._value);

        return other._errors.Length > 0 && CheckIfErrorsAreEqual(_errors, other._errors);
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Tolerates <c>default(ErrorOr&lt;TValue&gt;)</c> — returns 0 instead of throwing, so the struct
    ///     remains usable as a dictionary key / set element even in its uninitialized state. The regular
    ///     accessors still throw on <c>default</c>.
    /// </remarks>
    public override int GetHashCode()
    {
        if (_errors.IsDefault) return 0;

        if (_errors.Length is 0) return _value?.GetHashCode() ?? 0;

        var hashCode = new HashCode();
        foreach (var t in _errors) hashCode.Add(t);

        return hashCode.ToHashCode();
    }

    private static bool CheckIfErrorsAreEqual(ImmutableArray<Error> errors1, ImmutableArray<Error> errors2)
    {
        // This method is currently implemented with strict ordering in mind, so the errors
        // of the two arrays need to be in the exact same order.
        // This avoids allocating a hash set. We could provide a dedicated EqualityComparer for
        // ErrorOr<TValue> when arbitrary orders should be supported.
        if (errors1.Length != errors2.Length) return false;

        for (var i = 0; i < errors1.Length; i++)
        {
            if (!errors1[i].Equals(errors2[i]))
                return false;
        }

        return true;
    }
}
