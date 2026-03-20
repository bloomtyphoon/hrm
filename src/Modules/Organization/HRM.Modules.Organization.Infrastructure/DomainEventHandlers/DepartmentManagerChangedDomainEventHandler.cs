using HRM.Modules.Organization.Domain.Events;
using HRM.Modules.Organization.Infrastructure.Persistence;
using HRM.Modules.Organization.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Organization.Infrastructure.DomainEventHandlers;

/// <summary>
/// Publishes DepartmentManagerChangedIntegrationEvent to the outbox
/// when a department's manager changes.
/// </summary>
internal sealed class DepartmentManagerChangedDomainEventHandler
    : INotificationHandler<DepartmentManagerChangedDomainEvent>
{
    private readonly OrganizationDbContext _dbContext;

    public DepartmentManagerChangedDomainEventHandler(OrganizationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task Handle(DepartmentManagerChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        _dbContext.AddIntegrationEvent(
            new DepartmentManagerChangedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                DepartmentId: notification.DepartmentId,
                OldManagerEmployeeId: notification.OldManagerEmployeeId,
                NewManagerEmployeeId: notification.NewManagerEmployeeId));

        return Task.CompletedTask;
    }
}
