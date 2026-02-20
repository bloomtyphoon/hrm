using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Identity.IntegrationEvents;

public sealed record RoleDeletedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid RoleId,
    string RoleName
) : IIntegrationEvent;
