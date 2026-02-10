using HRM.BuildingBlocks.Application.Abstractions.EventBus;
using HRM.Modules.Identity.Domain.Events;
using HRM.Modules.Identity.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Identity.Infrastructure.DomainEventHandlers;

internal sealed class RoleDeletedDomainEventHandler
    : INotificationHandler<RoleDeletedDomainEvent>
{
    private readonly IEventBus _eventBus;

    public RoleDeletedDomainEventHandler(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task Handle(RoleDeletedDomainEvent notification, CancellationToken cancellationToken)
    {
        await _eventBus.PublishAsync(
            new RoleDeletedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                RoleId: notification.RoleId,
                RoleName: notification.RoleName),
            cancellationToken);
    }
}
