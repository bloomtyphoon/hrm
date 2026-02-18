using HRM.BuildingBlocks.Application.Pagination;
using HRM.Modules.Personnel.Application.Queries.GetEmployees;

namespace HRM.Modules.Personnel.Application.Queries.GetDirectReports;

/// <summary>
/// Query to retrieve paginated direct reports for a manager.
/// </summary>
public sealed record GetDirectReportsQuery : IPagedQuery<EmployeeSummaryDto>
{
    public required Guid ManagerId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
