using FluentValidation;

namespace HRM.Modules.Identity.Application.Commands.UpdateEmployeeProfile;

public sealed class UpdateEmployeeProfileCommandValidator : AbstractValidator<UpdateEmployeeProfileCommand>
{
    public UpdateEmployeeProfileCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required.");

        RuleFor(x => x.DefaultScopeLevel)
            .IsInEnum().WithMessage("Invalid data scope level.");
    }
}
