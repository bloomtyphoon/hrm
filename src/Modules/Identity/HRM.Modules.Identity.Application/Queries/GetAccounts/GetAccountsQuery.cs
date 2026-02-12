using HRM.BuildingBlocks.Application.Pagination;
using HRM.Modules.Identity.Domain.Enums;

namespace HRM.Modules.Identity.Application.Queries.GetAccounts;

/// <summary>
/// Query to retrieve paginated list of accounts.
///
/// Company filter behavior differs by account type:
///
/// System account:
/// - CompanyId specified: filter accounts belonging to that company
/// - AllCompanies = true (or default): show all accounts
///
/// Employee account (AllCompanies is ignored):
/// - CompanyId specified: filter by that company (must be in assigned companies)
/// - Default: filter by PrimaryCompanyId
/// - Never sees accounts outside assigned companies
/// </summary>
public sealed record GetAccountsQuery : IPagedQuery<AccountSummaryDto>
{
    public string? SearchTerm { get; init; }
    public AccountStatus? Status { get; init; }
    public Guid? CompanyId { get; init; }
    public bool AllCompanies { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
