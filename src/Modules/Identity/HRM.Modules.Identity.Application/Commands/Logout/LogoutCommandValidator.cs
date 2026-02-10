using FluentValidation;

namespace HRM.Modules.Identity.Application.Commands.Logout;

/// <summary>
/// Validator for LogoutCommand.
/// </summary>
public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required.");
    }
}
