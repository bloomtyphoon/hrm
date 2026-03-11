using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Attendance.IntegrationEvents;

/// <summary>
/// Published when an employee successfully checks in.
/// Stored in outbox and published asynchronously for cross-module consumers
/// (e.g., notifications, analytics, payroll future modules).
/// </summary>
public sealed record AttendanceCheckedInIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid EmployeeId,
    Guid RecordId,
    DateOnly Date,
    DateTime CheckInTimeUtc) : IIntegrationEvent;
