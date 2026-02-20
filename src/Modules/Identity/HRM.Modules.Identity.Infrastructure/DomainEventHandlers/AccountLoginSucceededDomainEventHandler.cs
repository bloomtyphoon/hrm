using HRM.BuildingBlocks.Application.Abstractions.EventBus;
using HRM.Modules.Identity.Domain.Events;
using HRM.Modules.Identity.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Identity.Infrastructure.DomainEventHandlers;

internal sealed class AccountLoginSucceededDomainEventHandler
    : INotificationHandler<AccountLoginSucceededDomainEvent>
{
    private readonly IEventBus _eventBus;

    public AccountLoginSucceededDomainEventHandler(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task Handle(AccountLoginSucceededDomainEvent notification, CancellationToken cancellationToken)
    {
        await _eventBus.PublishAsync(
            new AccountLoginSucceededIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                AccountId: notification.AccountId,
                Username: notification.Username),
            cancellationToken);
    }
}
