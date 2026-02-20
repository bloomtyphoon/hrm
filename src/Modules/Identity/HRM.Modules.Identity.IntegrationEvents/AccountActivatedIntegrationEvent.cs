using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Identity.IntegrationEvents;

public sealed record AccountActivatedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid AccountId,
    string Username
) : IIntegrationEvent;
