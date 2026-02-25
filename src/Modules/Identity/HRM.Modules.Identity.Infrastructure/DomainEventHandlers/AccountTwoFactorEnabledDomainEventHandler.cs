using HRM.Modules.Identity.Domain.Events;
using HRM.Modules.Identity.Infrastructure.Persistence;
using HRM.Modules.Identity.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Identity.Infrastructure.DomainEventHandlers;

internal sealed class AccountTwoFactorEnabledDomainEventHandler
    : INotificationHandler<AccountTwoFactorEnabledDomainEvent>
{
    private readonly IdentityDbContext _dbContext;

    public AccountTwoFactorEnabledDomainEventHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task Handle(AccountTwoFactorEnabledDomainEvent notification, CancellationToken cancellationToken)
    {
        _dbContext.AddIntegrationEvent(
            new AccountTwoFactorEnabledIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                AccountId: notification.AccountId,
                Username: notification.Username));

        return Task.CompletedTask;
    }
}
