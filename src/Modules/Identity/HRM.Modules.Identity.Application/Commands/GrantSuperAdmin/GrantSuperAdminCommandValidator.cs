using FluentValidation;

namespace HRM.Modules.Identity.Application.Commands.GrantSuperAdmin;

public sealed class GrantSuperAdminCommandValidator : AbstractValidator<GrantSuperAdminCommand>
{
    public GrantSuperAdminCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required.");
    }
}
