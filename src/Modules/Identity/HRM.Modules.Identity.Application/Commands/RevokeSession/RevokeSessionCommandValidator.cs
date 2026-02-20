using FluentValidation;

namespace HRM.Modules.Identity.Application.Commands.RevokeSession;

/// <summary>
/// Validator for RevokeSessionCommand.
/// </summary>
public sealed class RevokeSessionCommandValidator : AbstractValidator<RevokeSessionCommand>
{
    public RevokeSessionCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("Session ID is required.");

        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account ID is required.");
    }
}
