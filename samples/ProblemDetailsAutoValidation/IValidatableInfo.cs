using System.Diagnostics.CodeAnalysis;

namespace ProblemDetailsAutoValidation;

/// <summary>
///     Represents an object that can be validated.
/// </summary>
[Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public interface IValidatableInfo
{
    /// <summary>
    ///     Validates the object asynchronously.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous validation operation.</returns>
    Task ValidateAsync(object? value, ValidateContext context, CancellationToken cancellationToken);
}
