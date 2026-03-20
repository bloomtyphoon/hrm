using HRM.BuildingBlocks.Application.Abstractions.EventBus;
using HRM.Modules.Attendance.Domain.Entities;
using HRM.Modules.Attendance.Infrastructure.Persistence;
using HRM.Modules.Organization.IntegrationEvents;
using Microsoft.Extensions.Logging;

namespace HRM.Modules.Attendance.Infrastructure.IntegrationEventHandlers;

/// <summary>
/// Updates DepartmentSnapshot when a department's manager changes.
/// Creates the snapshot if it doesn't exist yet (upsert).
/// </summary>
internal sealed class DepartmentManagerChangedSyncHandler
    : IIntegrationEventHandler<DepartmentManagerChangedIntegrationEvent>
{
    private readonly AttendanceDbContext _dbContext;
    private readonly ILogger<DepartmentManagerChangedSyncHandler> _logger;

    public DepartmentManagerChangedSyncHandler(
        AttendanceDbContext dbContext,
        ILogger<DepartmentManagerChangedSyncHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(
        DepartmentManagerChangedIntegrationEvent notification,
        CancellationToken cancellationToken)
    {
        var handlerName = nameof(DepartmentManagerChangedSyncHandler);

        if (await _dbContext.IsEventProcessedAsync(notification.Id, handlerName, cancellationToken))
            return;

        var snapshot = await _dbContext.Set<DepartmentSnapshot>()
            .FindAsync([notification.DepartmentId], cancellationToken);

        if (snapshot is null)
        {
            snapshot = new DepartmentSnapshot { DepartmentId = notification.DepartmentId };
            _dbContext.Set<DepartmentSnapshot>().Add(snapshot);
        }

        snapshot.ManagerEmployeeId = notification.NewManagerEmployeeId;
        snapshot.LastUpdatedUtc = notification.OccurredOnUtc;

        _dbContext.MarkEventAsProcessed(
            notification.Id, nameof(DepartmentManagerChangedIntegrationEvent), handlerName);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Updated DepartmentSnapshot for DepartmentId={DepartmentId}: {OldManager} → {NewManager}",
            notification.DepartmentId, notification.OldManagerEmployeeId, notification.NewManagerEmployeeId);
    }
}
