using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Attendance.Domain.Events;

/// <summary>
/// Raised when an employee submits a new leave request.
/// </summary>
public sealed record LeaveRequestSubmittedDomainEvent(
    Guid TenantId,
    Guid LeaveRequestId,
    Guid EmployeeId,
    Guid LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate) : DomainEvent;
