using FluentValidation;

namespace HRM.Modules.Attendance.Application.Commands.CheckOut;

public sealed class CheckOutCommandValidator : AbstractValidator<CheckOutCommand>
{
    public CheckOutCommandValidator()
    {
        // Allow check-out up to 24 hours in the past but not in the future
        When(x => x.CheckOutTimeUtc.HasValue, () =>
        {
            RuleFor(x => x.CheckOutTimeUtc!.Value)
                .LessThanOrEqualTo(_ => DateTime.UtcNow.AddMinutes(5))
                .WithMessage("Check-out time cannot be in the future.")
                .GreaterThan(_ => DateTime.UtcNow.AddHours(-24))
                .WithMessage("Check-out time cannot be more than 24 hours in the past.");
        });

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Notes cannot exceed 500 characters.");
    }
}
