using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Identity.Domain.Events;

public sealed record EmployeeProfileCreatedDomainEvent(
    Guid ProfileId,
    Guid AccountId,
    Guid EmployeeId
) : DomainEvent;
