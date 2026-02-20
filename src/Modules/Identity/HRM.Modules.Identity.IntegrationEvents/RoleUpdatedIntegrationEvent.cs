using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Identity.IntegrationEvents;

public sealed record RoleUpdatedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid RoleId,
    string RoleName,
    int PermissionCount
) : IIntegrationEvent;
