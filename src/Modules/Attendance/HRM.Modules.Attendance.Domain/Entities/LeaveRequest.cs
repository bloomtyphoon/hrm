using HRM.BuildingBlocks.Domain.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.BuildingBlocks.Domain.Entities;

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

        return new LeaveRequest
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
    }

    public void Approve(Guid approverEmployeeId, string? notes = null)
    {
        if (Status != LeaveStatus.Pending)
            throw new InvalidOperationException($"Cannot approve a leave request with status '{Status}'.");

        Status = LeaveStatus.Approved;
        ApprovedByEmployeeId = approverEmployeeId;
        DecisionDateUtc = DateTime.UtcNow;
        DecisionNotes = notes;
    }

    public void Reject(Guid approverEmployeeId, string? notes = null)
    {
        if (Status != LeaveStatus.Pending)
            throw new InvalidOperationException($"Cannot reject a leave request with status '{Status}'.");

        Status = LeaveStatus.Rejected;
        ApprovedByEmployeeId = approverEmployeeId;
        DecisionDateUtc = DateTime.UtcNow;
        DecisionNotes = notes;
    }

    public void Cancel()
    {
        if (Status == LeaveStatus.Cancelled)
            throw new InvalidOperationException("Leave request is already cancelled.");

        Status = LeaveStatus.Cancelled;
    }

    public int TotalDays => EndDate.DayNumber - StartDate.DayNumber + 1;
}
