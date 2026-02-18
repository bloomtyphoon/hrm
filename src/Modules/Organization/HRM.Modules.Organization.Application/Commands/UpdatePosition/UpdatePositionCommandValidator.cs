using FluentValidation;

namespace HRM.Modules.Organization.Application.Commands.UpdatePosition;

public sealed class UpdatePositionCommandValidator : AbstractValidator<UpdatePositionCommand>
{
    public UpdatePositionCommandValidator()
    {
        RuleFor(x => x.PositionId)
            .NotEmpty()
            .WithMessage("Position ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Position title is required.")
            .Length(1, 200)
            .WithMessage("Position title must be between 1 and 200 characters.");

        RuleFor(x => x.PositionLevel)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Position level must be non-negative.");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.MaxHeadcount)
            .GreaterThan(0)
            .WithMessage("Max headcount must be greater than 0.")
            .When(x => x.MaxHeadcount.HasValue);
    }
}
