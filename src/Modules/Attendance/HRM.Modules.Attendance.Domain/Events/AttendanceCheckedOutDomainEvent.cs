using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Attendance.Domain.Events;

/// <summary>
/// Raised when an employee successfully checks out.
/// </summary>
public sealed record AttendanceCheckedOutDomainEvent(
    Guid TenantId,
    Guid EmployeeId,
    Guid RecordId,
    DateOnly Date,
    DateTime CheckInTimeUtc,
    DateTime CheckOutTimeUtc) : DomainEvent;
