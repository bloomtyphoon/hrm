using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Application.Abstractions.Data;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Identity.Application.Queries.GetRoleById;

/// <summary>
/// Handler for GetRoleByIdQuery.
/// Validates company access — Employee accounts can only view
/// global roles or roles for their assigned companies.
/// </summary>
public sealed class GetRoleByIdQueryHandler
    : IQueryHandler<GetRoleByIdQuery, Result<RoleDetailDto>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityQueryContext _context;

    public GetRoleByIdQueryHandler(
        IRoleRepository roleRepository,
        ICurrentUserService currentUser,
        IIdentityQueryContext context)
    {
        _roleRepository = roleRepository;
        _currentUser = currentUser;
        _context = context;
    }

    public async Task<Result<RoleDetailDto>> Handle(
        GetRoleByIdQuery request,
        CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);

        if (role is null)
        {
            return Result.Failure<RoleDetailDto>(RoleErrors.NotFound(request.RoleId));
        }

        // Company access check: Employee can only see global or own-company roles
        if (role.CompanyId.HasValue && _currentUser.IsEmployeeAccount())
        {
            var hasAccess = await _context.EmployeeProfiles
                .AsNoTracking()
                .Where(ep => ep.AccountId == _currentUser.UserId)
                .AnyAsync(ep => ep.CompanyAccess.Any(ca => ca.CompanyId == role.CompanyId.Value),
                    cancellationToken);

            if (!hasAccess)
            {
                return Result.Failure<RoleDetailDto>(RoleErrors.NotFound(request.RoleId));
            }
        }

        var dto = new RoleDetailDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            CompanyId = role.CompanyId,
            PermissionCount = role.PermissionCount,
            Permissions = role.Permissions.Select(p => new RolePermissionDto
            {
                Module = p.Module,
                Entity = p.Entity,
                Action = p.Action,
                Scope = p.Scope,
                PermissionKey = p.PermissionKey
            }).ToList(),
            CreatedAtUtc = role.CreatedAtUtc,
            ModifiedAtUtc = role.ModifiedAtUtc
        };

        return Result.Success(dto);
    }
}
