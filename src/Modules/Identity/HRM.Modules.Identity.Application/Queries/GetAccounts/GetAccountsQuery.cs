using HRM.BuildingBlocks.Application.Pagination;
using HRM.Modules.Identity.Domain.Enums;

namespace HRM.Modules.Identity.Application.Queries.GetAccounts;

/// <summary>
/// Query to retrieve paginated list of accounts.
/// Supports search by username/email and filter by status.
/// </summary>
public sealed record GetAccountsQuery : IPagedQuery<AccountSummaryDto>
{
    public string? SearchTerm { get; init; }
    public AccountStatus? Status { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
