using FluentValidation;

namespace HRM.Modules.Organization.Application.Commands.UpdateCompany;

public sealed class UpdateCompanyCommandValidator : AbstractValidator<UpdateCompanyCommand>
{
    public UpdateCompanyCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage("Company ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Company name is required.")
            .Length(1, 200)
            .WithMessage("Company name must be between 1 and 200 characters.");

        RuleFor(x => x.TaxId)
            .MaximumLength(50)
            .WithMessage("Tax ID cannot exceed 50 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.TaxId));
    }
}
