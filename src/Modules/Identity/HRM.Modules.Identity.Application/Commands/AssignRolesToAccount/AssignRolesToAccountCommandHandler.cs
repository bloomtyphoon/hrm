using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Domain.Enums;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Entities;
using HRM.Modules.Identity.Domain.Repositories;

namespace HRM.Modules.Identity.Application.Commands.AssignRolesToAccount;

internal sealed class AssignRolesToAccountCommandHandler : ICommandHandler<AssignRolesToAccountCommand>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IAccountRoleRepository _accountRoleRepository;
    private readonly IExecutionContext _executionContext;

    public AssignRolesToAccountCommandHandler(
        IAccountRepository accountRepository,
        IRoleRepository roleRepository,
        IAccountRoleRepository accountRoleRepository,
        IExecutionContext executionContext)
    {
        _accountRepository = accountRepository;
        _roleRepository = roleRepository;
        _accountRoleRepository = accountRoleRepository;
        _executionContext = executionContext;
    }

    public async Task<Result> Handle(AssignRolesToAccountCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify account exists
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
        {
            return Result.Failure(AccountErrors.NotFound(request.AccountId));
        }

        // 2. Validate and assign each role
        foreach (var roleId in request.RoleIds)
        {
            var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
            if (role is null)
            {
                return Result.Failure(RoleErrors.NotFound(roleId));
            }

            // Validate: company-scoped roles cannot be assigned to System accounts
            if (role.CompanyId.HasValue && account.AccountType == AccountType.System)
            {
                return Result.Failure(RoleErrors.CompanyRoleNotAllowedForSystemAccount(role.Name));
            }

            // Skip if already assigned
            if (await _accountRoleRepository.ExistsAsync(request.AccountId, roleId, cancellationToken))
            {
                continue;
            }

            var assignedById = _executionContext.IsAuthenticated ? _executionContext.UserId : (Guid?)null;
            var accountRole = AccountRole.Create(request.AccountId, roleId, assignedById);
            _accountRoleRepository.Add(accountRole);
        }

        return Result.Success();
    }
}
