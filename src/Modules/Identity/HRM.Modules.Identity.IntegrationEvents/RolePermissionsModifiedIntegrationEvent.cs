using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Identity.IntegrationEvents;

public sealed record RolePermissionsModifiedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid RoleId,
    string RoleName,
    int PermissionsAdded,
    int PermissionsRemoved,
    int TotalPermissions
) : IIntegrationEvent;
