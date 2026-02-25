using HRM.Modules.Identity.Domain.Events;
using HRM.Modules.Identity.Infrastructure.Persistence;
using HRM.Modules.Identity.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Identity.Infrastructure.DomainEventHandlers;

internal sealed class AccountActivatedDomainEventHandler
    : INotificationHandler<AccountActivatedDomainEvent>
{
    private readonly IdentityDbContext _dbContext;

    public AccountActivatedDomainEventHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task Handle(AccountActivatedDomainEvent notification, CancellationToken cancellationToken)
    {
        _dbContext.AddIntegrationEvent(
            new AccountActivatedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                AccountId: notification.AccountId,
                Username: notification.Username));

        return Task.CompletedTask;
    }
}
