using FluentValidation;

namespace HRM.Modules.Personnel.Application.Commands.EndAssignment;

public sealed class EndAssignmentCommandValidator : AbstractValidator<EndAssignmentCommand>
{
    public EndAssignmentCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.AssignmentId).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty().WithMessage("End date is required.");
    }
}
