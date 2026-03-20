using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Application.Pagination;

namespace HRM.Modules.Attendance.Application.Queries.GetShiftAssignments;

public sealed record GetShiftAssignmentsQuery : IQuery<PagedResult<ShiftAssignmentDto>>
{
    public Guid? EmployeeId { get; init; }
    public Guid? ShiftId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
