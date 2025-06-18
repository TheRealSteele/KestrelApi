using System.ComponentModel.DataAnnotations;
using KestrelApi.Infrastructure.Validation;

namespace KestrelApi.Names;

public class NameRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(ValidationConstants.NameMaxLength, 
        MinimumLength = ValidationConstants.NameMinLength,
        ErrorMessage = "Name must be between {2} and {1} characters")]
    [RegularExpression(ValidationConstants.SafeInputPattern,
        ErrorMessage = "Name contains invalid characters")]
    public string Name { get; set; } = string.Empty;
}