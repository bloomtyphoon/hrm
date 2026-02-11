using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Application.Pagination;
using HRM.Modules.Identity.Application.Abstractions.Authorization;
using HRM.Modules.Identity.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Identity.Application.Queries.GetAccounts;

/// <summary>
/// Handler for GetAccountsQuery.
/// Returns paginated list of accounts with search and filter support.
/// Applies visibility filtering based on current user's company assignments.
/// </summary>
public sealed class GetAccountsQueryHandler
    : IQueryHandler<GetAccountsQuery, PagedResult<AccountSummaryDto>>
{
    private readonly IIdentityQueryContext _context;
    private readonly IAccountVisibilityFilter _visibilityFilter;

    public GetAccountsQueryHandler(
        IIdentityQueryContext context,
        IAccountVisibilityFilter visibilityFilter)
    {
        _context = context;
        _visibilityFilter = visibilityFilter;
    }

    public async Task<PagedResult<AccountSummaryDto>> Handle(
        GetAccountsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Accounts.AsNoTracking();

        // Apply visibility filter (Employee accounts only see accounts in their companies)
        var visibleAccountIds = await _visibilityFilter.GetVisibleAccountIdsAsync(cancellationToken);
        if (visibleAccountIds != null)
        {
            query = query.Where(a => visibleAccountIds.Contains(a.Id));
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(a =>
                a.Username.ToLower().Contains(searchTerm) ||
                a.Email.ToLower().Contains(searchTerm));
        }

        // Apply status filter
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
}
