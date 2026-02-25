using HRM.Modules.Identity.Domain.Events;
using HRM.Modules.Identity.Infrastructure.Persistence;
using HRM.Modules.Identity.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Identity.Infrastructure.DomainEventHandlers;

internal sealed class SuperAdminGrantedDomainEventHandler
    : INotificationHandler<SuperAdminGrantedDomainEvent>
{
    private readonly IdentityDbContext _dbContext;

    public SuperAdminGrantedDomainEventHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task Handle(SuperAdminGrantedDomainEvent notification, CancellationToken cancellationToken)
    {
        _dbContext.AddIntegrationEvent(
            new SuperAdminGrantedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                ProfileId: notification.ProfileId,
                AccountId: notification.AccountId));

        return Task.CompletedTask;
    }
}
