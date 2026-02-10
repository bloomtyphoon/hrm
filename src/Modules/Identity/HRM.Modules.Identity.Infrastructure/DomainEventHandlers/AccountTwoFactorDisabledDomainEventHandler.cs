using HRM.BuildingBlocks.Application.Abstractions.EventBus;
using HRM.Modules.Identity.Domain.Events;
using HRM.Modules.Identity.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Identity.Infrastructure.DomainEventHandlers;

internal sealed class AccountTwoFactorDisabledDomainEventHandler
    : INotificationHandler<AccountTwoFactorDisabledDomainEvent>
{
    private readonly IEventBus _eventBus;

    public AccountTwoFactorDisabledDomainEventHandler(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task Handle(AccountTwoFactorDisabledDomainEvent notification, CancellationToken cancellationToken)
    {
        await _eventBus.PublishAsync(
            new AccountTwoFactorDisabledIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                AccountId: notification.AccountId,
                Username: notification.Username),
            cancellationToken);
    }
}
