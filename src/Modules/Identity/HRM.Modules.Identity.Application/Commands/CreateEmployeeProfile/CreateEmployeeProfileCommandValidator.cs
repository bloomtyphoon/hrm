using FluentValidation;

namespace HRM.Modules.Identity.Application.Commands.CreateEmployeeProfile;

public sealed class CreateEmployeeProfileCommandValidator : AbstractValidator<CreateEmployeeProfileCommand>
{
    public CreateEmployeeProfileCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required.");

        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("Employee ID is required.");

        RuleFor(x => x.DefaultScopeLevel)
            .IsInEnum().WithMessage("Invalid data scope level.");
    }
}
