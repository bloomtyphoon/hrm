using FluentValidation;

namespace HRM.Modules.Identity.Application.Commands.UpdateRole;

public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role name is required.")
            .Length(3, 100).WithMessage("Role name must be between 3 and 100 characters.");

        RuleFor(x => x.Permissions)
            .NotEmpty().WithMessage("At least one permission is required.");

        RuleForEach(x => x.Permissions).ChildRules(permission =>
        {
            permission.RuleFor(p => p.Module)
                .NotEmpty().WithMessage("Permission module is required.");

            permission.RuleFor(p => p.Entity)
                .NotEmpty().WithMessage("Permission entity is required.");

            permission.RuleFor(p => p.Action)
                .NotEmpty().WithMessage("Permission action is required.");
        });
    }
}
