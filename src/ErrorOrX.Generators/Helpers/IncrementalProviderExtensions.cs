using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ErrorOr.Generators;

/// <summary>
///     Extension methods for combining multiple <see cref="IncrementalValuesProvider{TValue}" /> instances.
/// </summary>
internal static class IncrementalProviderExtensions
{
    /// <summary>
    ///     Combines an arbitrary number of providers into a single collected array.
    ///     Each step collects and merges pairwise, producing a flat <see cref="EquatableArray{T}" />.
    /// </summary>
    public static IncrementalValueProvider<EquatableArray<T>> CombineAll<T>(
        params IncrementalValuesProvider<T>[] providers)
        where T : IEquatable<T>
    {
        var result = providers[0].CollectAsEquatableArray();

        for (var i = 1; i < providers.Length; i++)
        {
            var next = providers[i].CollectAsEquatableArray();
            result = result.Combine(next)
                .Select(static (pair, _) =>
                {
                    var (left, right) = pair;
                    if (right.IsDefaultOrEmpty)
                    {
                        return left;
                    }

                    if (left.IsDefaultOrEmpty)
                    {
                        return right;
                    }

                    var builder = ImmutableArray.CreateBuilder<T>(left.Length + right.Length);
                    builder.AddRange(left.AsImmutableArray());
                    builder.AddRange(right.AsImmutableArray());
                    return new EquatableArray<T>(builder.ToImmutable());
                });
        }

        return result;
    }
}
