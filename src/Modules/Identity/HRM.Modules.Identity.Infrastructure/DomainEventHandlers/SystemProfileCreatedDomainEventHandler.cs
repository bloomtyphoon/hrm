using HRM.Modules.Identity.Domain.Events;
using HRM.Modules.Identity.Infrastructure.Persistence;
using HRM.Modules.Identity.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Identity.Infrastructure.DomainEventHandlers;

internal sealed class SystemProfileCreatedDomainEventHandler
    : INotificationHandler<SystemProfileCreatedDomainEvent>
{
    private readonly IdentityDbContext _dbContext;

    public SystemProfileCreatedDomainEventHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task Handle(SystemProfileCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        _dbContext.AddIntegrationEvent(
            new SystemProfileCreatedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                ProfileId: notification.ProfileId,
                AccountId: notification.AccountId,
                IsSuperAdmin: notification.IsSuperAdmin));

        return Task.CompletedTask;
    }
}
