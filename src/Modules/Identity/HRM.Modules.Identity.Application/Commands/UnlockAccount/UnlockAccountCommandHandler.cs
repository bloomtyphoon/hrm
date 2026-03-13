using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Application.Abstractions.Data;
using HRM.Modules.Identity.Application.Security;

namespace HRM.Modules.Identity.Application.Commands.UnlockAccount;

internal sealed class UnlockAccountCommandHandler : ICommandHandler<UnlockAccountCommand>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IDataScopeService _dataScopeService;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityQueryContext _queryContext;

    public UnlockAccountCommandHandler(
        IAccountRepository accountRepository,
        IDataScopeService dataScopeService,
        ICurrentUserService currentUser,
        IIdentityQueryContext queryContext)
    {
        _accountRepository = accountRepository;
        _dataScopeService = dataScopeService;
        _currentUser = currentUser;
        _queryContext = queryContext;
    }

    public async Task<Result> Handle(UnlockAccountCommand request, CancellationToken cancellationToken)
    {
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _currentUser.UserId, IdentityPermissions.Account.View, cancellationToken);
        if (!await AccountScopeFilter.IsAccessibleAsync(rule, _currentUser.UserId, request.AccountId, _queryContext, cancellationToken))
        {
            return Result.Failure(AccountErrors.NotFound(request.AccountId));
        }

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
