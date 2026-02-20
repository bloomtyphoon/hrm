using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Identity.Domain.Events;

public sealed record AccountTwoFactorEnabledDomainEvent(
    Guid AccountId,
    string Username
) : DomainEvent;
