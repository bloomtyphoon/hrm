using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Application.Pagination;
using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Application.Abstractions.Authorization;
using HRM.Modules.Identity.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Identity.Application.Queries.GetAccounts;

/// <summary>
/// Handler for GetAccountsQuery.
/// Returns paginated list of accounts with search and filter support.
///
/// Filtering layers (applied in order):
/// 1. Visibility filter — security boundary (Employee sees only assigned companies)
/// 2. Company filter — UX convenience (default = PrimaryCompanyId for Employee accounts)
/// 3. Search/Status — user-driven refinement
/// </summary>
public sealed class GetAccountsQueryHandler
    : IQueryHandler<GetAccountsQuery, PagedResult<AccountSummaryDto>>
{
    private readonly IIdentityQueryContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAccountVisibilityFilter _visibilityFilter;

    public GetAccountsQueryHandler(
        IIdentityQueryContext context,
        ICurrentUserService currentUser,
        IAccountVisibilityFilter visibilityFilter)
    {
        _context = context;
        _currentUser = currentUser;
        _visibilityFilter = visibilityFilter;
    }

    public async Task<PagedResult<AccountSummaryDto>> Handle(
        GetAccountsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Accounts.AsNoTracking();

        // Layer 1: Visibility filter (security boundary)
        var visibleAccountIds = await _visibilityFilter.GetVisibleAccountIdsAsync(cancellationToken);
        if (visibleAccountIds != null)
        {
            query = query.Where(a => visibleAccountIds.Contains(a.Id));
        }

        // Layer 2: Company filter (UX convenience)
        if (!request.AllCompanies)
        {
            query = await ApplyCompanyFilterAsync(query, request.CompanyId, cancellationToken);
        }

        // Layer 3: Search filter
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(a =>
                a.Username.ToLower().Contains(searchTerm) ||
                a.Email.ToLower().Contains(searchTerm));
        }

        // Layer 3: Status filter
        if (request.Status.HasValue)
        {
            query = query.Where(a => a.Status == request.Status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AccountSummaryDto
            {
                Id = a.Id,
                Username = a.Username,
                Email = a.Email,
                FullName = a.FullName,
                Status = a.Status,
                AccountType = a.AccountType,
                CreatedAtUtc = a.CreatedAtUtc,
                LastLoginAtUtc = a.LastLoginAtUtc
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<AccountSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    /// <summary>
    /// Apply company filter to the query.
    /// - If companyId is explicitly provided, filter by that company.
    /// - If not provided and user is Employee, default to PrimaryCompanyId.
    /// - System accounts without explicit companyId see all (no filter).
    /// </summary>
    private async Task<IQueryable<Domain.Entities.Account>> ApplyCompanyFilterAsync(
        IQueryable<Domain.Entities.Account> query,
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

        // No company to filter by → return unfiltered
        if (!filterCompanyId.HasValue)
        {
            return query;
        }

        // Get account IDs belonging to the target company
        var accountIdsInCompany = await _context.EmployeeProfiles
            .AsNoTracking()
            .Where(ep => ep.CompanyAccess.Any(ca => ca.CompanyId == filterCompanyId.Value))
            .Select(ep => ep.AccountId)
            .ToListAsync(cancellationToken);

        var accountIdSet = accountIdsInCompany.ToHashSet();

        return query.Where(a => accountIdSet.Contains(a.Id));
    }
}
