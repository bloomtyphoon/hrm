using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Identity.Domain.Events;

public sealed record AccountRolesChangedDomainEvent(
    Guid AccountId,
    List<Guid> RolesAdded,
    List<Guid> RolesRemoved
) : DomainEvent;
