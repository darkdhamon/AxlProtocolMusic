using System.ComponentModel.DataAnnotations;

namespace AxlProtocolMusic.WebApp.Validation;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class NonDefaultDateAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is DateTime date && date != default)
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(
            ErrorMessage ?? $"{validationContext.DisplayName} is required.");
    }
}
