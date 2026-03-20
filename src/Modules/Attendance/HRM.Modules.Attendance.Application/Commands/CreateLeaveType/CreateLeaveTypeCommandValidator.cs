using FluentValidation;

namespace HRM.Modules.Attendance.Application.Commands.CreateLeaveType;

public sealed class CreateLeaveTypeCommandValidator
    : AbstractValidator<CreateLeaveTypeCommand>
{
    public CreateLeaveTypeCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Leave type name is required.")
            .MaximumLength(100).WithMessage("Leave type name cannot exceed 100 characters.");

        RuleFor(x => x.DefaultDaysPerYear)
            .GreaterThanOrEqualTo(0).WithMessage("Default days per year must be zero or greater.")
            .LessThanOrEqualTo(365).WithMessage("Default days per year cannot exceed 365.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
    }
}
