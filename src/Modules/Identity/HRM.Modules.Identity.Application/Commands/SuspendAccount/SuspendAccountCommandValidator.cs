using FluentValidation;

namespace HRM.Modules.Identity.Application.Commands.SuspendAccount;

public sealed class SuspendAccountCommandValidator : AbstractValidator<SuspendAccountCommand>
{
    public SuspendAccountCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required.");
    }
}
