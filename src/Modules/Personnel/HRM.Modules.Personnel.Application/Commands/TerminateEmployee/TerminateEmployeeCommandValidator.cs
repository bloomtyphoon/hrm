using FluentValidation;

namespace HRM.Modules.Personnel.Application.Commands.TerminateEmployee;

public sealed class TerminateEmployeeCommandValidator : AbstractValidator<TerminateEmployeeCommand>
{
    public TerminateEmployeeCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.TerminationDate).NotEmpty().WithMessage("Termination date is required.");
    }
}
