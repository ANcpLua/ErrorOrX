using ANcpLua.Roslyn.Utilities.Models;
using Microsoft.CodeAnalysis;

namespace ErrorOr.Generators;

/// <summary>
///     Bridges the cache-safe <see cref="LocationInfo" /> snapshots stored on pipeline models back to
///     Roslyn <see cref="Location" /> values in the output stage, where no symbols are available.
/// </summary>
internal static class LocationInfoExtensions
{
    /// <summary>
    ///     Converts the snapshot to a path-based <see cref="Location" />, or <see cref="Location.None" />
    ///     when the snapshot is default (no source location was captured in the transform stage).
    /// </summary>
    public static Location ToLocationOrNone(this LocationInfo info)
    {
        return info == default ? Location.None : info.ToLocation();
    }
}
