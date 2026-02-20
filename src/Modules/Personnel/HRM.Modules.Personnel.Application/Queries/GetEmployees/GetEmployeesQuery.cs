using HRM.BuildingBlocks.Application.Pagination;
using HRM.Modules.Personnel.Domain.Entities;

namespace HRM.Modules.Personnel.Application.Queries.GetEmployees;

/// <summary>
/// Query to retrieve paginated list of employees with optional filtering.
/// </summary>
public sealed record GetEmployeesQuery : IPagedQuery<EmployeeSummaryDto>
{
    public string? SearchTerm { get; init; }
    public EmploymentStatus? Status { get; init; }
    public Guid? CompanyId { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? ManagerId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
