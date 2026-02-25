using HRM.Modules.Identity.Domain.Events;
using HRM.Modules.Identity.Infrastructure.Persistence;
using HRM.Modules.Identity.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Identity.Infrastructure.DomainEventHandlers;

internal sealed class RoleDeletedDomainEventHandler
    : INotificationHandler<RoleDeletedDomainEvent>
{
    private readonly IdentityDbContext _dbContext;

    public RoleDeletedDomainEventHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task Handle(RoleDeletedDomainEvent notification, CancellationToken cancellationToken)
    {
        _dbContext.AddIntegrationEvent(
            new RoleDeletedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                RoleId: notification.RoleId,
                RoleName: notification.RoleName));

        return Task.CompletedTask;
    }
}
