using HRM.BuildingBlocks.Application.Abstractions.EventBus;
using HRM.Modules.Attendance.Domain.Entities;
using HRM.Modules.Attendance.Infrastructure.Persistence;
using HRM.Modules.Personnel.IntegrationEvents;
using Microsoft.Extensions.Logging;

namespace HRM.Modules.Attendance.Infrastructure.IntegrationEventHandlers;

/// <summary>
/// Creates an EmployeeOrganizationSnapshot when a new employee is created.
/// Initial snapshot has no manager/department/company — those are filled by subsequent events.
/// </summary>
internal sealed class EmployeeCreatedSyncHandler
    : IIntegrationEventHandler<EmployeeCreatedIntegrationEvent>
{
    private readonly AttendanceDbContext _dbContext;
    private readonly ILogger<EmployeeCreatedSyncHandler> _logger;

    public EmployeeCreatedSyncHandler(
        AttendanceDbContext dbContext,
        ILogger<EmployeeCreatedSyncHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(
        EmployeeCreatedIntegrationEvent notification,
        CancellationToken cancellationToken)
    {
        var handlerName = nameof(EmployeeCreatedSyncHandler);

        if (await _dbContext.IsEventProcessedAsync(notification.Id, handlerName, cancellationToken))
            return;

        var existing = await _dbContext.Set<EmployeeOrganizationSnapshot>()
            .FindAsync([notification.EmployeeId], cancellationToken);

        if (existing is null)
        {
            _dbContext.Set<EmployeeOrganizationSnapshot>().Add(new EmployeeOrganizationSnapshot
            {
                EmployeeId = notification.EmployeeId,
                LastUpdatedUtc = notification.OccurredOnUtc
            });
        }

        _dbContext.MarkEventAsProcessed(notification.Id, nameof(EmployeeCreatedIntegrationEvent), handlerName);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Created EmployeeOrganizationSnapshot for EmployeeId={EmployeeId}", notification.EmployeeId);
    }
}
