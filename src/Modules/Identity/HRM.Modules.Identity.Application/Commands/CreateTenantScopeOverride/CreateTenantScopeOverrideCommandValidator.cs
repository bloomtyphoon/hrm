using FluentValidation;

namespace HRM.Modules.Identity.Application.Commands.CreateTenantScopeOverride;

public sealed class CreateTenantScopeOverrideCommandValidator
    : AbstractValidator<CreateTenantScopeOverrideCommand>
{
    public CreateTenantScopeOverrideCommandValidator()
    {
        RuleFor(x => x.Module)
            .NotEmpty().WithMessage("Module is required.");

        RuleFor(x => x.Entity)
            .NotEmpty().WithMessage("Entity is required.");

        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("Action is required.");

        RuleFor(x => x.AllowedScopes)
            .NotEmpty().WithMessage("At least one allowed scope is required.");
    }
}
