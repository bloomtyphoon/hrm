using HRM.Modules.Personnel.Domain.Events;
using HRM.Modules.Personnel.Infrastructure.Persistence;
using HRM.Modules.Personnel.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Personnel.Infrastructure.DomainEventHandlers;

/// <summary>
/// Domain event handler for EmployeeCreatedDomainEvent.
/// Creates an outbox message containing EmployeeCreatedIntegrationEvent
/// for cross-module communication (Identity module creates Account + EmployeeProfile).
/// </summary>
internal sealed class EmployeeCreatedDomainEventHandler
    : INotificationHandler<EmployeeCreatedDomainEvent>
{
    private readonly PersonnelDbContext _dbContext;

    public EmployeeCreatedDomainEventHandler(PersonnelDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task Handle(EmployeeCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        _dbContext.AddIntegrationEvent(
            new EmployeeCreatedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                TenantId: notification.TenantId,
                EmployeeId: notification.EmployeeId,
                EmployeeCode: notification.EmployeeCode,
                FirstName: notification.FirstName,
                LastName: notification.LastName,
                Email: notification.Email,
                Phone: notification.Phone));

        return Task.CompletedTask;
    }
}
