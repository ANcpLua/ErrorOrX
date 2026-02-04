namespace ErrorOr.Generators;

/// <summary>
///     Type-safe class representing a parameter binding source.
///     Inspired by ASP.NET Core's <c>BindingSource</c>.
/// </summary>
internal sealed class ParameterSource : IEquatable<ParameterSource>
{
    // Request-based sources
    public static readonly ParameterSource Route = new("Route",
        true, false, false);

    public static readonly ParameterSource Body = new("Body",
        true, true, false);

    public static readonly ParameterSource Query = new("Query",
        true, false, false);

    public static readonly ParameterSource Header = new("Header",
        true, false, false);

    public static readonly ParameterSource Form = new("Form",
        true, false, false);

    public static readonly ParameterSource FormFile = new("FormFile",
        true, false, false);

    public static readonly ParameterSource FormFiles = new("FormFiles",
        true, false, false);

    public static readonly ParameterSource FormCollection = new("FormCollection",
        true, false, false);

    public static readonly ParameterSource Stream = new("Stream",
        true, false, false);

    public static readonly ParameterSource PipeReader = new("PipeReader",
        true, false, false);

    // DI-based sources
    public static readonly ParameterSource Service = new("Service",
        false, false, false);

    public static readonly ParameterSource KeyedService = new("KeyedService",
        false, false, false);

    // Special types (auto-bound by ASP.NET Core)
    public static readonly ParameterSource HttpContext = new("HttpContext",
        false, false, true);

    public static readonly ParameterSource CancellationToken = new("CancellationToken",
        false, false, true);

    // Composite binding
    public static readonly ParameterSource AsParameters = new("AsParameters",
        true, false, false, true);

    private ParameterSource(
        string id,
        bool _, // isFromRequest - reserved for future use
        bool __, // requiresJsonContext - reserved for future use
        bool ___, // isSpecialType - reserved for future use
        bool ____ = false) // isComposite - reserved for future use
    {
        Id = id;
    }

    /// <summary>Gets the unique identifier for this source.</summary>
    private string Id { get; }

    /// <summary>Gets whether this source binds from form-related data.</summary>
    public bool IsFormRelated => this == Form || this == FormFile || this == FormFiles || this == FormCollection;

    public bool Equals(ParameterSource? other)
    {
        return other?.Id == Id;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ParameterSource);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public override string ToString()
    {
        return Id;
    }

    public static bool operator ==(ParameterSource? left, ParameterSource? right)
    {
        return left?.Equals(right) ?? right is null;
    }

    public static bool operator !=(ParameterSource? left, ParameterSource? right)
    {
        return !(left == right);
    }
}
