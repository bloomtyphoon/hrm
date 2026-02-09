using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;

namespace HRM.Modules.Identity.Application.Commands.ChangePassword;

internal sealed class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordCommandHandler(
        IAccountRepository accountRepository,
        IPasswordHasher passwordHasher)
    {
        _accountRepository = accountRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);

        if (account is null)
        {
            return Result.Failure(AccountErrors.NotFound(request.AccountId));
        }

        // If not admin reset, verify current password
        if (!request.IsAdminReset)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            {
                return Result.Failure(new ValidationError(
                    "Account.CurrentPasswordRequired",
                    "Current password is required when changing your own password."));
            }

            if (!_passwordHasher.VerifyPassword(request.CurrentPassword, account.PasswordHash))
            {
                return Result.Failure(AccountErrors.InvalidCredentials);
            }
        }

        var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        account.UpdatePassword(newPasswordHash);

        return Result.Success();
    }
}
