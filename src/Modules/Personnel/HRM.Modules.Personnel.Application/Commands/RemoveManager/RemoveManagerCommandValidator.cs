using FluentValidation;

namespace HRM.Modules.Personnel.Application.Commands.RemoveManager;

public sealed class RemoveManagerCommandValidator : AbstractValidator<RemoveManagerCommand>
{
    public RemoveManagerCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
    }
}
