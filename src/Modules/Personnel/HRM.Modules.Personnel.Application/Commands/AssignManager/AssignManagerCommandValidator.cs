using FluentValidation;

namespace HRM.Modules.Personnel.Application.Commands.AssignManager;

public sealed class AssignManagerCommandValidator : AbstractValidator<AssignManagerCommand>
{
    public AssignManagerCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ManagerId).NotEmpty();
        RuleFor(x => x).Must(x => x.EmployeeId != x.ManagerId)
            .WithMessage("Employee cannot be their own manager.");
    }
}
