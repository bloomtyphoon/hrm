using System.Text.RegularExpressions;
using FluentValidation;

namespace HRM.Modules.Organization.Application.Commands.CreateDepartment;

public sealed class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    private static readonly Regex CodeRegex = new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

    public CreateDepartmentCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage("Company ID is required.");

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Department code is required.")
            .Length(1, 50)
            .WithMessage("Department code must be between 1 and 50 characters.")
            .Matches(CodeRegex)
            .WithMessage("Department code can only contain letters, numbers, underscores, and hyphens.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Department name is required.")
            .Length(1, 200)
            .WithMessage("Department name must be between 1 and 200 characters.");
    }
}
