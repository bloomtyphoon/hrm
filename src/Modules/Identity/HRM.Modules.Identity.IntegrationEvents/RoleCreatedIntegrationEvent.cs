using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Identity.IntegrationEvents;

public sealed record RoleCreatedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid RoleId,
    string RoleName,
    string? Description,
    int PermissionCount
) : IIntegrationEvent;
