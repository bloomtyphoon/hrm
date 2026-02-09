using HRM.BuildingBlocks.Application.Pagination;

namespace HRM.Modules.Identity.Application.Queries.GetRoles;

/// <summary>
/// Query to retrieve paginated list of roles.
/// </summary>
public sealed record GetRolesQuery : IPagedQuery<RoleSummaryDto>
{
    public string? SearchTerm { get; init; }
    public bool? IsSystemRole { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
