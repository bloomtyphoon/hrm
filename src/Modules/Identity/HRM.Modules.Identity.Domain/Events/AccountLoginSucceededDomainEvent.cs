using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Identity.Domain.Events;

public sealed record AccountLoginSucceededDomainEvent(
    Guid AccountId,
    string Username
) : DomainEvent;
