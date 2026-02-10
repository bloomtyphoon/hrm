using HRM.BuildingBlocks.Application.Abstractions.EventBus;
using HRM.Modules.Identity.Domain.Events;
using HRM.Modules.Identity.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Identity.Infrastructure.DomainEventHandlers;

internal sealed class AccountPasswordChangedDomainEventHandler
    : INotificationHandler<AccountPasswordChangedDomainEvent>
{
    private readonly IEventBus _eventBus;

    public AccountPasswordChangedDomainEventHandler(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task Handle(AccountPasswordChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        await _eventBus.PublishAsync(
            new AccountPasswordChangedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                AccountId: notification.AccountId,
                Username: notification.Username),
            cancellationToken);
    }
}
