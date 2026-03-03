using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Attendance.Domain.Events;

/// <summary>
/// Raised when an employee successfully checks in.
/// Consumed by: AttendanceCheckedInDomainEventHandler (creates integration event for outbox).
/// </summary>
public sealed record AttendanceCheckedInDomainEvent(
    Guid TenantId,
    Guid EmployeeId,
    Guid RecordId,
    DateOnly Date,
    DateTime CheckInTimeUtc) : DomainEvent;
