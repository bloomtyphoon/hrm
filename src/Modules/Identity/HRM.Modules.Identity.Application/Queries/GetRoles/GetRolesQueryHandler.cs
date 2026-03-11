using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Application.Pagination;
using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Identity.Application.Queries.GetRoles;

/// <summary>
/// Handler for GetRolesQuery.
///
/// Access rules:
/// - System: sees all roles (global + all companies). AllCompanies/CompanyId for UX filtering.
/// - Employee: sees ONLY company-scoped roles for assigned companies. Never sees global roles.
///   AllCompanies is ignored — always filtered by company.
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

        // Company filter — different rules per account type
        query = _currentUser.IsSystemAccount()
            ? ApplySystemCompanyFilter(query, request)
            : await ApplyEmployeeCompanyFilterAsync(query, request.CompanyId, cancellationToken);

        // Search filter
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var pattern = $"%{request.SearchTerm}%";
            query = query.Where(r =>
                EF.Functions.Like(r.Name, pattern) ||
                (r.Description != null && EF.Functions.Like(r.Description, pattern)));
        }

        // System role filter
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
    /// System account: sees everything.
    /// - AllCompanies = true or no CompanyId → all roles (global + all companies)
    /// - CompanyId specified → global + that company's roles
    /// </summary>
    private static IQueryable<Domain.Entities.Role> ApplySystemCompanyFilter(
        IQueryable<Domain.Entities.Role> query,
        GetRolesQuery request)
    {
        if (request.AllCompanies || !request.CompanyId.HasValue)
        {
            return query;
        }

        var cid = request.CompanyId.Value;
        return query.Where(r => r.CompanyId == null || r.CompanyId == cid);
    }

    /// <summary>
    /// Employee account: ONLY sees company-scoped roles for assigned companies.
    /// Global roles (CompanyId = null) are never visible to Employee.
    /// AllCompanies is ignored — always filtered by company.
    /// Default to PrimaryCompanyId when no CompanyId specified.
    /// </summary>
    private async Task<IQueryable<Domain.Entities.Role>> ApplyEmployeeCompanyFilterAsync(
        IQueryable<Domain.Entities.Role> query,
        Guid? companyId,
        CancellationToken cancellationToken)
    {
        var filterCompanyId = companyId;

        // Default to PrimaryCompanyId
        if (!filterCompanyId.HasValue)
        {
            filterCompanyId = await _context.EmployeeProfiles
                .AsNoTracking()
                .Where(ep => ep.AccountId == _currentUser.UserId)
                .Select(ep => ep.PrimaryCompanyId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // No company resolved → empty result (Employee must belong to a company)
        if (!filterCompanyId.HasValue)
        {
            return query.Where(_ => false);
        }

        // Only company-scoped roles for this company (NO global roles)
        var cid = filterCompanyId.Value;
        return query.Where(r => r.CompanyId == cid);
    }
}
