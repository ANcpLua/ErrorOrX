namespace ErrorOr;

/// <summary>Marker type representing a successful operation (200 OK).</summary>
public readonly record struct Success;

/// <summary>Marker type representing a resource creation (201 Created).</summary>
public readonly record struct Created;

/// <summary>Marker type representing a resource deletion (204 No Content).</summary>
public readonly record struct Deleted;

/// <summary>Marker type representing a resource update (204 No Content).</summary>
public readonly record struct Updated;

/// <summary>Factory for result marker types.</summary>
public static class Result
{
    /// <summary>Gets a success result marker.</summary>
    public static Success Success => default;

    /// <summary>Gets a created result marker.</summary>
    public static Created Created => default;

    /// <summary>Gets a deleted result marker.</summary>
    public static Deleted Deleted => default;

    /// <summary>Gets an updated result marker.</summary>
    public static Updated Updated => default;
}
