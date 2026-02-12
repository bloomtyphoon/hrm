using HRM.BuildingBlocks.Application.Pagination;

namespace HRM.Modules.Identity.Application.Queries.GetRoles;

/// <summary>
/// Query to retrieve paginated list of roles.
///
/// Company filter behavior differs by account type:
///
/// System account:
/// - CompanyId specified: global roles + roles for that company
/// - AllCompanies = true (or default): all roles (global + all companies)
///
/// Employee account (AllCompanies is ignored):
/// - CompanyId specified: ONLY roles for that company (no global)
/// - Default: ONLY roles for PrimaryCompanyId (no global)
/// - Never sees global roles (CompanyId = null)
/// </summary>
public sealed record GetRolesQuery : IPagedQuery<RoleSummaryDto>
{
    public string? SearchTerm { get; init; }
    public bool? IsSystemRole { get; init; }
    public Guid? CompanyId { get; init; }
    public bool AllCompanies { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
