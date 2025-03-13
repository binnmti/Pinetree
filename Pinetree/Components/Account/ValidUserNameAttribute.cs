using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Pinetree.Components.Account;

public class ValidUserNameAttribute : ValidationAttribute
{
    private static readonly Regex UserNameRegex = new Regex(@"^[a-zA-Z][a-zA-Z0-9-_]*$", RegexOptions.Compiled);

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is string userName)
        {
            if (!UserNameRegex.IsMatch(userName))
            {
                return new ValidationResult("UserName must start with a letter and can only contain letters, numbers, hyphens, and underscores.");
            }
        }
        return ValidationResult.Success;
    }
}