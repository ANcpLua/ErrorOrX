namespace ErrorOr.Core.Results;

/// <summary>
/// Provides a container for external libraries to extend
/// the default `TypedResults` set with their own samples.
/// </summary>
public static class TypedResults
{
    /// <summary>
    /// Provides a container for external libraries to extend
    /// the default `TypedResults` set with their own samples.
    /// </summary>
    public static IResultExtensions Extensions { get; } = new ResultExtensions();
}

/// <summary>
/// Provides an interface to registering external methods that provide
/// custom IResult instances.
/// </summary>
public interface IResultExtensions
{
}

/// <summary>
/// Implements an interface for registering external methods that provide
/// custom IResult instances.
/// </summary>
internal sealed class ResultExtensions : IResultExtensions
{
}
