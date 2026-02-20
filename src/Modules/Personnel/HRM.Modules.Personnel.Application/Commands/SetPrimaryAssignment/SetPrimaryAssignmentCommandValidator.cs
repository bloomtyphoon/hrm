using FluentValidation;

namespace HRM.Modules.Personnel.Application.Commands.SetPrimaryAssignment;

public sealed class SetPrimaryAssignmentCommandValidator : AbstractValidator<SetPrimaryAssignmentCommand>
{
    public SetPrimaryAssignmentCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.AssignmentId).NotEmpty();
    }
}
