using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Application.Abstractions.Authorization;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;

namespace HRM.Modules.Identity.Application.Commands.SuspendAccount;

internal sealed class SuspendAccountCommandHandler : ICommandHandler<SuspendAccountCommand>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IAccountVisibilityFilter _visibilityFilter;

    public SuspendAccountCommandHandler(
        IAccountRepository accountRepository,
        IAccountVisibilityFilter visibilityFilter)
    {
        _accountRepository = accountRepository;
        _visibilityFilter = visibilityFilter;
    }

    public async Task<Result> Handle(SuspendAccountCommand request, CancellationToken cancellationToken)
    {
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

        account.Suspend();

        return Result.Success();
    }
}
