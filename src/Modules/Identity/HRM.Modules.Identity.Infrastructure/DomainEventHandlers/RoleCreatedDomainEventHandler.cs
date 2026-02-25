using HRM.Modules.Identity.Domain.Events;
using HRM.Modules.Identity.Infrastructure.Persistence;
using HRM.Modules.Identity.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Identity.Infrastructure.DomainEventHandlers;

internal sealed class RoleCreatedDomainEventHandler
    : INotificationHandler<RoleCreatedDomainEvent>
{
    private readonly IdentityDbContext _dbContext;

    public RoleCreatedDomainEventHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task Handle(RoleCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        _dbContext.AddIntegrationEvent(
            new RoleCreatedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                RoleId: notification.RoleId,
                RoleName: notification.RoleName,
                Description: notification.Description,
                PermissionCount: notification.PermissionCount));

        return Task.CompletedTask;
    }
}
