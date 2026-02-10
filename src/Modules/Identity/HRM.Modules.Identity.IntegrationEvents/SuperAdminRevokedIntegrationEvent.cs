using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Identity.IntegrationEvents;

public sealed record SuperAdminRevokedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid ProfileId,
    Guid AccountId
) : IIntegrationEvent;
