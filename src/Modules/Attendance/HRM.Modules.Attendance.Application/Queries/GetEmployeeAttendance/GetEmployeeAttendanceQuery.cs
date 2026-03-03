using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Application.Pagination;
using HRM.Modules.Attendance.Application.Queries.GetMyAttendance;

namespace HRM.Modules.Attendance.Application.Queries.GetEmployeeAttendance;

/// <summary>
/// Returns attendance history for a specific employee.
/// Access is scope-checked: manager sees subordinates, HR sees company, admin sees all.
/// </summary>
public sealed record GetEmployeeAttendanceQuery : IQuery<PagedResult<AttendanceSummaryDto>>
{
    public required Guid EmployeeId { get; init; }
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
