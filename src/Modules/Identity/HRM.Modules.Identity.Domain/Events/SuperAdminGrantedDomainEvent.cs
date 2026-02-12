using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Identity.Domain.Events;

public sealed record SuperAdminGrantedDomainEvent(
    Guid ProfileId,
    Guid AccountId
) : DomainEvent;
