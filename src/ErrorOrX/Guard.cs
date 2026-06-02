namespace ErrorOr;

/// <summary>
///     Minimal null-guard helper. Replaces the single <c>Guard.NotNull</c> usage that previously
///     pulled in ANcpLua.Roslyn.Utilities — a Roslyn-heavy package whose Microsoft.CodeAnalysis
///     dependency leaked into every runtime consumer and clashed with their own Roslyn versions.
/// </summary>
internal static class Guard
{
    /// <summary>Throws <see cref="System.ArgumentNullException" /> if <paramref name="value" /> is null; otherwise returns it.</summary>
    public static T NotNull<T>(T value,
        [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value is null)
        {
            throw new System.ArgumentNullException(paramName);
        }

        return value;
    }
}
