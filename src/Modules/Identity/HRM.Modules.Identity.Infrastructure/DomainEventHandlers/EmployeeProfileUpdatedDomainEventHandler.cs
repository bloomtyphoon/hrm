using HRM.BuildingBlocks.Application.Abstractions.EventBus;
using HRM.Modules.Identity.Domain.Events;
using HRM.Modules.Identity.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Identity.Infrastructure.DomainEventHandlers;

internal sealed class EmployeeProfileUpdatedDomainEventHandler
    : INotificationHandler<EmployeeProfileUpdatedDomainEvent>
{
    private readonly IEventBus _eventBus;

    public EmployeeProfileUpdatedDomainEventHandler(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task Handle(EmployeeProfileUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        await _eventBus.PublishAsync(
            new EmployeeProfileUpdatedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                ProfileId: notification.ProfileId,
                AccountId: notification.AccountId,
                EmployeeId: notification.EmployeeId),
            cancellationToken);
    }
}
