using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Personnel.Domain.Events;

/// <summary>
/// Raised when an employee's manager assignment changes (assigned or removed).
///
/// Consumed by ManagerChangedDomainEventHandler to maintain the closure table
/// using the Celko 2-step prune+graft algorithm.
/// </summary>
public sealed record ManagerChangedDomainEvent(
    Guid TenantId,
    Guid EmployeeId,
    Guid? OldManagerId,
    Guid? NewManagerId
) : DomainEvent;
