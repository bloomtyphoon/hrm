using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Identity.IntegrationEvents;

public sealed record AccountRolesChangedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid AccountId,
    List<Guid> RolesAdded,
    List<Guid> RolesRemoved
) : IIntegrationEvent;
