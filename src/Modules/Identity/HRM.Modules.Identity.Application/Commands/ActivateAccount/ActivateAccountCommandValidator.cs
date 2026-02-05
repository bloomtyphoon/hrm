using FluentValidation;

namespace HRM.Modules.Identity.Application.Commands.ActivateAccount;

/// <summary>
/// Validator for ActivateAccountCommand.
/// </summary>
public sealed class ActivateAccountCommandValidator : AbstractValidator<ActivateAccountCommand>
{
    public ActivateAccountCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account ID is required.");
    }
}
