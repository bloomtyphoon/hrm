using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Application.Abstractions.Data;
using HRM.Modules.Identity.Domain.Entities;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;
using HRM.Modules.Identity.Domain.Services;
using HRM.Modules.Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Identity.Application.Commands.CreateRole;

internal sealed class CreateRoleCommandHandler : ICommandHandler<CreateRoleCommand, Guid>
{
    private readonly IRoleRepository _roleRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityQueryContext _context;
    private readonly IPermissionCatalogService _catalogService;

    public CreateRoleCommandHandler(
        IRoleRepository roleRepository,
        ICurrentUserService currentUser,
        IIdentityQueryContext context,
        IPermissionCatalogService catalogService)
    {
        _roleRepository = roleRepository;
        _currentUser = currentUser;
        _context = context;
        _catalogService = catalogService;
    }

    public async Task<Result<Guid>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        // 1. Employee access check
        if (_currentUser.IsEmployeeAccount())
        {
            // Employee cannot create global roles
            if (!request.CompanyId.HasValue)
            {
                return Result.Failure<Guid>(RoleErrors.NotFound(Guid.Empty));
            }

            // Employee can only create roles for their assigned companies
            var hasAccess = await _context.EmployeeProfiles
                .AsNoTracking()
                .Where(ep => ep.AccountId == _currentUser.UserId)
                .AnyAsync(ep => ep.CompanyAccess.Any(ca => ca.CompanyId == request.CompanyId.Value),
                    cancellationToken);

            if (!hasAccess)
            {
                return Result.Failure<Guid>(RoleErrors.NotFound(Guid.Empty));
            }
        }

        // 2. Check name uniqueness within same company scope
        if (await _roleRepository.ExistsByNameAsync(request.Name, request.CompanyId, cancellationToken))
        {
            return Result.Failure<Guid>(RoleErrors.NameAlreadyExists(request.Name));
        }

        // 3. Resolve tenant and validate permissions against tenant-aware catalog
        var tenantId = _currentUser.TenantId
            ?? throw new InvalidOperationException("TenantId is required to create a role.");

        var validationError = await ValidatePermissionsAsync(tenantId, request.Permissions);
        if (validationError is not null)
        {
            return Result.Failure<Guid>(validationError);
        }

        // 4. Create Role aggregate
        var role = Role.Create(tenantId, request.Name, request.Description, request.IsSystemRole, request.CompanyId);

        // 5. Add permissions
        var permissions = request.Permissions
            .Select(p => RolePermission.Create(p.Module, p.Entity, p.Action, p.Scope))
            .ToList();

        role.AddPermissions(permissions);

        // 6. Finalize creation (raises domain event)
        role.FinalizeCreation();

        // 7. Persist
        _roleRepository.Add(role);

        return Result.Success(role.Id);
    }

    private async Task<DomainError?> ValidatePermissionsAsync(Guid tenantId, List<PermissionDto> permissions)
    {
        foreach (var p in permissions)
        {
            if (!await _catalogService.ExistsAsync(p.Module, p.Entity, p.Action))
            {
                return RoleErrors.PermissionNotInCatalog(p.Module, p.Entity, p.Action);
            }

            if (p.Scope.HasValue)
            {
                var action = await _catalogService.GetActionAsync(tenantId, p.Module, p.Entity, p.Action);
                if (action is not null && action.HasScopes() && !action.AllowsScope(p.Scope.Value))
                {
                    return RoleErrors.ScopeNotAllowed(
                        $"{p.Module}.{p.Entity}.{p.Action}",
                        p.Scope.Value.ToString());
                }
            }
        }

        return null;
    }
}
