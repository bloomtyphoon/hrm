using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Application.Abstractions.Data;
using HRM.Modules.Identity.Application.Commands.CreateRole;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;
using HRM.Modules.Identity.Domain.Services;
using HRM.Modules.Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Identity.Application.Commands.UpdateRole;

internal sealed class UpdateRoleCommandHandler : ICommandHandler<UpdateRoleCommand>
{
    private readonly IRoleRepository _roleRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityQueryContext _context;
    private readonly IPermissionCatalogService _catalogService;

    public UpdateRoleCommandHandler(
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

    public async Task<Result> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        // 1. Get role
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure(RoleErrors.NotFound(request.RoleId));
        }

        // 2. Employee access check — same rules as GetRoleById
        if (_currentUser.IsEmployeeAccount())
        {
            // Employee cannot modify global roles
            if (!role.CompanyId.HasValue)
            {
                return Result.Failure(RoleErrors.NotFound(request.RoleId));
            }

            // Employee can only modify roles for their assigned companies
            var hasAccess = await _context.EmployeeProfiles
                .AsNoTracking()
                .Where(ep => ep.AccountId == _currentUser.UserId)
                .AnyAsync(ep => ep.CompanyAccess.Any(ca => ca.CompanyId == role.CompanyId.Value),
                    cancellationToken);

            if (!hasAccess)
            {
                return Result.Failure(RoleErrors.NotFound(request.RoleId));
            }
        }

        // 3. Check name uniqueness within same company scope (exclude current role)
        if (await _roleRepository.ExistsByNameAsync(request.Name, role.CompanyId, request.RoleId, cancellationToken))
        {
            return Result.Failure(RoleErrors.NameAlreadyExists(request.Name));
        }

        // 4. Validate permissions against tenant-aware catalog
        var tenantId = _currentUser.TenantId
            ?? throw new InvalidOperationException("TenantId is required to update a role.");

        var validationError = await ValidatePermissionsAsync(tenantId, request.Permissions);
        if (validationError is not null)
        {
            return Result.Failure(validationError);
        }

        // 5. Update name and description
        role.Update(request.Name, request.Description);

        // 6. Replace permissions
        var permissions = request.Permissions
            .Select(p => RolePermission.Create(p.Module, p.Entity, p.Action, p.Scope))
            .ToList();

        role.SetPermissions(permissions);

        return Result.Success();
    }

    private async Task<DomainError?> ValidatePermissionsAsync(Guid tenantId, List<PermissionDto> permissions)
    {
        foreach (var p in permissions)
        {
            if (!await _catalogService.ExistsAsync(p.Module, p.Entity, p.Action))
            {
                return RoleErrors.PermissionNotInCatalog(p.Module, p.Entity, p.Action);
            }

            if (p.Scope is not null)
            {
                var action = await _catalogService.GetActionAsync(tenantId, p.Module, p.Entity, p.Action);
                if (action is not null && action.HasScopes() && !action.AllowsScope(p.Scope))
                {
                    return RoleErrors.ScopeNotAllowed(
                        $"{p.Module}.{p.Entity}.{p.Action}",
                        p.Scope.ToString());
                }
            }
        }

        return null;
    }
}
