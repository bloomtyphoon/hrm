using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Application.Abstractions.Data;
using HRM.Modules.Identity.Application.Security;

namespace HRM.Modules.Identity.Application.Commands.RemoveRolesFromAccount;

internal sealed class RemoveRolesFromAccountCommandHandler : ICommandHandler<RemoveRolesFromAccountCommand>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IAccountRoleRepository _accountRoleRepository;
    private readonly IDataScopeService _dataScopeService;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityQueryContext _queryContext;

    public RemoveRolesFromAccountCommandHandler(
        IAccountRepository accountRepository,
        IAccountRoleRepository accountRoleRepository,
        IDataScopeService dataScopeService,
        ICurrentUserService currentUser,
        IIdentityQueryContext queryContext)
    {
        _accountRepository = accountRepository;
        _accountRoleRepository = accountRoleRepository;
        _dataScopeService = dataScopeService;
        _currentUser = currentUser;
        _queryContext = queryContext;
    }

    public async Task<Result> Handle(RemoveRolesFromAccountCommand request, CancellationToken cancellationToken)
    {
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _currentUser.UserId, IdentityPermissions.Account.View, cancellationToken);
        if (!await AccountScopeFilter.IsAccessibleAsync(rule, _currentUser.UserId, request.AccountId, _queryContext, cancellationToken))
        {
            return Result.Failure(AccountErrors.NotFound(request.AccountId));
        }

        // 1. Verify account exists
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
        {
            return Result.Failure(AccountErrors.NotFound(request.AccountId));
        }

        // 2. Remove each role assignment
        foreach (var roleId in request.RoleIds)
        {
            var accountRole = await _accountRoleRepository.GetAsync(request.AccountId, roleId, cancellationToken);
            if (accountRole is null)
            {
                return Result.Failure(RoleErrors.NotFound(roleId));
            }

            _accountRoleRepository.Remove(accountRole);
        }

        return Result.Success();
    }
}
