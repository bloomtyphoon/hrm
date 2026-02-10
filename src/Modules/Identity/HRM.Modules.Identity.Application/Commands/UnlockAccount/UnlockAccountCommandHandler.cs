using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;

namespace HRM.Modules.Identity.Application.Commands.UnlockAccount;

internal sealed class UnlockAccountCommandHandler : ICommandHandler<UnlockAccountCommand>
{
    private readonly IAccountRepository _accountRepository;

    public UnlockAccountCommandHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<Result> Handle(UnlockAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);

        if (account is null)
        {
            return Result.Failure(AccountErrors.NotFound(request.AccountId));
        }

        if (!account.IsLocked())
        {
            return Result.Failure(AccountErrors.AccountNotLocked);
        }

        account.ForceUnlock();

        return Result.Success();
    }
}
