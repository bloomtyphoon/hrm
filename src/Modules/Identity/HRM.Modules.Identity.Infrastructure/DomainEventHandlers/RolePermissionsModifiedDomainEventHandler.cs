using HRM.BuildingBlocks.Application.Abstractions.EventBus;
using HRM.Modules.Identity.Domain.Events;
using HRM.Modules.Identity.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Identity.Infrastructure.DomainEventHandlers;

internal sealed class RolePermissionsModifiedDomainEventHandler
    : INotificationHandler<RolePermissionsModifiedDomainEvent>
{
    private readonly IEventBus _eventBus;

    public RolePermissionsModifiedDomainEventHandler(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task Handle(RolePermissionsModifiedDomainEvent notification, CancellationToken cancellationToken)
    {
        await _eventBus.PublishAsync(
            new RolePermissionsModifiedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                RoleId: notification.RoleId,
                RoleName: notification.RoleName,
                PermissionsAdded: notification.PermissionsAdded,
                PermissionsRemoved: notification.PermissionsRemoved,
                TotalPermissions: notification.TotalPermissions),
            cancellationToken);
    }
}
