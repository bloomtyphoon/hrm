using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Application.Abstractions.Data;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;
using HRM.Modules.Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Identity.Application.Commands.UpdateRole;

internal sealed class UpdateRoleCommandHandler : ICommandHandler<UpdateRoleCommand>
{
    private readonly IRoleRepository _roleRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityQueryContext _context;

    public UpdateRoleCommandHandler(
        IRoleRepository roleRepository,
        ICurrentUserService currentUser,
        IIdentityQueryContext context)
    {
        _roleRepository = roleRepository;
        _currentUser = currentUser;
        _context = context;
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

        // 4. Update name and description
        role.Update(request.Name, request.Description);

        // 5. Replace permissions
        var permissions = request.Permissions
            .Select(p => RolePermission.Create(p.Module, p.Entity, p.Action, p.Scope))
            .ToList();

        role.SetPermissions(permissions);

        return Result.Success();
    }
}
