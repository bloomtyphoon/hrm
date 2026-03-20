using HRM.BuildingBlocks.Domain.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Entities;

namespace HRM.Modules.Attendance.Domain.Entities;

/// <summary>
/// Tenant-level configuration for leave approval workflow.
/// One setting record per tenant. Created with defaults on first access.
/// </summary>
public class LeaveApprovalSetting : AuditableEntity, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }

    /// <summary>Whether leave requests require approval. If false, requests are auto-approved.</summary>
    public bool RequiresApproval { get; private set; } = true;

    /// <summary>Number of approval levels required (1 = direct approver, 2 = approver + final approver, etc.).</summary>
    public int MaxApprovalLevels { get; private set; } = 1;

    /// <summary>If set, leave requests with total days <= this value are auto-approved.</summary>
    public int? AutoApproveIfDaysLessThanOrEqual { get; private set; }

    /// <summary>Whether employees can cancel their own pending/approved leave requests.</summary>
    public bool AllowSelfCancel { get; private set; } = true;

    /// <summary>Whether to notify the employee when their leave request is approved/rejected.</summary>
    public bool NotifyOnDecision { get; private set; } = true;

    private LeaveApprovalSetting() { }

    public static LeaveApprovalSetting CreateDefault(Guid tenantId)
    {
        return new LeaveApprovalSetting
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RequiresApproval = true,
            MaxApprovalLevels = 1,
            AutoApproveIfDaysLessThanOrEqual = null,
            AllowSelfCancel = true,
            NotifyOnDecision = true
        };
    }

    public void Update(
        bool requiresApproval,
        int maxApprovalLevels,
        int? autoApproveIfDaysLessThanOrEqual,
        bool allowSelfCancel,
        bool notifyOnDecision)
    {
        if (maxApprovalLevels < 1 || maxApprovalLevels > 5)
            throw new InvalidOperationException("MaxApprovalLevels must be between 1 and 5.");

        if (autoApproveIfDaysLessThanOrEqual.HasValue && autoApproveIfDaysLessThanOrEqual.Value < 0)
            throw new InvalidOperationException("AutoApproveIfDaysLessThanOrEqual must be non-negative.");

        RequiresApproval = requiresApproval;
        MaxApprovalLevels = maxApprovalLevels;
        AutoApproveIfDaysLessThanOrEqual = autoApproveIfDaysLessThanOrEqual;
        AllowSelfCancel = allowSelfCancel;
        NotifyOnDecision = notifyOnDecision;
    }
}
