using System.Text.RegularExpressions;
using FluentValidation;

namespace HRM.Modules.Organization.Application.Commands.CreateCompany;

/// <summary>
/// Validator for CreateCompanyCommand.
/// Validates input before handler execution.
///
/// Validation Rules:
/// 1. Code:
///    - Required
///    - Length: 1-50 characters
///    - Format: Alphanumeric with underscores and hyphens only
///    - Examples: ABC-001, COMPANY_01, HRM123
///
/// 2. Name:
///    - Required
///    - Length: 1-200 characters
///    - No format restrictions
///
/// 3. TaxId:
///    - Optional
///    - Max length: 50 characters
///    - No format restrictions (varies by country)
/// </summary>
public sealed class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    // Code: Alphanumeric with underscores and hyphens
    private static readonly Regex CodeRegex = new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

    public CreateCompanyCommandValidator()
    {
        // Code validation
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Company code is required.")
            .Length(1, 50)
            .WithMessage("Company code must be between 1 and 50 characters.")
            .Matches(CodeRegex)
            .WithMessage("Company code can only contain letters, numbers, underscores, and hyphens.");

        // Name validation
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Company name is required.")
            .Length(1, 200)
            .WithMessage("Company name must be between 1 and 200 characters.");

        // TaxId validation (optional)
        RuleFor(x => x.TaxId)
            .MaximumLength(50)
            .WithMessage("Tax ID cannot exceed 50 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.TaxId));
    }
}
