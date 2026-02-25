using HRM.Modules.Identity.Domain.Events;
using HRM.Modules.Identity.Infrastructure.Persistence;
using HRM.Modules.Identity.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Identity.Infrastructure.DomainEventHandlers;

internal sealed class RolePermissionsModifiedDomainEventHandler
    : INotificationHandler<RolePermissionsModifiedDomainEvent>
{
    private readonly IdentityDbContext _dbContext;

    public RolePermissionsModifiedDomainEventHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task Handle(RolePermissionsModifiedDomainEvent notification, CancellationToken cancellationToken)
    {
        _dbContext.AddIntegrationEvent(
            new RolePermissionsModifiedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                RoleId: notification.RoleId,
                RoleName: notification.RoleName,
                PermissionsAdded: notification.PermissionsAdded,
                PermissionsRemoved: notification.PermissionsRemoved,
                TotalPermissions: notification.TotalPermissions));

        return Task.CompletedTask;
    }
}
