using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Application.Abstractions.Data;
using HRM.Modules.Identity.Domain.Errors;
using Microsoft.EntityFrameworkCore;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Application.Security;

namespace HRM.Modules.Identity.Application.Queries.GetAccountRoles;

public sealed class GetAccountRolesQueryHandler
    : IQueryHandler<GetAccountRolesQuery, Result<List<AccountRoleDto>>>
{
    private readonly IIdentityQueryContext _context;
    private readonly IDataScopeService _dataScopeService;
    private readonly ICurrentUserService _currentUser;

    public GetAccountRolesQueryHandler(
        IIdentityQueryContext context,
        IDataScopeService dataScopeService,
        ICurrentUserService currentUser)
    {
        _context = context;
        _dataScopeService = dataScopeService;
        _currentUser = currentUser;
    }

    public async Task<Result<List<AccountRoleDto>>> Handle(
        GetAccountRolesQuery request,
        CancellationToken cancellationToken)
    {
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _currentUser.UserId, IdentityPermissions.Account.View, cancellationToken);
        if (!await AccountScopeFilter.IsAccessibleAsync(rule, _currentUser.UserId, request.AccountId, _context, cancellationToken))
        {
            return Result.Failure<List<AccountRoleDto>>(AccountErrors.NotFound(request.AccountId));
        }

        // Verify account exists
        var accountExists = await _context.Accounts
            .AsNoTracking()
            .AnyAsync(a => a.Id == request.AccountId, cancellationToken);

        if (!accountExists)
        {
            return Result.Failure<List<AccountRoleDto>>(AccountErrors.NotFound(request.AccountId));
        }

        var rolesQuery = _context.Roles.AsNoTracking().AsQueryable();

        // Company filter: when CompanyId is provided, return roles for that company + global roles
        if (request.CompanyId.HasValue)
        {
            rolesQuery = rolesQuery.Where(r => r.CompanyId == request.CompanyId.Value || r.CompanyId == null);
        }

        var roles = await _context.AccountRoles
            .AsNoTracking()
            .Where(ar => ar.AccountId == request.AccountId)
            .Join(
                rolesQuery,
                ar => ar.RoleId,
                r => r.Id,
                (ar, r) => new AccountRoleDto
                {
                    RoleId = r.Id,
                    RoleName = r.Name,
                    RoleDescription = r.Description,
                    IsSystemRole = r.IsSystemRole,
                    CompanyId = r.CompanyId,
                    PermissionCount = r.Permissions.Count,
                    AssignedAtUtc = ar.AssignedAtUtc,
                    AssignedById = ar.AssignedById
                })
            .OrderBy(r => r.RoleName)
            .ToListAsync(cancellationToken);

        return Result.Success(roles);
    }
}
