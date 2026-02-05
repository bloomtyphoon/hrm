using System.Text.RegularExpressions;
using FluentValidation;

namespace HRM.Modules.Identity.Application.Commands.RegisterAccount;

/// <summary>
/// Validator for RegisterAccountCommand.
/// </summary>
public sealed class RegisterAccountCommandValidator : AbstractValidator<RegisterAccountCommand>
{
    private static readonly Regex UsernameRegex = new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);
    private static readonly Regex PasswordUppercaseRegex = new(@"[A-Z]", RegexOptions.Compiled);
    private static readonly Regex PasswordLowercaseRegex = new(@"[a-z]", RegexOptions.Compiled);
    private static readonly Regex PasswordDigitRegex = new(@"[0-9]", RegexOptions.Compiled);
    private static readonly Regex PasswordSpecialCharRegex = new(@"[!@#$%^&*()_+\-=\[\]{}|;:,.<>?]", RegexOptions.Compiled);
    private static readonly Regex PhoneNumberRegex = new(@"^\+?[0-9]{10,15}$", RegexOptions.Compiled);

    public RegisterAccountCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .Length(3, 50).WithMessage("Username must be between 3 and 50 characters.")
            .Matches(UsernameRegex).WithMessage("Username can only contain letters, numbers, underscores, and hyphens.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email address format is invalid.")
            .MaximumLength(255).WithMessage("Email address cannot exceed 255 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(12).WithMessage("Password must be at least 12 characters long.")
            .Must(p => PasswordUppercaseRegex.IsMatch(p)).WithMessage("Password must contain at least one uppercase letter (A-Z).")
            .Must(p => PasswordLowercaseRegex.IsMatch(p)).WithMessage("Password must contain at least one lowercase letter (a-z).")
            .Must(p => PasswordDigitRegex.IsMatch(p)).WithMessage("Password must contain at least one digit (0-9).")
            .Must(p => PasswordSpecialCharRegex.IsMatch(p)).WithMessage("Password must contain at least one special character (!@#$%^&*()_+-=[]{}|;:,.<>?).");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .Length(1, 200).WithMessage("Full name must be between 1 and 200 characters.");

        RuleFor(x => x.PhoneNumber)
            .Matches(PhoneNumberRegex).WithMessage("Phone number must be in valid format (10-15 digits, optional + prefix).")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }
}
