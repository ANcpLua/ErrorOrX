namespace ErrorOr;

/// <summary>
///     Indicates that an empty request body is allowed for this parameter.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class AllowEmptyBodyAttribute : Attribute;
