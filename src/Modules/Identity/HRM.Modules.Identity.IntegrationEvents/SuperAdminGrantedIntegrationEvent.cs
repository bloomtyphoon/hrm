using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Identity.IntegrationEvents;

public sealed record SuperAdminGrantedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid ProfileId,
    Guid AccountId
) : IIntegrationEvent;
