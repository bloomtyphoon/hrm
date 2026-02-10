using FluentValidation;

namespace HRM.Modules.Identity.Application.Commands.RevokeAllSessionsExceptCurrent;

/// <summary>
/// Validator for RevokeAllSessionsExceptCurrentCommand.
/// </summary>
public sealed class RevokeAllSessionsExceptCurrentCommandValidator : AbstractValidator<RevokeAllSessionsExceptCurrentCommand>
{
    public RevokeAllSessionsExceptCurrentCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account ID is required.");

        RuleFor(x => x.CurrentRefreshToken)
            .NotEmpty()
            .WithMessage("Current refresh token is required.");
    }
}
