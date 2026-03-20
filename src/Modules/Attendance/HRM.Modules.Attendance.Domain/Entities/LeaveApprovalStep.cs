using HRM.BuildingBlocks.Domain.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Entities;

namespace HRM.Modules.Attendance.Domain.Entities;

/// <summary>
/// Tracks an individual approval step within a leave request's approval chain.
/// Each step represents one approver's decision.
/// </summary>
public class LeaveApprovalStep : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid LeaveRequestId { get; private set; }

    /// <summary>Order in the approval chain (1 = first approver, 2 = second, etc.).</summary>
    public int StepOrder { get; private set; }

    /// <summary>The employee who is expected to approve at this step.</summary>
    public Guid ApproverEmployeeId { get; private set; }

    /// <summary>Human-readable level name (e.g., "Manager", "DepartmentHead", "CompanyLevel").</summary>
    public string ApprovalLevelName { get; private set; } = string.Empty;

    public LeaveApprovalStepStatus Status { get; private set; }
    public DateTime? DecisionDateUtc { get; private set; }
    public string? Notes { get; private set; }

    private LeaveApprovalStep() { }

    public static LeaveApprovalStep Create(
        Guid tenantId,
        Guid leaveRequestId,
        int stepOrder,
        Guid approverEmployeeId,
        string approvalLevelName = "")
    {
        return new LeaveApprovalStep
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LeaveRequestId = leaveRequestId,
            StepOrder = stepOrder,
            ApproverEmployeeId = approverEmployeeId,
            ApprovalLevelName = approvalLevelName,
            Status = LeaveApprovalStepStatus.Pending
        };
    }

    public void Approve(string? notes = null)
    {
        if (Status != LeaveApprovalStepStatus.Pending)
            throw new InvalidOperationException($"Cannot approve step with status '{Status}'.");

        Status = LeaveApprovalStepStatus.Approved;
        DecisionDateUtc = DateTime.UtcNow;
        Notes = notes;
    }

    public void Reject(string? notes = null)
    {
        if (Status != LeaveApprovalStepStatus.Pending)
            throw new InvalidOperationException($"Cannot reject step with status '{Status}'.");

        Status = LeaveApprovalStepStatus.Rejected;
        DecisionDateUtc = DateTime.UtcNow;
        Notes = notes;
    }

    public void Skip(string? notes = null)
    {
        if (Status != LeaveApprovalStepStatus.Pending)
            throw new InvalidOperationException($"Cannot skip step with status '{Status}'.");

        Status = LeaveApprovalStepStatus.Skipped;
        DecisionDateUtc = DateTime.UtcNow;
        Notes = notes ?? "Auto-skipped";
    }
}

public enum LeaveApprovalStepStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Skipped = 4
}
