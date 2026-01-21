using System.Collections.Immutable;
using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;

namespace ErrorOr.Generators;

/// <summary>
///     Extension methods for combining multiple <see cref="IncrementalValuesProvider{TValue}" /> instances.
/// </summary>
internal static class IncrementalProviderExtensions
{
    /// <summary>
    ///     Combines six providers into a single collected array.
    ///     Used when collecting endpoints from multiple HTTP method attribute providers.
    /// </summary>
    public static IncrementalValueProvider<EquatableArray<T>> CombineSix<T>(
        IncrementalValuesProvider<T> p1,
        IncrementalValuesProvider<T> p2,
        IncrementalValuesProvider<T> p3,
        IncrementalValuesProvider<T> p4,
        IncrementalValuesProvider<T> p5,
        IncrementalValuesProvider<T> p6)
        where T : IEquatable<T>
    {
        // Collect each provider into EquatableArray for proper caching
        var c1 = p1.CollectAsEquatableArray();
        var c2 = p2.CollectAsEquatableArray();
        var c3 = p3.CollectAsEquatableArray();
        var c4 = p4.CollectAsEquatableArray();
        var c5 = p5.CollectAsEquatableArray();
        var c6 = p6.CollectAsEquatableArray();

        // Chain combines (Roslyn only supports pairwise)
        var combined = c1.Combine(c2).Combine(c3).Combine(c4).Combine(c5).Combine(c6);

        // Flatten into single array
        return combined.Select(static (data, _) =>
        {
            var (((((e0, e1), e2), e3), e4), e5) = data;
            var capacity = e0.Length + e1.Length + e2.Length + e3.Length + e4.Length + e5.Length;
            var builder = ImmutableArray.CreateBuilder<T>(capacity);

            if (!e0.IsDefaultOrEmpty) builder.AddRange(e0.AsImmutableArray());
            if (!e1.IsDefaultOrEmpty) builder.AddRange(e1.AsImmutableArray());
            if (!e2.IsDefaultOrEmpty) builder.AddRange(e2.AsImmutableArray());
            if (!e3.IsDefaultOrEmpty) builder.AddRange(e3.AsImmutableArray());
            if (!e4.IsDefaultOrEmpty) builder.AddRange(e4.AsImmutableArray());
            if (!e5.IsDefaultOrEmpty) builder.AddRange(e5.AsImmutableArray());

            return new EquatableArray<T>(builder.ToImmutable());
        });
    }

    /// <summary>
    ///     Combines nine providers into a single collected array.
    ///     Used when collecting endpoints from all HTTP method attribute providers (including HEAD, OPTIONS, TRACE).
    /// </summary>
    public static IncrementalValueProvider<EquatableArray<T>> CombineNine<T>(
        IncrementalValuesProvider<T> p1,
        IncrementalValuesProvider<T> p2,
        IncrementalValuesProvider<T> p3,
        IncrementalValuesProvider<T> p4,
        IncrementalValuesProvider<T> p5,
        IncrementalValuesProvider<T> p6,
        IncrementalValuesProvider<T> p7,
        IncrementalValuesProvider<T> p8,
        IncrementalValuesProvider<T> p9)
        where T : IEquatable<T>
    {
        // Collect each provider into EquatableArray for proper caching
        var c1 = p1.CollectAsEquatableArray();
        var c2 = p2.CollectAsEquatableArray();
        var c3 = p3.CollectAsEquatableArray();
        var c4 = p4.CollectAsEquatableArray();
        var c5 = p5.CollectAsEquatableArray();
        var c6 = p6.CollectAsEquatableArray();
        var c7 = p7.CollectAsEquatableArray();
        var c8 = p8.CollectAsEquatableArray();
        var c9 = p9.CollectAsEquatableArray();

        // Chain combines (Roslyn only supports pairwise)
        var combined = c1.Combine(c2).Combine(c3).Combine(c4).Combine(c5)
            .Combine(c6).Combine(c7).Combine(c8).Combine(c9);

        // Flatten into single array
        return combined.Select(static (data, _) =>
        {
            var ((((((((e0, e1), e2), e3), e4), e5), e6), e7), e8) = data;
            var capacity = e0.Length + e1.Length + e2.Length + e3.Length + e4.Length +
                           e5.Length + e6.Length + e7.Length + e8.Length;
            var builder = ImmutableArray.CreateBuilder<T>(capacity);

            if (!e0.IsDefaultOrEmpty) builder.AddRange(e0.AsImmutableArray());
            if (!e1.IsDefaultOrEmpty) builder.AddRange(e1.AsImmutableArray());
            if (!e2.IsDefaultOrEmpty) builder.AddRange(e2.AsImmutableArray());
            if (!e3.IsDefaultOrEmpty) builder.AddRange(e3.AsImmutableArray());
            if (!e4.IsDefaultOrEmpty) builder.AddRange(e4.AsImmutableArray());
            if (!e5.IsDefaultOrEmpty) builder.AddRange(e5.AsImmutableArray());
            if (!e6.IsDefaultOrEmpty) builder.AddRange(e6.AsImmutableArray());
            if (!e7.IsDefaultOrEmpty) builder.AddRange(e7.AsImmutableArray());
            if (!e8.IsDefaultOrEmpty) builder.AddRange(e8.AsImmutableArray());

            return new EquatableArray<T>(builder.ToImmutable());
        });
    }

