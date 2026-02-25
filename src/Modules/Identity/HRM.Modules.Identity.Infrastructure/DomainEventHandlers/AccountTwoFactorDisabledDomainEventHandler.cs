using HRM.Modules.Identity.Domain.Events;
using HRM.Modules.Identity.Infrastructure.Persistence;
using HRM.Modules.Identity.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Identity.Infrastructure.DomainEventHandlers;

internal sealed class AccountTwoFactorDisabledDomainEventHandler
    : INotificationHandler<AccountTwoFactorDisabledDomainEvent>
{
    private readonly IdentityDbContext _dbContext;

    public AccountTwoFactorDisabledDomainEventHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task Handle(AccountTwoFactorDisabledDomainEvent notification, CancellationToken cancellationToken)
    {
        _dbContext.AddIntegrationEvent(
            new AccountTwoFactorDisabledIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                AccountId: notification.AccountId,
                Username: notification.Username));

        return Task.CompletedTask;
    }
}
