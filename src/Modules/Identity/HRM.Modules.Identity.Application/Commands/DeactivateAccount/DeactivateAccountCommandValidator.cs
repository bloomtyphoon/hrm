using FluentValidation;

namespace HRM.Modules.Identity.Application.Commands.DeactivateAccount;

public sealed class DeactivateAccountCommandValidator : AbstractValidator<DeactivateAccountCommand>
{
    public DeactivateAccountCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required.");
    }
}
