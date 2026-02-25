using HRM.Modules.Identity.Domain.Events;
using HRM.Modules.Identity.Infrastructure.Persistence;
using HRM.Modules.Identity.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Identity.Infrastructure.DomainEventHandlers;

internal sealed class AccountRolesChangedDomainEventHandler
    : INotificationHandler<AccountRolesChangedDomainEvent>
{
    private readonly IdentityDbContext _dbContext;

    public AccountRolesChangedDomainEventHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task Handle(AccountRolesChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        _dbContext.AddIntegrationEvent(
            new AccountRolesChangedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                AccountId: notification.AccountId,
                RolesAdded: notification.RolesAdded,
                RolesRemoved: notification.RolesRemoved));

        return Task.CompletedTask;
    }
}
