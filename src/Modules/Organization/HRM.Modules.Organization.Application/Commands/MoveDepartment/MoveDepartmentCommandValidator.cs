using FluentValidation;

namespace HRM.Modules.Organization.Application.Commands.MoveDepartment;

public sealed class MoveDepartmentCommandValidator : AbstractValidator<MoveDepartmentCommand>
{
    public MoveDepartmentCommandValidator()
    {
        RuleFor(x => x.DepartmentId)
            .NotEmpty()
            .WithMessage("Department ID is required.");
    }
}
