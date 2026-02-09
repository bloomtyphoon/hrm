using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Application.Abstractions.Data;
using HRM.Modules.Identity.Domain.Errors;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Identity.Application.Queries.GetAccountRoles;

public sealed class GetAccountRolesQueryHandler
    : IQueryHandler<GetAccountRolesQuery, Result<List<AccountRoleDto>>>
{
    private readonly IIdentityQueryContext _context;

    public GetAccountRolesQueryHandler(IIdentityQueryContext context)
    {
        _context = context;
    }

    public async Task<Result<List<AccountRoleDto>>> Handle(
        GetAccountRolesQuery request,
        CancellationToken cancellationToken)
    {
        // Verify account exists
        var accountExists = await _context.Accounts
            .AsNoTracking()
            .AnyAsync(a => a.Id == request.AccountId, cancellationToken);

        if (!accountExists)
        {
            return Result.Failure<List<AccountRoleDto>>(AccountErrors.NotFound(request.AccountId));
        }

        var roles = await _context.AccountRoles
            .AsNoTracking()
            .Where(ar => ar.AccountId == request.AccountId)
            .Join(
                _context.Roles.AsNoTracking(),
                ar => ar.RoleId,
                r => r.Id,
                (ar, r) => new AccountRoleDto
                {
                    RoleId = r.Id,
                    RoleName = r.Name,
                    RoleDescription = r.Description,
                    IsSystemRole = r.IsSystemRole,
                    PermissionCount = r.Permissions.Count,
                    AssignedAtUtc = ar.AssignedAtUtc,
                    AssignedById = ar.AssignedById
                })
            .OrderBy(r => r.RoleName)
            .ToListAsync(cancellationToken);

        return Result.Success(roles);
    }
}
