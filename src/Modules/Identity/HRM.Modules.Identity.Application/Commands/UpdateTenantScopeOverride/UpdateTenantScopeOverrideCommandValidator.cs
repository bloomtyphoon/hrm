using FluentValidation;

namespace HRM.Modules.Identity.Application.Commands.UpdateTenantScopeOverride;

public sealed class UpdateTenantScopeOverrideCommandValidator
    : AbstractValidator<UpdateTenantScopeOverrideCommand>
{
    public UpdateTenantScopeOverrideCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Override ID is required.");

        RuleFor(x => x.AllowedScopes)
            .NotEmpty().WithMessage("At least one allowed scope is required.");
    }
}
