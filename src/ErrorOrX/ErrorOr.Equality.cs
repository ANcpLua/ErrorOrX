using System.Collections.Immutable;

namespace ErrorOr;

public readonly partial record struct ErrorOr<TValue>
{
    /// <inheritdoc />
    public bool Equals(ErrorOr<TValue> other)
    {
        if (!IsError)
        {
            return !other.IsError && EqualityComparer<TValue>.Default.Equals(_value, other._value);
        }

        return other.IsError && CheckIfErrorsAreEqual(_errors, other._errors);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        if (!IsError)
        {
            return _value?.GetHashCode() ?? 0;
        }

        var hashCode = new HashCode();
        foreach (var t in _errors)
        {
            hashCode.Add(t);
        }

        return hashCode.ToHashCode();
    }

    private static bool CheckIfErrorsAreEqual(ImmutableArray<Error> errors1, ImmutableArray<Error> errors2)
    {
        // This method is currently implemented with strict ordering in mind, so the errors
        // of the two arrays need to be in the exact same order.
        // This avoids allocating a hash set. We could provide a dedicated EqualityComparer for
        // ErrorOr<TValue> when arbitrary orders should be supported.
        if (errors1.Length != errors2.Length)
        {
            return false;
        }

        for (var i = 0; i < errors1.Length; i++)
        {
            if (!errors1[i].Equals(errors2[i]))
            {
                return false;
            }
        }

        return true;
    }
}
