using HRM.BuildingBlocks.Application.Abstractions.EventBus;
using HRM.Modules.Attendance.Domain.Entities;
using HRM.Modules.Attendance.Infrastructure.Persistence;
using HRM.Modules.Personnel.IntegrationEvents;
using Microsoft.Extensions.Logging;

namespace HRM.Modules.Attendance.Infrastructure.IntegrationEventHandlers;

/// <summary>
/// Updates EmployeeOrganizationSnapshot.ManagerId when an employee's manager changes.
/// Creates the snapshot if it doesn't exist yet (upsert).
/// </summary>
internal sealed class ManagerChangedSyncHandler
    : IIntegrationEventHandler<ManagerChangedIntegrationEvent>
{
    private readonly AttendanceDbContext _dbContext;
    private readonly ILogger<ManagerChangedSyncHandler> _logger;

    public ManagerChangedSyncHandler(
        AttendanceDbContext dbContext,
        ILogger<ManagerChangedSyncHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(
        ManagerChangedIntegrationEvent notification,
        CancellationToken cancellationToken)
    {
        var handlerName = nameof(ManagerChangedSyncHandler);

        if (await _dbContext.IsEventProcessedAsync(notification.Id, handlerName, cancellationToken))
            return;

        var snapshot = await _dbContext.Set<EmployeeOrganizationSnapshot>()
            .FindAsync([notification.EmployeeId], cancellationToken);

        if (snapshot is null)
        {
            snapshot = new EmployeeOrganizationSnapshot { EmployeeId = notification.EmployeeId };
            _dbContext.Set<EmployeeOrganizationSnapshot>().Add(snapshot);
        }

        snapshot.ManagerId = notification.NewManagerId;
        snapshot.LastUpdatedUtc = notification.OccurredOnUtc;

        _dbContext.MarkEventAsProcessed(notification.Id, nameof(ManagerChangedIntegrationEvent), handlerName);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Updated EmployeeOrganizationSnapshot ManagerId for EmployeeId={EmployeeId}: {OldManager} → {NewManager}",
            notification.EmployeeId, notification.OldManagerId, notification.NewManagerId);
    }
}
