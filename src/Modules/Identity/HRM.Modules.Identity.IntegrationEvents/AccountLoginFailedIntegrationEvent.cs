using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Identity.IntegrationEvents;

public sealed record AccountLoginFailedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid AccountId,
    string Username,
    int FailedAttempts
) : IIntegrationEvent;
