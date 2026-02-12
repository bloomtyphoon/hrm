using FluentValidation;

namespace HRM.Modules.Identity.Application.Commands.UnlockAccount;

public sealed class UnlockAccountCommandValidator : AbstractValidator<UnlockAccountCommand>
{
    public UnlockAccountCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required.");
    }
}
