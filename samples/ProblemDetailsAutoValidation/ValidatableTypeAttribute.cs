using System.Diagnostics.CodeAnalysis;

namespace ProblemDetailsAutoValidation;

/// <summary>
///     Indicates that a type is validatable to support discovery by the
///     validations generator.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
[Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public sealed class ValidatableTypeAttribute : Attribute
{
}