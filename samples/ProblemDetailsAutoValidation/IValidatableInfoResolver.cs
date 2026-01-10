using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ProblemDetailsAutoValidation;

/// <summary>
///     Provides an interface for resolving the validation information associated
///     with a given <seealso cref="Type" /> or <seealso cref="ParameterInfo" />.
/// </summary>
[Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public interface IValidatableInfoResolver
{
    /// <summary>
    ///     Gets validation information for the specified type.
    /// </summary>
    /// <param name="type">The type to get validation information for.</param>
    /// <param name="validatableInfo">
    ///     When this method returns, contains the validatable information if found.
    /// </param>
    /// <returns><see langword="true" /> if the validatable type information was found; otherwise, <see langword="false" />.</returns>
    bool TryGetValidatableTypeInfo(Type type, [NotNullWhen(true)] out IValidatableInfo? validatableInfo);

    /// <summary>
    ///     Gets validation information for the specified parameter.
    /// </summary>
    /// <param name="parameterInfo">The parameter to get validation information for.</param>
    /// <param name="validatableInfo">When this method returns, contains the validatable information if found.</param>
    /// <returns>
    ///     <see langword="true" /> if the validatable parameter information was found; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    bool TryGetValidatableParameterInfo(ParameterInfo parameterInfo,
        [NotNullWhen(true)] out IValidatableInfo? validatableInfo);
}