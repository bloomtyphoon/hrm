using FluentValidation;

namespace HRM.Modules.Attendance.Application.Commands.UpdateAttendance;

public sealed class UpdateAttendanceCommandValidator
    : AbstractValidator<UpdateAttendanceCommand>
{
    public UpdateAttendanceCommandValidator()
    {
        RuleFor(x => x.RecordId)
            .NotEmpty()
            .WithMessage("Record ID is required.");

        RuleFor(x => x.CheckInTimeUtc)
            .NotEmpty()
            .WithMessage("Check-in time is required.");

        RuleFor(x => x.CheckOutTimeUtc)
            .GreaterThan(x => x.CheckInTimeUtc)
            .When(x => x.CheckOutTimeUtc.HasValue)
            .WithMessage("Check-out time must be after check-in time.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Notes cannot exceed 500 characters.");
    }
}
