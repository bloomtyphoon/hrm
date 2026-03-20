namespace HRM.Modules.Attendance.Application.Queries.GetLeaveRequests;

public sealed record LeaveRequestDto
{
    public required Guid Id { get; init; }
    public required Guid EmployeeId { get; init; }
    public required Guid LeaveTypeId { get; init; }
    public required string LeaveTypeName { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
    public required int TotalDays { get; init; }
    public string? Reason { get; init; }
    public required string Status { get; init; }
    public Guid? ApprovedByEmployeeId { get; init; }
    public DateTime? DecisionDateUtc { get; init; }
    public string? DecisionNotes { get; init; }
}
