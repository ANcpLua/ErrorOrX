namespace ErrorOr.Generators;

/// <summary>
///     A named argument literal for a validation attribute (e.g., MinimumLength = 1).
/// </summary>
internal readonly record struct NamedArgLiteral(string Name, string Value);

/// <summary>
///     Represents a validation attribute extracted from a property for the IValidatableInfoResolver emitter.
/// </summary>
internal readonly record struct ValidatableAttributeInfo(
    string AttributeTypeFqn,
    EquatableArray<string> ConstructorArgLiterals,
    EquatableArray<NamedArgLiteral> NamedArgLiterals);

/// <summary>
///     Represents a property on a validatable type, with its validation attribute metadata.
/// </summary>
internal readonly record struct ValidatablePropertyDescriptor(
    string Name,
    string TypeFqn,
    string DisplayName,
    EquatableArray<ValidatableAttributeInfo> ValidationAttributes);

/// <summary>
///     Represents a type that requires validation, along with its validatable properties.
/// </summary>
internal readonly record struct ValidatableTypeDescriptor(
    string TypeFqn,
    EquatableArray<ValidatablePropertyDescriptor> Properties);
