using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Application.Pagination;

namespace HRM.Modules.Attendance.Application.Queries.GetMyAttendance;

/// <summary>
/// Returns the current authenticated employee's attendance history.
/// Always scoped to Self (current user's employee records only).
/// </summary>
public sealed record GetMyAttendanceQuery : IQuery<PagedResult<AttendanceSummaryDto>>
{
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
