using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Identity.IntegrationEvents;

public sealed record SystemProfileCreatedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid ProfileId,
    Guid AccountId,
    bool IsSuperAdmin
) : IIntegrationEvent;
