using HRM.Modules.Identity.Domain.Events;
using HRM.Modules.Identity.Infrastructure.Persistence;
using HRM.Modules.Identity.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Identity.Infrastructure.DomainEventHandlers;

internal sealed class SystemProfileUpdatedDomainEventHandler
    : INotificationHandler<SystemProfileUpdatedDomainEvent>
{
    private readonly IdentityDbContext _dbContext;

    public SystemProfileUpdatedDomainEventHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task Handle(SystemProfileUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        _dbContext.AddIntegrationEvent(
            new SystemProfileUpdatedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                ProfileId: notification.ProfileId,
                AccountId: notification.AccountId,
                Department: notification.Department,
                JobTitle: notification.JobTitle));

        return Task.CompletedTask;
    }
}
