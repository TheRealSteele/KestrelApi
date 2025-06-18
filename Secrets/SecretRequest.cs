using System.ComponentModel.DataAnnotations;
using KestrelApi.Infrastructure.Validation;

namespace KestrelApi.Secrets;

public class SecretRequest
{
    [Required(ErrorMessage = "Secret is required")]
    [StringLength(ValidationConstants.SecretMaxLength, 
        MinimumLength = ValidationConstants.SecretMinLength,
        ErrorMessage = "Secret must be between {2} and {1} characters")]
    [RegularExpression(ValidationConstants.SafeInputPattern,
        ErrorMessage = "Secret contains invalid characters")]
    public string Secret { get; set; } = string.Empty;
}