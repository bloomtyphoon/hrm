using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Identity.Domain.Events;

public sealed record AccountDeactivatedDomainEvent(
    Guid AccountId,
    string Username
) : DomainEvent;
