using System.Text.RegularExpressions;
using FluentValidation;

namespace HRM.Modules.Organization.Application.Commands.CreatePosition;

public sealed class CreatePositionCommandValidator : AbstractValidator<CreatePositionCommand>
{
    private static readonly Regex CodeRegex = new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

    public CreatePositionCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage("Company ID is required.");

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Position code is required.")
            .Length(1, 50)
            .WithMessage("Position code must be between 1 and 50 characters.")
            .Matches(CodeRegex)
            .WithMessage("Position code can only contain letters, numbers, underscores, and hyphens.");

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
