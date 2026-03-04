using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Application.Abstractions.Authorization;
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
    private readonly IAccountVisibilityFilter _visibilityFilter;

    public AssignRolesToAccountCommandHandler(
        IAccountRepository accountRepository,
        IRoleRepository roleRepository,
        IAccountRoleRepository accountRoleRepository,
        IExecutionContext executionContext,
        IAccountVisibilityFilter visibilityFilter)
    {
        _accountRepository = accountRepository;
        _roleRepository = roleRepository;
        _accountRoleRepository = accountRoleRepository;
        _executionContext = executionContext;
        _visibilityFilter = visibilityFilter;
    }

    public async Task<Result> Handle(AssignRolesToAccountCommand request, CancellationToken cancellationToken)
    {
        // Visibility check
        var visibleAccountIds = await _visibilityFilter.GetVisibleAccountIdsAsync(cancellationToken);
        if (visibleAccountIds != null && !visibleAccountIds.Contains(request.AccountId))
        {
            return Result.Failure(AccountErrors.NotFound(request.AccountId));
        }

        // 1. Verify account exists
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
        {
            return Result.Failure(AccountErrors.NotFound(request.AccountId));
        }

        // 2. Batch-load all requested roles and existing assignments
        var roles = await _roleRepository.GetByIdsAsync(request.RoleIds, cancellationToken);
        var existingAssignments = await _accountRoleRepository.GetByAccountIdAsync(request.AccountId, cancellationToken);
        var existingRoleIds = existingAssignments.Select(ar => ar.RoleId).ToHashSet();

        // Validate and assign each role
        foreach (var roleId in request.RoleIds)
        {
            var role = roles.FirstOrDefault(r => r.Id == roleId);
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
            if (existingRoleIds.Contains(roleId))
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
