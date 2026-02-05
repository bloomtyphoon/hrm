using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Identity.Domain.Events;

public sealed record AccountPasswordChangedDomainEvent(
    Guid AccountId,
    string Username
) : DomainEvent;
