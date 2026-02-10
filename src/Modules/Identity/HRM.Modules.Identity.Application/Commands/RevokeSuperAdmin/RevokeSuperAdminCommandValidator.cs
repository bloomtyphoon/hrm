using FluentValidation;

namespace HRM.Modules.Identity.Application.Commands.RevokeSuperAdmin;

public sealed class RevokeSuperAdminCommandValidator : AbstractValidator<RevokeSuperAdminCommand>
{
    public RevokeSuperAdminCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required.");
    }
}
