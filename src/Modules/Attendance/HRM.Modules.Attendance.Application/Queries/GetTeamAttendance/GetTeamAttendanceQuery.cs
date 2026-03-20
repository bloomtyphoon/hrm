using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Application.Pagination;
using HRM.Modules.Attendance.Application.Queries.GetMyAttendance;

namespace HRM.Modules.Attendance.Application.Queries.GetTeamAttendance;

/// <summary>
/// Returns attendance records for all employees within the current user's data scope.
/// </summary>
public sealed record GetTeamAttendanceQuery : IQuery<PagedResult<AttendanceSummaryDto>>
{
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
