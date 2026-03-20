using HRM.Modules.Personnel.Domain.Events;
using HRM.Modules.Personnel.Infrastructure.Persistence;
using HRM.Modules.Personnel.IntegrationEvents;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Personnel.Infrastructure.DomainEventHandlers;

internal sealed class EmployeeAssignmentsChangedDomainEventHandler
    : INotificationHandler<EmployeeAssignmentsChangedDomainEvent>
{
    private readonly PersonnelDbContext _dbContext;

    public EmployeeAssignmentsChangedDomainEventHandler(PersonnelDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(EmployeeAssignmentsChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        // Look up primary assignment IDs from the employee (already in change tracker)
        var primaryIds = await _dbContext.Employees
            .Where(e => e.Id == notification.EmployeeId)
            .Select(e => new { e.PrimaryDepartmentId, e.PrimaryCompanyId })
            .FirstOrDefaultAsync(cancellationToken);

        _dbContext.AddIntegrationEvent(
            new EmployeeAssignmentsChangedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                EmployeeId: notification.EmployeeId,
                ActiveCompanyIds: notification.ActiveCompanyIds,
                ActiveDepartmentIds: notification.ActiveDepartmentIds,
                ActivePositionIds: notification.ActivePositionIds,
                PrimaryDepartmentId: primaryIds?.PrimaryDepartmentId,
                PrimaryCompanyId: primaryIds?.PrimaryCompanyId));
    }
}
