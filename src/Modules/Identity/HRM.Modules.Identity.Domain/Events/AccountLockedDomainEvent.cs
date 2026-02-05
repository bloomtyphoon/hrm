using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Identity.Domain.Events;

public sealed record AccountLockedDomainEvent(
    Guid AccountId,
    string Username,
    int FailedAttempts,
    DateTime LockedUntilUtc
) : DomainEvent;
