using System.Text.RegularExpressions;
using FluentValidation;

namespace HRM.Modules.Personnel.Application.Commands.CreateEmployee;

public sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    private static readonly Regex EmployeeCodeRegex = new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.EmployeeCode)
            .NotEmpty()
            .WithMessage("Employee code is required.")
            .Length(1, 50)
            .WithMessage("Employee code must be between 1 and 50 characters.")
            .Matches(EmployeeCodeRegex)
            .WithMessage("Employee code can only contain letters, numbers, underscores, and hyphens.");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required.")
            .Length(1, 100)
            .WithMessage("First name must be between 1 and 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required.")
            .Length(1, 100)
            .WithMessage("Last name must be between 1 and 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Email address format is invalid.")
            .MaximumLength(255)
            .WithMessage("Email address cannot exceed 255 characters.");

        RuleFor(x => x.Phone)
            .MaximumLength(20)
            .WithMessage("Phone number cannot exceed 20 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}
