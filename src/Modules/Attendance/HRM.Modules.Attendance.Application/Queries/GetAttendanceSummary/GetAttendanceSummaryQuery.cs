using HRM.BuildingBlocks.Application.Abstractions.Queries;

namespace HRM.Modules.Attendance.Application.Queries.GetAttendanceSummary;

/// <summary>
/// Returns daily attendance summary within the date range and current user's data scope.
/// </summary>
public sealed record GetAttendanceSummaryQuery : IQuery<IReadOnlyList<AttendanceDailySummaryDto>>
{
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
}
