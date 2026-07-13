using System.Text.RegularExpressions;

namespace UsersApi.Application.Validators;

public static class UserValidator
{
    private static readonly Regex EmailRegex =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public static ValidationResult ValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return ValidationResult.Fail("Email is required.");
        if (!EmailRegex.IsMatch(email))
            return ValidationResult.Fail("Email format is invalid.");
        return ValidationResult.Success();
    }

    public static ValidationResult ValidatePassword(string? password)
    {
        var errors = new List<string>();
        if (string.IsNullOrEmpty(password))
            return ValidationResult.Fail("Password is required.");
        if (password.Length < 8)
            errors.Add("Password must be at least 8 characters long.");
        if (!password.Any(char.IsLetter))
            errors.Add("Password must contain letters.");
        if (!password.Any(char.IsDigit))
            errors.Add("Password must contain numbers.");
        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            errors.Add("Password must contain special characters.");
        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Fail(errors.ToArray());
    }

    public static ValidationResult ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ValidationResult.Fail("Name is required.");
        return ValidationResult.Success();
    }

    public static ValidationResult ValidateRegistration(string? name, string? email, string? password)
    {
        var errors = new List<string>();
        errors.AddRange(ValidateName(name).Errors);
        errors.AddRange(ValidateEmail(email).Errors);
        errors.AddRange(ValidatePassword(password).Errors);
        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Fail(errors.ToArray());
    }
}
