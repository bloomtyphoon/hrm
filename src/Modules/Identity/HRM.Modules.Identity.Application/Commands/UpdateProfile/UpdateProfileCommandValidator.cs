using System.Text.RegularExpressions;
using FluentValidation;

namespace HRM.Modules.Identity.Application.Commands.UpdateProfile;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    private static readonly Regex PhoneNumberRegex = new(@"^\+?[0-9]{10,15}$", RegexOptions.Compiled);

    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .Length(1, 200).WithMessage("Full name must be between 1 and 200 characters.");

        RuleFor(x => x.PhoneNumber)
            .Matches(PhoneNumberRegex).WithMessage("Phone number must be in valid format (10-15 digits, optional + prefix).")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }
}
