using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Identity.IntegrationEvents;

public sealed record SystemProfileUpdatedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid ProfileId,
    Guid AccountId,
    string? Department,
    string? JobTitle
) : IIntegrationEvent;
