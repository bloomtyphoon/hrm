namespace HRM.Modules.Attendance.Application.Queries.GetLeaveApprovalSteps;

public sealed record LeaveApprovalStepDto
{
    public required Guid Id { get; init; }
    public required int StepOrder { get; init; }
    public required Guid ApproverEmployeeId { get; init; }
    public required string ApprovalLevelName { get; init; }
    public required string Status { get; init; }
    public DateTime? DecisionDateUtc { get; init; }
    public string? Notes { get; init; }
}
