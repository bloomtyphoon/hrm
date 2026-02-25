using HRM.Modules.Identity.Domain.Events;
using HRM.Modules.Identity.Infrastructure.Persistence;
using HRM.Modules.Identity.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Identity.Infrastructure.DomainEventHandlers;

internal sealed class AccountLockedDomainEventHandler
    : INotificationHandler<AccountLockedDomainEvent>
{
    private readonly IdentityDbContext _dbContext;

    public AccountLockedDomainEventHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task Handle(AccountLockedDomainEvent notification, CancellationToken cancellationToken)
    {
        _dbContext.AddIntegrationEvent(
            new AccountLockedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                AccountId: notification.AccountId,
                Username: notification.Username,
                FailedAttempts: notification.FailedAttempts,
                LockedUntilUtc: notification.LockedUntilUtc));

        return Task.CompletedTask;
    }
}
