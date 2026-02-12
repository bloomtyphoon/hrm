using FluentValidation;

namespace HRM.Modules.Identity.Application.Commands.AssignRolesToAccount;

public sealed class AssignRolesToAccountCommandValidator : AbstractValidator<AssignRolesToAccountCommand>
{
    public AssignRolesToAccountCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required.");

        RuleFor(x => x.RoleIds)
            .NotEmpty().WithMessage("At least one role ID is required.");

        RuleForEach(x => x.RoleIds)
            .NotEmpty().WithMessage("Role ID cannot be empty.");
    }
}
