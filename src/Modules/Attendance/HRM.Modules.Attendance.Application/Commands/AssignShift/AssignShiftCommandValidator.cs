using FluentValidation;

namespace HRM.Modules.Attendance.Application.Commands.AssignShift;

public sealed class AssignShiftCommandValidator
    : AbstractValidator<AssignShiftCommand>
{
    public AssignShiftCommandValidator()
    {
        RuleFor(x => x.ShiftId)
            .NotEmpty().WithMessage("Shift ID is required.");

        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("Employee ID is required.");

        RuleFor(x => x.EffectiveFrom)
            .NotEmpty().WithMessage("Effective from date is required.");

        RuleFor(x => x.EffectiveTo)
            .GreaterThanOrEqualTo(x => x.EffectiveFrom)
            .When(x => x.EffectiveTo.HasValue)
            .WithMessage("Effective to date must be on or after effective from date.");
    }
}
