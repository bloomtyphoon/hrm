using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;

namespace HRM.Modules.Identity.Application.Commands.RemoveRolesFromAccount;

internal sealed class RemoveRolesFromAccountCommandHandler : ICommandHandler<RemoveRolesFromAccountCommand>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IAccountRoleRepository _accountRoleRepository;

    public RemoveRolesFromAccountCommandHandler(
        IAccountRepository accountRepository,
        IAccountRoleRepository accountRoleRepository)
    {
        _accountRepository = accountRepository;
        _accountRoleRepository = accountRoleRepository;
    }

    public async Task<Result> Handle(RemoveRolesFromAccountCommand request, CancellationToken cancellationToken)
    {
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
