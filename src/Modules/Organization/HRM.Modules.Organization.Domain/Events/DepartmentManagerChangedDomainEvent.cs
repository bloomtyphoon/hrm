using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Organization.Domain.Events;

/// <summary>
/// Raised when a department's manager (head) is assigned or removed.
/// Consumed by domain event handler to publish DepartmentManagerChangedIntegrationEvent.
/// </summary>
public sealed record DepartmentManagerChangedDomainEvent(
    Guid DepartmentId,
    Guid? OldManagerEmployeeId,
    Guid? NewManagerEmployeeId
) : DomainEvent;
