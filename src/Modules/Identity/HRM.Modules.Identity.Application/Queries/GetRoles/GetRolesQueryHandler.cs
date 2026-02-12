using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Application.Pagination;
using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Identity.Application.Queries.GetRoles;

/// <summary>
/// Handler for GetRolesQuery.
///
/// Filtering layers:
/// 1. Company filter — default to PrimaryCompanyId for Employee accounts
/// 2. Search/SystemRole — user-driven refinement
/// </summary>
public sealed class GetRolesQueryHandler
    : IQueryHandler<GetRolesQuery, PagedResult<RoleSummaryDto>>
{
    private readonly IIdentityQueryContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetRolesQueryHandler(
        IIdentityQueryContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<RoleSummaryDto>> Handle(
        GetRolesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Roles.AsNoTracking();

        // Layer 1: Company filter (default to PrimaryCompanyId for Employee)
        if (!request.AllCompanies)
        {
            query = await ApplyCompanyFilterAsync(query, request.CompanyId, cancellationToken);
        }

        // Layer 2: Search filter
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(r =>
                r.Name.ToLower().Contains(searchTerm) ||
                (r.Description != null && r.Description.ToLower().Contains(searchTerm)));
        }

        // Layer 2: System role filter
        if (request.IsSystemRole.HasValue)
        {
            query = query.Where(r => r.IsSystemRole == request.IsSystemRole.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(r => r.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new RoleSummaryDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsSystemRole = r.IsSystemRole,
                CompanyId = r.CompanyId,
                PermissionCount = r.Permissions.Count,
                CreatedAtUtc = r.CreatedAtUtc,
                ModifiedAtUtc = r.ModifiedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<RoleSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    /// <summary>
    /// Apply company filter.
    /// - If companyId is explicitly provided, show global + that company's roles.
    /// - If not provided and user is Employee, default to PrimaryCompanyId.
    /// - System accounts without explicit companyId see all roles.
    /// </summary>
    private async Task<IQueryable<Domain.Entities.Role>> ApplyCompanyFilterAsync(
        IQueryable<Domain.Entities.Role> query,
        Guid? companyId,
        CancellationToken cancellationToken)
    {
        var filterCompanyId = companyId;

        // Default to PrimaryCompanyId for Employee accounts
        if (!filterCompanyId.HasValue && _currentUser.IsEmployeeAccount())
        {
            filterCompanyId = await _context.EmployeeProfiles
                .AsNoTracking()
                .Where(ep => ep.AccountId == _currentUser.UserId)
                .Select(ep => ep.PrimaryCompanyId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // No company to filter by → return unfiltered (all roles)
        if (!filterCompanyId.HasValue)
        {
            return query;
        }

        // Show global roles (CompanyId = null) + roles for the target company
        var cid = filterCompanyId.Value;
        return query.Where(r => r.CompanyId == null || r.CompanyId == cid);
    }
}
