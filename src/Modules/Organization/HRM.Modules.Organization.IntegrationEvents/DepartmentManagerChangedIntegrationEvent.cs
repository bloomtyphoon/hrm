using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Organization.IntegrationEvents;

/// <summary>
/// Published when a department's manager (head) changes.
/// Consumed by Attendance module to update local department snapshot.
/// </summary>
public sealed record DepartmentManagerChangedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid DepartmentId,
    Guid? OldManagerEmployeeId,
    Guid? NewManagerEmployeeId
) : IIntegrationEvent;
