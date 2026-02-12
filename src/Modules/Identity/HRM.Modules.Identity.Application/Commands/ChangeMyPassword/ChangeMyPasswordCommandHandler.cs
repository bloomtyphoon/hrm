using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;

namespace HRM.Modules.Identity.Application.Commands.ChangeMyPassword;

internal sealed class ChangeMyPasswordCommandHandler : ICommandHandler<ChangeMyPasswordCommand>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUser;

    public ChangeMyPasswordCommandHandler(
        IAccountRepository accountRepository,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUser)
    {
        _accountRepository = accountRepository;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(ChangeMyPasswordCommand request, CancellationToken cancellationToken)
    {
        // Self-change only: AccountId must match current user
        if (_currentUser.UserId != request.AccountId)
        {
            return Result.Failure(AccountErrors.NotFound(request.AccountId));
        }

        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);

        if (account is null)
        {
            return Result.Failure(AccountErrors.NotFound(request.AccountId));
        }

        // Verify current password
        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, account.PasswordHash))
        {
            return Result.Failure(AccountErrors.InvalidCredentials);
        }

        var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        account.UpdatePassword(newPasswordHash);

        return Result.Success();
    }
}
