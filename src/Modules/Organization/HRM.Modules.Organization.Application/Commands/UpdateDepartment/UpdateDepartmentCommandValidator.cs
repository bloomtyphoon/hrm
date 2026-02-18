using FluentValidation;

namespace HRM.Modules.Organization.Application.Commands.UpdateDepartment;

public sealed class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentCommandValidator()
    {
        RuleFor(x => x.DepartmentId)
            .NotEmpty()
            .WithMessage("Department ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Department name is required.")
            .Length(1, 200)
            .WithMessage("Department name must be between 1 and 200 characters.");
    }
}
