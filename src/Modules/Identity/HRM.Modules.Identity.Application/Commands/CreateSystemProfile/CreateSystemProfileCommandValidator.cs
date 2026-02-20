using FluentValidation;

namespace HRM.Modules.Identity.Application.Commands.CreateSystemProfile;

public sealed class CreateSystemProfileCommandValidator : AbstractValidator<CreateSystemProfileCommand>
{
    public CreateSystemProfileCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required.");

        RuleFor(x => x.Department)
            .MaximumLength(200).WithMessage("Department must not exceed 200 characters.")
            .When(x => x.Department is not null);

        RuleFor(x => x.JobTitle)
            .MaximumLength(200).WithMessage("Job title must not exceed 200 characters.")
            .When(x => x.JobTitle is not null);
    }
}
