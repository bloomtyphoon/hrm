using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Personnel.Domain.Events;

/// <summary>
/// Raised when an employee's direct manager is assigned or removed.
/// Consumed by the domain event handler to invalidate hierarchy scope caches,
/// ensuring EmployeeSet scope resolution reflects the updated reporting line.
/// </summary>
public sealed record ManagerChangedDomainEvent(
    Guid TenantId,
    Guid EmployeeId,
    Guid? PreviousManagerId,
    Guid? NewManagerId
) : DomainEvent;
