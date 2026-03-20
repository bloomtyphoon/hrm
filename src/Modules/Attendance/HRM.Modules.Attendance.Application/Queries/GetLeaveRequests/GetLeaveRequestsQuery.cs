using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Application.Pagination;

namespace HRM.Modules.Attendance.Application.Queries.GetLeaveRequests;

public sealed record GetLeaveRequestsQuery : IQuery<PagedResult<LeaveRequestDto>>
{
    public Guid? EmployeeId { get; init; }
    public string? Status { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
