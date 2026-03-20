using HRM.BuildingBlocks.Application.Abstractions.EventBus;
using HRM.Modules.Attendance.Domain.Entities;
using HRM.Modules.Attendance.Infrastructure.Persistence;
using HRM.Modules.Personnel.IntegrationEvents;
using Microsoft.Extensions.Logging;

namespace HRM.Modules.Attendance.Infrastructure.IntegrationEventHandlers;

/// <summary>
/// Updates EmployeeOrganizationSnapshot with PrimaryDepartmentId and PrimaryCompanyId
/// when employee assignments change.
/// </summary>
internal sealed class EmployeeAssignmentsChangedSyncHandler
    : IIntegrationEventHandler<EmployeeAssignmentsChangedIntegrationEvent>
{
    private readonly AttendanceDbContext _dbContext;
    private readonly ILogger<EmployeeAssignmentsChangedSyncHandler> _logger;

    public EmployeeAssignmentsChangedSyncHandler(
        AttendanceDbContext dbContext,
        ILogger<EmployeeAssignmentsChangedSyncHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(
        EmployeeAssignmentsChangedIntegrationEvent notification,
        CancellationToken cancellationToken)
    {
        var handlerName = nameof(EmployeeAssignmentsChangedSyncHandler);

        if (await _dbContext.IsEventProcessedAsync(notification.Id, handlerName, cancellationToken))
            return;

        var snapshot = await _dbContext.Set<EmployeeOrganizationSnapshot>()
            .FindAsync([notification.EmployeeId], cancellationToken);

        if (snapshot is null)
        {
            snapshot = new EmployeeOrganizationSnapshot { EmployeeId = notification.EmployeeId };
            _dbContext.Set<EmployeeOrganizationSnapshot>().Add(snapshot);
        }

        snapshot.PrimaryDepartmentId = notification.PrimaryDepartmentId;
        snapshot.PrimaryCompanyId = notification.PrimaryCompanyId;
        snapshot.LastUpdatedUtc = notification.OccurredOnUtc;

        _dbContext.MarkEventAsProcessed(
            notification.Id, nameof(EmployeeAssignmentsChangedIntegrationEvent), handlerName);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Updated EmployeeOrganizationSnapshot assignments for EmployeeId={EmployeeId}: Dept={DeptId}, Company={CompanyId}",
            notification.EmployeeId, notification.PrimaryDepartmentId, notification.PrimaryCompanyId);
    }
}
