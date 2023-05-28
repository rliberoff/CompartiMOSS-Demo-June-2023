using System.ComponentModel.DataAnnotations;

namespace CompartiMOSS.Demo.Web.Infrastructure;

/// <summary>
/// Provides a more specific validation for <see cref="string"/> types, checking it is not null, empty or whitespace.
/// </summary>
/// <remarks>
/// This is attribute complements <see cref="RequiredAttribute"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
internal sealed class NotEmptyOrWhitespaceAttribute : ValidationAttribute
{
    /// <inheritdoc/>
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success;
        }

        if (value is string s)
        {
            return !string.IsNullOrWhiteSpace(s)
                ? ValidationResult.Success
                : new ValidationResult($@"'{validationContext.MemberName}' cannot be empty or whitespace");
        }

        return new ValidationResult($@"'{validationContext.MemberName}' must be a string!");
    }
}
