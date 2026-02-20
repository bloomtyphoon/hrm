using System.Text.RegularExpressions;
using FluentValidation;

namespace HRM.Modules.Identity.Application.Commands.ResetAccountPassword;

public sealed class ResetAccountPasswordCommandValidator : AbstractValidator<ResetAccountPasswordCommand>
{
    private static readonly Regex PasswordUppercaseRegex = new(@"[A-Z]", RegexOptions.Compiled);
    private static readonly Regex PasswordLowercaseRegex = new(@"[a-z]", RegexOptions.Compiled);
    private static readonly Regex PasswordDigitRegex = new(@"[0-9]", RegexOptions.Compiled);
    private static readonly Regex PasswordSpecialCharRegex = new(@"[!@#$%^&*()_+\-=\[\]{}|;:,.<>?]", RegexOptions.Compiled);

    public ResetAccountPasswordCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(12).WithMessage("Password must be at least 12 characters long.")
            .Must(p => PasswordUppercaseRegex.IsMatch(p)).WithMessage("Password must contain at least one uppercase letter (A-Z).")
            .Must(p => PasswordLowercaseRegex.IsMatch(p)).WithMessage("Password must contain at least one lowercase letter (a-z).")
            .Must(p => PasswordDigitRegex.IsMatch(p)).WithMessage("Password must contain at least one digit (0-9).")
            .Must(p => PasswordSpecialCharRegex.IsMatch(p)).WithMessage("Password must contain at least one special character.");
    }
}
