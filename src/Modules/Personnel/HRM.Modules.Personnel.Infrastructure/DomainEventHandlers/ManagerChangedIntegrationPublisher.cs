using HRM.Modules.Personnel.Domain.Events;
using HRM.Modules.Personnel.Infrastructure.Persistence;
using HRM.Modules.Personnel.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Personnel.Infrastructure.DomainEventHandlers;

/// <summary>
/// Publishes ManagerChangedIntegrationEvent to the outbox
/// when an employee's manager changes.
///
/// Separate from ManagerChangedDomainEventHandler (which maintains the closure table).
/// MediatR dispatches domain events to ALL registered handlers.
/// </summary>
internal sealed class ManagerChangedIntegrationPublisher
    : INotificationHandler<ManagerChangedDomainEvent>
{
    private readonly PersonnelDbContext _dbContext;

    public ManagerChangedIntegrationPublisher(PersonnelDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task Handle(ManagerChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        _dbContext.AddIntegrationEvent(
            new ManagerChangedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                EmployeeId: notification.EmployeeId,
                OldManagerId: notification.OldManagerId,
                NewManagerId: notification.NewManagerId));

        return Task.CompletedTask;
    }
}
