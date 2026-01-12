using System.ComponentModel.DataAnnotations;

namespace ErrorOrX.Tests.Generators;

/// <summary>
///     Tests for BCL validation detection in the ErrorOr generator.
///     Verifies that types with ValidationAttribute descendants or IValidatableObject
///     are correctly detected as requiring validation.
/// </summary>
public class BclValidationTests
{
    [Fact]
    public void RequiresValidation_Attribute_IsBaseClass()
    {
        // Verify that ValidationAttribute is the base class for Required, Range, etc.
        typeof(RequiredAttribute).IsSubclassOf(typeof(ValidationAttribute)).Should().BeTrue();
        typeof(RangeAttribute).IsSubclassOf(typeof(ValidationAttribute)).Should().BeTrue();
        typeof(StringLengthAttribute).IsSubclassOf(typeof(ValidationAttribute)).Should().BeTrue();
        typeof(MinLengthAttribute).IsSubclassOf(typeof(ValidationAttribute)).Should().BeTrue();
        typeof(MaxLengthAttribute).IsSubclassOf(typeof(ValidationAttribute)).Should().BeTrue();
        typeof(RegularExpressionAttribute).IsSubclassOf(typeof(ValidationAttribute)).Should().BeTrue();
        typeof(EmailAddressAttribute).IsSubclassOf(typeof(ValidationAttribute)).Should().BeTrue();
        typeof(UrlAttribute).IsSubclassOf(typeof(ValidationAttribute)).Should().BeTrue();
        typeof(CompareAttribute).IsSubclassOf(typeof(ValidationAttribute)).Should().BeTrue();
    }

    [Fact]
    public void BclValidator_WorksWithValidationAttributes()
    {
        // Verify BCL Validator.TryValidateObject works as expected
        var model = new TestModelWithRequired { Name = null };
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, context, results, validateAllProperties: true);

        isValid.Should().BeFalse("Model with null required field should fail validation");
        results.Should().ContainSingle(static r => r.MemberNames.Contains("Name"));
    }

    [Fact]
    public void BclValidator_PassesForValidModel()
    {
        var model = new TestModelWithRequired { Name = "Valid Name" };
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, context, results, validateAllProperties: true);

        isValid.Should().BeTrue("Model with valid data should pass validation");
        results.Should().BeEmpty();
        model.Name.Should().Be("Valid Name", "property value should be preserved after validation");
    }

    [Fact]
    public void BclValidator_WorksWithIValidatableObject()
    {
        var model = new TestModelWithIValidatableObject { Value = -1 };
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, context, results, validateAllProperties: true);

        isValid.Should().BeFalse("IValidatableObject.Validate returning errors should fail");
        results.Should().ContainSingle(static r => r.ErrorMessage != null && r.ErrorMessage.Contains("positive"));
    }

    [Fact]
    public void BclValidator_WorksWithMultipleValidationErrors()
    {
        var model = new TestModelWithMultipleValidations
        {
            Name = "", // Required, MinLength(3)
            Email = "invalid-email", // EmailAddress
            Age = 150 // Range(0, 120)
        };
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, context, results, validateAllProperties: true);

        isValid.Should().BeFalse();
        results.Should().HaveCountGreaterThan(1, "Multiple validation errors should be reported");

        // Verify property values are preserved (validation doesn't mutate the model)
        model.Name.Should().BeEmpty();
        model.Email.Should().Be("invalid-email");
        model.Age.Should().Be(150);
    }

    // Test models for validation
    // Note: Properties use nullable types since we're testing BCL validation which catches null values.
    // Getters are accessed via reflection by Validator.TryValidateObject.
    private sealed class TestModelWithRequired
    {
        [Required]
        public string? Name { get; set; }
    }

    private sealed class TestModelWithIValidatableObject : IValidatableObject
    {
        public int Value { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Value < 0)
                yield return new ValidationResult("Value must be positive", [nameof(Value)]);
        }
    }

    private sealed class TestModelWithMultipleValidations
    {
        [Required]
        [MinLength(3)]
        public string? Name { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Range(0, 120)]
        public int Age { get; set; }
    }
}