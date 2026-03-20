namespace HRM.Modules.Attendance.Application.Queries.GetAttendanceSummary;

/// <summary>
/// Daily attendance summary for dashboard views.
/// </summary>
public sealed record AttendanceDailySummaryDto
{
    public required DateOnly Date { get; init; }
    public required int TotalEmployees { get; init; }
    public required int CheckedIn { get; init; }
    public required int CheckedOut { get; init; }
    public required int ManualEntries { get; init; }
}
