using HRM.BuildingBlocks.Application.Abstractions.EventBus;
using HRM.Modules.Identity.Domain.Events;
using HRM.Modules.Identity.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Identity.Infrastructure.DomainEventHandlers;

internal sealed class AccountLoginFailedDomainEventHandler
    : INotificationHandler<AccountLoginFailedDomainEvent>
{
    private readonly IEventBus _eventBus;

    public AccountLoginFailedDomainEventHandler(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task Handle(AccountLoginFailedDomainEvent notification, CancellationToken cancellationToken)
    {
        await _eventBus.PublishAsync(
            new AccountLoginFailedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                AccountId: notification.AccountId,
                Username: notification.Username,
                FailedAttempts: notification.FailedAttempts),
            cancellationToken);
    }
}
