using HRM.BuildingBlocks.Application.Pagination;

namespace HRM.Modules.Identity.Application.Queries.GetRoles;

/// <summary>
/// Query to retrieve paginated list of roles.
///
/// Company filter behavior:
/// - CompanyId specified: show global roles + roles for that company
/// - AllCompanies = true: show all roles (no company filter)
/// - Neither (default): Employee accounts default to PrimaryCompanyId,
///   System accounts see all
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
