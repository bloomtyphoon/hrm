using HRM.BuildingBlocks.Application.Pagination;
using HRM.Modules.Identity.Domain.Enums;

namespace HRM.Modules.Identity.Application.Queries.GetAccounts;

/// <summary>
/// Query to retrieve paginated list of accounts.
/// Supports search by username/email and filter by status and company.
///
/// Company filter behavior:
/// - CompanyId specified: filter accounts belonging to that company
/// - AllCompanies = true: show all visible accounts (no company filter)
/// - Neither (default): Employee accounts default to PrimaryCompanyId,
///   System accounts see all
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
