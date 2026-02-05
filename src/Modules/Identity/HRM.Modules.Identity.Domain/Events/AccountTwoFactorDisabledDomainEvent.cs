using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Identity.Domain.Events;

public sealed record AccountTwoFactorDisabledDomainEvent(
    Guid AccountId,
    string Username
) : DomainEvent;
