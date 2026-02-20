using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Application.Abstractions.Authorization;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;

namespace HRM.Modules.Identity.Application.Commands.ResetAccountPassword;

internal sealed class ResetAccountPasswordCommandHandler : ICommandHandler<ResetAccountPasswordCommand>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccountVisibilityFilter _visibilityFilter;

    public ResetAccountPasswordCommandHandler(
        IAccountRepository accountRepository,
        IPasswordHasher passwordHasher,
        IAccountVisibilityFilter visibilityFilter)
    {
        _accountRepository = accountRepository;
        _passwordHasher = passwordHasher;
        _visibilityFilter = visibilityFilter;
    }

    public async Task<Result> Handle(ResetAccountPasswordCommand request, CancellationToken cancellationToken)
    {
        // Visibility check: admin can only reset passwords for accounts they can see
        var visibleAccountIds = await _visibilityFilter.GetVisibleAccountIdsAsync(cancellationToken);
        if (visibleAccountIds != null && !visibleAccountIds.Contains(request.AccountId))
        {
            return Result.Failure(AccountErrors.NotFound(request.AccountId));
        }

        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);

        if (account is null)
        {
            return Result.Failure(AccountErrors.NotFound(request.AccountId));
        }

        var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        account.UpdatePassword(newPasswordHash);

        return Result.Success();
    }
}
