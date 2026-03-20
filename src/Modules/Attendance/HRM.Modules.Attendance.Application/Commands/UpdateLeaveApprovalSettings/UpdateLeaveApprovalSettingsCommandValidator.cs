using FluentValidation;

namespace HRM.Modules.Attendance.Application.Commands.UpdateLeaveApprovalSettings;

internal sealed class UpdateLeaveApprovalSettingsCommandValidator
    : AbstractValidator<UpdateLeaveApprovalSettingsCommand>
{
    public UpdateLeaveApprovalSettingsCommandValidator()
    {
        RuleFor(x => x.MaxApprovalLevels)
            .InclusiveBetween(1, 5)
            .WithMessage("Max approval levels must be between 1 and 5.");

        RuleFor(x => x.AutoApproveIfDaysLessThanOrEqual)
            .GreaterThanOrEqualTo(0)
            .When(x => x.AutoApproveIfDaysLessThanOrEqual.HasValue)
            .WithMessage("Auto-approve days threshold must be non-negative.");
    }
}
