using System.Diagnostics.CodeAnalysis;

namespace ProblemDetailsAutoValidation;

/// <summary>
///     Indicates that a property, parameter, or type should not be validated.
/// </summary>
/// <remarks>
///     When applied to a property, validation is skipped for that property.
///     When applied to a parameter, validation is skipped for that parameter.
///     When applied to a type, validation is skipped for all properties and parameters of that type.
///     This includes skipping validation of nested properties for complex types.
/// </remarks>
[Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class SkipValidationAttribute : Attribute
{
}