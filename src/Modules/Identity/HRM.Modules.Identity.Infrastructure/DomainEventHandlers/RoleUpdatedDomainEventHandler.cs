using HRM.Modules.Identity.Domain.Events;
using HRM.Modules.Identity.Infrastructure.Persistence;
using HRM.Modules.Identity.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Identity.Infrastructure.DomainEventHandlers;

internal sealed class RoleUpdatedDomainEventHandler
    : INotificationHandler<RoleUpdatedDomainEvent>
{
    private readonly IdentityDbContext _dbContext;

    public RoleUpdatedDomainEventHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task Handle(RoleUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        _dbContext.AddIntegrationEvent(
            new RoleUpdatedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                RoleId: notification.RoleId,
                RoleName: notification.RoleName,
                PermissionCount: notification.PermissionCount));

        return Task.CompletedTask;
    }
}
