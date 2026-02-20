using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Application.Abstractions.Data;
using HRM.Modules.Identity.Domain.Entities;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;
using HRM.Modules.Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Identity.Application.Commands.CreateRole;

internal sealed class CreateRoleCommandHandler : ICommandHandler<CreateRoleCommand, Guid>
{
    private readonly IRoleRepository _roleRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityQueryContext _context;

    public CreateRoleCommandHandler(
        IRoleRepository roleRepository,
        ICurrentUserService currentUser,
        IIdentityQueryContext context)
    {
        _roleRepository = roleRepository;
        _currentUser = currentUser;
        _context = context;
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

        // 3. Create Role aggregate
        var role = Role.Create(request.Name, request.Description, request.IsSystemRole, request.CompanyId);

        // 4. Add permissions
        var permissions = request.Permissions
            .Select(p => RolePermission.Create(p.Module, p.Entity, p.Action, p.Scope))
            .ToList();

        role.AddPermissions(permissions);

        // 5. Finalize creation (raises domain event)
        role.FinalizeCreation();

        // 6. Persist
        _roleRepository.Add(role);

        return Result.Success(role.Id);
    }
}
