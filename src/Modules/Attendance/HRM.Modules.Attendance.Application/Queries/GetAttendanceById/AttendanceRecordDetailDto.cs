namespace HRM.Modules.Attendance.Application.Queries.GetAttendanceById;

public sealed record AttendanceRecordDetailDto
{
    public required Guid Id { get; init; }
    public required Guid EmployeeId { get; init; }
    public required DateOnly Date { get; init; }
    public required DateTime CheckInTimeUtc { get; init; }
    public DateTime? CheckOutTimeUtc { get; init; }
    public required string Status { get; init; }
    public required bool IsManualEntry { get; init; }
    public string? Notes { get; init; }
    public Guid? CompanyId { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public DateTime? ModifiedAtUtc { get; init; }
}
