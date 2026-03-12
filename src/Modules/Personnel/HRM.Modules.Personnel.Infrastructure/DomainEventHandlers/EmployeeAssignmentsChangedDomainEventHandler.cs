using HRM.Modules.Personnel.Domain.Events;
using HRM.Modules.Personnel.Infrastructure.Persistence;
using HRM.Modules.Personnel.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Personnel.Infrastructure.DomainEventHandlers;

internal sealed class EmployeeAssignmentsChangedDomainEventHandler
    : INotificationHandler<EmployeeAssignmentsChangedDomainEvent>
{
    private readonly PersonnelDbContext _dbContext;

    public EmployeeAssignmentsChangedDomainEventHandler(PersonnelDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task Handle(EmployeeAssignmentsChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        _dbContext.AddIntegrationEvent(
            new EmployeeAssignmentsChangedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                EmployeeId: notification.EmployeeId,
                ActiveCompanyIds: notification.ActiveCompanyIds,
                ActiveDepartmentIds: notification.ActiveDepartmentIds,
                ActivePositionIds: notification.ActivePositionIds));

        return Task.CompletedTask;
    }
}
