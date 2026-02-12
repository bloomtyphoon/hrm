using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Identity.Domain.Events;

public sealed record EmployeeProfileUpdatedDomainEvent(
    Guid ProfileId,
    Guid AccountId,
    Guid EmployeeId
) : DomainEvent;
