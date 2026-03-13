using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Application.Pagination;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Application.Abstractions.Data;
using HRM.Modules.Identity.Application.Security;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Identity.Application.Queries.GetAccounts;

/// <summary>
/// Handler for GetAccountsQuery.
///
/// Access rules:
/// - System: sees all accounts. AllCompanies/CompanyId for UX filtering only.
/// - Employee: sees ONLY accounts in assigned companies. AllCompanies is ignored.
///   Default to PrimaryCompanyId when no CompanyId specified.
///
/// Filtering layers:
/// 1. Data scope rule — security boundary (via IDataScopeService)
/// 2. Company filter — scoped by account type rules above
/// 3. Search/Status — user-driven refinement
/// </summary>
public sealed class GetAccountsQueryHandler
    : IQueryHandler<GetAccountsQuery, PagedResult<AccountSummaryDto>>
{
    private readonly IIdentityQueryContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDataScopeService _dataScopeService;

    public GetAccountsQueryHandler(
        IIdentityQueryContext context,
        ICurrentUserService currentUser,
        IDataScopeService dataScopeService)
    {
        _context = context;
        _currentUser = currentUser;
        _dataScopeService = dataScopeService;
    }

    public async Task<PagedResult<AccountSummaryDto>> Handle(
        GetAccountsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var query = _context.Accounts.AsNoTracking();

        // Layer 1: Data scope security boundary
        var rule = await _dataScopeService.GetScopeRuleAsync(
            userId, IdentityPermissions.Account.View, cancellationToken);
        query = AccountScopeFilter.ApplyScope(query, rule, userId, _context);

        // Layer 2: Company filter — different rules per account type
        query = rule.Level == DataScopeLevel.Global
            ? ApplySystemCompanyFilter(query, request)
            : await ApplyEmployeeCompanyFilterAsync(query, request.CompanyId, cancellationToken);

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
    /// System account: sees everything.
    /// - AllCompanies = true or no CompanyId → all accounts
    /// - CompanyId specified → accounts in that company only
    /// </summary>
    private IQueryable<Domain.Entities.Account> ApplySystemCompanyFilter(
        IQueryable<Domain.Entities.Account> query,
        GetAccountsQuery request)
    {
        if (request.AllCompanies || !request.CompanyId.HasValue)
        {
            return query;
        }

        return FilterByCompany(query, request.CompanyId.Value);
    }

    /// <summary>
    /// Employee account: ONLY sees accounts in assigned companies.
    /// AllCompanies is ignored — always filtered by company.
    /// Default to PrimaryCompanyId when no CompanyId specified.
    /// </summary>
    private async Task<IQueryable<Domain.Entities.Account>> ApplyEmployeeCompanyFilterAsync(
        IQueryable<Domain.Entities.Account> query,
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

        // No company resolved → only own account
        if (!filterCompanyId.HasValue)
        {
            var userId = _currentUser.UserId;
            return query.Where(a => a.Id == userId);
        }

        return FilterByCompany(query, filterCompanyId.Value);
    }

    private IQueryable<Domain.Entities.Account> FilterByCompany(
        IQueryable<Domain.Entities.Account> query,
        Guid companyId)
    {
        var accountIdsInCompany = _context.EmployeeProfiles
            .AsNoTracking()
            .Where(ep => ep.CompanyAccess.Any(ca => ca.CompanyId == companyId))
            .Select(ep => ep.AccountId);

        return query.Where(a => accountIdsInCompany.Contains(a.Id));
    }
}
