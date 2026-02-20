using HRM.BuildingBlocks.Application.Abstractions.EventBus;
using HRM.Modules.Identity.Domain.Events;
using HRM.Modules.Identity.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Identity.Infrastructure.DomainEventHandlers;

internal sealed class AccountRolesChangedDomainEventHandler
    : INotificationHandler<AccountRolesChangedDomainEvent>
{
    private readonly IEventBus _eventBus;

    public AccountRolesChangedDomainEventHandler(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task Handle(AccountRolesChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        await _eventBus.PublishAsync(
            new AccountRolesChangedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                AccountId: notification.AccountId,
                RolesAdded: notification.RolesAdded,
                RolesRemoved: notification.RolesRemoved),
            cancellationToken);
    }
}
