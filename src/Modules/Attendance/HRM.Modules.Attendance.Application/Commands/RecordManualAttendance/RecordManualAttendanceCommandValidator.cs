using FluentValidation;

namespace HRM.Modules.Attendance.Application.Commands.RecordManualAttendance;

public sealed class RecordManualAttendanceCommandValidator
    : AbstractValidator<RecordManualAttendanceCommand>
{
    public RecordManualAttendanceCommandValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty()
            .WithMessage("Employee ID is required.");

        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("Date is required.")
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Cannot record attendance for a future date.");

        RuleFor(x => x.CheckInTimeUtc)
            .NotEmpty()
            .WithMessage("Check-in time is required.");

        RuleFor(x => x.CheckOutTimeUtc)
            .NotEmpty()
            .WithMessage("Check-out time is required.")
            .GreaterThan(x => x.CheckInTimeUtc)
            .WithMessage("Check-out time must be after check-in time.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Notes cannot exceed 500 characters.");
    }
}
