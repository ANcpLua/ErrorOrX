namespace ErrorOr.Generators;

/// <summary>
///     Type-safe class representing a parameter binding source.
///     Inspired by ASP.NET Core's <c>BindingSource</c>.
/// </summary>
internal sealed class ParameterSource : IEquatable<ParameterSource>
{
    // Request-based sources
    public static readonly ParameterSource Route = new("Route");
    public static readonly ParameterSource Body = new("Body");
    public static readonly ParameterSource Query = new("Query");
    public static readonly ParameterSource Header = new("Header");
    public static readonly ParameterSource Form = new("Form");
    public static readonly ParameterSource FormFile = new("FormFile");
    public static readonly ParameterSource FormFiles = new("FormFiles");
    public static readonly ParameterSource FormCollection = new("FormCollection");
    public static readonly ParameterSource Stream = new("Stream");
    public static readonly ParameterSource PipeReader = new("PipeReader");
    public static readonly ParameterSource Service = new("Service");
    public static readonly ParameterSource KeyedService = new("KeyedService");
    public static readonly ParameterSource HttpContext = new("HttpContext");
    public static readonly ParameterSource CancellationToken = new("CancellationToken");
    public static readonly ParameterSource AsParameters = new("AsParameters");

    private ParameterSource(string id)
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
