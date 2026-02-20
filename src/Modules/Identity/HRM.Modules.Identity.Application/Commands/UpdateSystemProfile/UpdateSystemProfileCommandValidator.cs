using FluentValidation;

namespace HRM.Modules.Identity.Application.Commands.UpdateSystemProfile;

public sealed class UpdateSystemProfileCommandValidator : AbstractValidator<UpdateSystemProfileCommand>
{
    public UpdateSystemProfileCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required.");

        RuleFor(x => x.Department)
            .MaximumLength(200).WithMessage("Department must not exceed 200 characters.")
            .When(x => x.Department is not null);

        RuleFor(x => x.JobTitle)
            .MaximumLength(200).WithMessage("Job title must not exceed 200 characters.")
            .When(x => x.JobTitle is not null);

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters.")
            .When(x => x.Notes is not null);
    }
}
