using FluentValidation;

namespace HRM.Modules.Personnel.Application.Commands.AddAssignment;

public sealed class AddAssignmentCommandValidator : AbstractValidator<AddAssignmentCommand>
{
    public AddAssignmentCommandValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty()
            .WithMessage("Employee ID is required.");

        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage("Company ID is required.");

        RuleFor(x => x.DepartmentId)
            .NotEmpty()
            .WithMessage("Department ID is required.");

        RuleFor(x => x.PositionId)
            .NotEmpty()
            .WithMessage("Position ID is required.");
    }
}
