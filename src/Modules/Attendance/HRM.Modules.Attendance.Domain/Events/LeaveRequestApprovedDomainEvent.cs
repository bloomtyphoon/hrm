using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Attendance.Domain.Events;

/// <summary>
/// Raised when a leave request is fully approved (final step or single-level).
/// </summary>
public sealed record LeaveRequestApprovedDomainEvent(
    Guid TenantId,
    Guid LeaveRequestId,
    Guid EmployeeId,
    Guid? ApproverEmployeeId) : DomainEvent;
