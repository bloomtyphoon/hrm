using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Identity.Domain.Events;

public sealed record AccountLoginFailedDomainEvent(
    Guid AccountId,
    string Username,
    int FailedAttempts
) : DomainEvent;