    /// <summary>
    ///     Concatenates two collected arrays into a flattened values provider.
    ///     Chainable: <c>p1.CollectAsEquatableArray().Concat(p2.CollectAsEquatableArray())</c>
    /// </summary>
    /// <remarks>
    ///     Pattern from .NET Foundation validation generator.
    ///     Enables flexible provider composition without fixed-arity methods.
    /// </remarks>
    public static IncrementalValuesProvider<T> Concat<T>(
        this IncrementalValueProvider<EquatableArray<T>> first,
        IncrementalValueProvider<EquatableArray<T>> second)
        where T : IEquatable<T>
    {
        return first.Combine(second)
            .SelectMany(static (tuple, _) =>
            {
                var (left, right) = tuple;

                if (left.IsDefaultOrEmpty && right.IsDefaultOrEmpty)
                    return ImmutableArray<T>.Empty;

                var leftArr = left.IsDefaultOrEmpty
                    ? ImmutableArray<T>.Empty
                    : left.AsImmutableArray();
                var rightArr = right.IsDefaultOrEmpty
                    ? ImmutableArray<T>.Empty
                    : right.AsImmutableArray();

                var builder = ImmutableArray.CreateBuilder<T>(leftArr.Length + rightArr.Length);
                builder.AddRange(leftArr);
                builder.AddRange(rightArr);
                return builder.ToImmutable();
            });
    }

    /// <summary>
    ///     Concatenates a collected array with a values provider.
    ///     Enables mixing collected and uncollected providers.
    /// </summary>
    public static IncrementalValuesProvider<T> Concat<T>(
        this IncrementalValueProvider<EquatableArray<T>> first,
        IncrementalValuesProvider<T> second)
        where T : IEquatable<T>
    {
        return first.Combine(second.CollectAsEquatableArray())
            .SelectMany(static (tuple, _) =>
            {
                var (left, right) = tuple;

                if (left.IsDefaultOrEmpty && right.IsDefaultOrEmpty)
                    return ImmutableArray<T>.Empty;

                var leftArr = left.IsDefaultOrEmpty
                    ? ImmutableArray<T>.Empty
                    : left.AsImmutableArray();
                var rightArr = right.IsDefaultOrEmpty
                    ? ImmutableArray<T>.Empty
                    : right.AsImmutableArray();

                var builder = ImmutableArray.CreateBuilder<T>(leftArr.Length + rightArr.Length);
                builder.AddRange(leftArr);
                builder.AddRange(rightArr);
                return builder.ToImmutable();
            });
    }

    /// <summary>
    ///     Collects and concatenates with another collected array, returning EquatableArray.
    ///     Final step in a concat chain when you need EquatableArray for further combining.
    /// </summary>
    public static IncrementalValueProvider<EquatableArray<T>> ConcatAndCollect<T>(
        this IncrementalValueProvider<EquatableArray<T>> first,
        IncrementalValueProvider<EquatableArray<T>> second)
        where T : IEquatable<T>
    {
        return first.Combine(second)
            .Select(static (tuple, _) =>
            {
                var (left, right) = tuple;

                if (left.IsDefaultOrEmpty && right.IsDefaultOrEmpty)
                    return new EquatableArray<T>(ImmutableArray<T>.Empty);

                var leftArr = left.IsDefaultOrEmpty
                    ? ImmutableArray<T>.Empty
                    : left.AsImmutableArray();
                var rightArr = right.IsDefaultOrEmpty
                    ? ImmutableArray<T>.Empty
                    : right.AsImmutableArray();

                var builder = ImmutableArray.CreateBuilder<T>(leftArr.Length + rightArr.Length);
                builder.AddRange(leftArr);
                builder.AddRange(rightArr);
                return new EquatableArray<T>(builder.ToImmutable());
            });
    }
}
