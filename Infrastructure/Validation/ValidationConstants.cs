namespace KestrelApi.Infrastructure.Validation;

public static class ValidationConstants
{
    public const int NameMaxLength = 100;
    public const int NameMinLength = 1;
    
    public const int SecretMaxLength = 500;
    public const int SecretMinLength = 1;
    
    // Regex pattern to prevent potential injection attacks
    // Allows alphanumeric, spaces, and common punctuation
    public const string SafeInputPattern = @"^[a-zA-Z0-9\s\-_.,!?@#$%^&*()'""]+$";
}