using HRM.BuildingBlocks.Domain.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.BuildingBlocks.Domain.Entities;
using HRM.Modules.Attendance.Domain.Events;

namespace HRM.Modules.Attendance.Domain.Entities;

/// <summary>
/// A leave request from an employee, subject to approval workflow.
/// </summary>
public class LeaveRequest : AuditableEntity, IAggregateRoot, IScopedEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid LeaveTypeId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public string? Reason { get; private set; }
    public LeaveStatus Status { get; private set; }
    public Guid? ApprovedByEmployeeId { get; private set; }
    public DateTime? DecisionDateUtc { get; private set; }
    public string? DecisionNotes { get; private set; }

    /// <summary>Current approval step (1-based). Null if no multi-level approval.</summary>
    public int? CurrentApprovalStep { get; private set; }

    /// <summary>Total approval steps required. Null if no multi-level approval.</summary>
    public int? TotalApprovalSteps { get; private set; }

    [ScopeDimension(DimensionKeys.Company)]
    public Guid? CompanyId { get; private set; }

    public Guid OwnerId => EmployeeId;

    private LeaveRequest() { }

    public static LeaveRequest Create(
        Guid tenantId,
        Guid employeeId,
        Guid leaveTypeId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? companyId = null,
        string? reason = null)
    {
        if (endDate < startDate)
            throw new InvalidOperationException("End date must be on or after start date.");

        var request = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            StartDate = startDate,
            EndDate = endDate,
            CompanyId = companyId,
            Reason = reason,
            Status = LeaveStatus.Pending
        };

        request.AddDomainEvent(new LeaveRequestSubmittedDomainEvent(
            tenantId, request.Id, employeeId, leaveTypeId, startDate, endDate));

        return request;
    }

    /// <summary>
    /// Auto-approve a leave request (no approval required per settings).
    /// </summary>
    public void AutoApprove()
    {
        if (Status != LeaveStatus.Pending)
            throw new InvalidOperationException($"Cannot auto-approve a leave request with status '{Status}'.");

        Status = LeaveStatus.Approved;
        DecisionDateUtc = DateTime.UtcNow;
        DecisionNotes = "Auto-approved per approval settings.";

        AddDomainEvent(new LeaveRequestApprovedDomainEvent(
            TenantId, Id, EmployeeId, null));
    }

    /// <summary>
    /// Set up multi-level approval chain.
    /// </summary>
    public void InitializeApprovalChain(int totalSteps)
    {
        if (totalSteps < 1)
            throw new InvalidOperationException("TotalSteps must be at least 1.");

        CurrentApprovalStep = 1;
        TotalApprovalSteps = totalSteps;
    }

    public void Approve(Guid approverEmployeeId, string? notes = null)
    {
        if (Status != LeaveStatus.Pending)
            throw new InvalidOperationException($"Cannot approve a leave request with status '{Status}'.");

        // Multi-level: advance to next step or finalize
        if (TotalApprovalSteps.HasValue && CurrentApprovalStep.HasValue
            && CurrentApprovalStep.Value < TotalApprovalSteps.Value)
        {
            CurrentApprovalStep = CurrentApprovalStep.Value + 1;
            // Don't change status yet - still pending next approval
            return;
        }

        Status = LeaveStatus.Approved;
        ApprovedByEmployeeId = approverEmployeeId;
        DecisionDateUtc = DateTime.UtcNow;
        DecisionNotes = notes;

        AddDomainEvent(new LeaveRequestApprovedDomainEvent(
            TenantId, Id, EmployeeId, approverEmployeeId));
    }

    public void Reject(Guid approverEmployeeId, string? notes = null)
    {
        if (Status != LeaveStatus.Pending)
            throw new InvalidOperationException($"Cannot reject a leave request with status '{Status}'.");

        Status = LeaveStatus.Rejected;
        ApprovedByEmployeeId = approverEmployeeId;
        DecisionDateUtc = DateTime.UtcNow;
        DecisionNotes = notes;

        AddDomainEvent(new LeaveRequestRejectedDomainEvent(
            TenantId, Id, EmployeeId, approverEmployeeId));
    }

    public void Cancel()
    {
        if (Status == LeaveStatus.Cancelled)
            throw new InvalidOperationException("Leave request is already cancelled.");

        Status = LeaveStatus.Cancelled;
    }

    public int TotalDays => EndDate.DayNumber - StartDate.DayNumber + 1;
}
