using HRM.BuildingBlocks.Application.Abstractions.EventBus;
using HRM.Modules.Identity.Domain.Events;
using HRM.Modules.Identity.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Identity.Infrastructure.DomainEventHandlers;

internal sealed class AccountTwoFactorEnabledDomainEventHandler
    : INotificationHandler<AccountTwoFactorEnabledDomainEvent>
{
    private readonly IEventBus _eventBus;

    public AccountTwoFactorEnabledDomainEventHandler(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task Handle(AccountTwoFactorEnabledDomainEvent notification, CancellationToken cancellationToken)
    {
        await _eventBus.PublishAsync(
            new AccountTwoFactorEnabledIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                AccountId: notification.AccountId,
                Username: notification.Username),
            cancellationToken);
    }
}
