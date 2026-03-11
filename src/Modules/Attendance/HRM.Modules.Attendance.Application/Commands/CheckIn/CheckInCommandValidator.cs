using FluentValidation;

namespace HRM.Modules.Attendance.Application.Commands.CheckIn;

public sealed class CheckInCommandValidator : AbstractValidator<CheckInCommand>
{
    public CheckInCommandValidator()
    {
        // Allow check-in up to 24 hours in the past but not in the future
        When(x => x.CheckInTimeUtc.HasValue, () =>
        {
            RuleFor(x => x.CheckInTimeUtc!.Value)
                .LessThanOrEqualTo(_ => DateTime.UtcNow.AddMinutes(5))
                .WithMessage("Check-in time cannot be in the future.")
                .GreaterThan(_ => DateTime.UtcNow.AddHours(-24))
                .WithMessage("Check-in time cannot be more than 24 hours in the past.");
        });

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Notes cannot exceed 500 characters.");
    }
}
