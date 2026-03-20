using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Personnel.IntegrationEvents;

/// <summary>
/// Published when an employee's manager changes.
/// Consumed by Attendance module to update local approval chain snapshot.
/// </summary>
public sealed record ManagerChangedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid EmployeeId,
    Guid? OldManagerId,
    Guid? NewManagerId
) : IIntegrationEvent;
