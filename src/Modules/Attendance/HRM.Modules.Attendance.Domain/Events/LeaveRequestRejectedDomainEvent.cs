using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Attendance.Domain.Events;

/// <summary>
/// Raised when a leave request is rejected.
/// </summary>
public sealed record LeaveRequestRejectedDomainEvent(
    Guid TenantId,
    Guid LeaveRequestId,
    Guid EmployeeId,
    Guid ApproverEmployeeId) : DomainEvent;
