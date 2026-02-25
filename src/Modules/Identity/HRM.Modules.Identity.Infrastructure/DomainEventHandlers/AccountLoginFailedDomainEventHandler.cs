using HRM.Modules.Identity.Domain.Events;
using HRM.Modules.Identity.Infrastructure.Persistence;
using HRM.Modules.Identity.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Identity.Infrastructure.DomainEventHandlers;

internal sealed class AccountLoginFailedDomainEventHandler
    : INotificationHandler<AccountLoginFailedDomainEvent>
{
    private readonly IdentityDbContext _dbContext;

    public AccountLoginFailedDomainEventHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task Handle(AccountLoginFailedDomainEvent notification, CancellationToken cancellationToken)
    {
        _dbContext.AddIntegrationEvent(
            new AccountLoginFailedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                AccountId: notification.AccountId,
                Username: notification.Username,
                FailedAttempts: notification.FailedAttempts));

        return Task.CompletedTask;
    }
}
