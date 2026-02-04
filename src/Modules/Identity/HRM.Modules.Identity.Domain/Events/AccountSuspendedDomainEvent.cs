using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Identity.Domain.Events;

public sealed record AccountSuspendedDomainEvent(
    Guid AccountId,
    string Username
) : DomainEvent;
