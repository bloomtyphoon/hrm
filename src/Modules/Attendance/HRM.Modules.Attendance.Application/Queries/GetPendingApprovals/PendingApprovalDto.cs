namespace HRM.Modules.Attendance.Application.Queries.GetPendingApprovals;

public sealed record PendingApprovalDto
{
    public required Guid LeaveRequestId { get; init; }
    public required Guid EmployeeId { get; init; }
    public required Guid LeaveTypeId { get; init; }
    public required string LeaveTypeName { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
    public required int TotalDays { get; init; }
    public string? Reason { get; init; }
    public required int CurrentStep { get; init; }
    public required int TotalSteps { get; init; }
    public required string ApprovalLevelName { get; init; }
}
