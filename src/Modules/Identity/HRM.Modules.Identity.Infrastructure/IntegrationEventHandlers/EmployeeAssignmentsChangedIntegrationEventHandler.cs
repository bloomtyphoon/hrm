using HRM.BuildingBlocks.Application.Abstractions.EventBus;
using HRM.Modules.Identity.Domain.Repositories;
using HRM.Modules.Identity.Infrastructure.Persistence;
using HRM.Modules.Personnel.IntegrationEvents;
using Microsoft.Extensions.Logging;

namespace HRM.Modules.Identity.Infrastructure.IntegrationEventHandlers;

/// <summary>
/// Handles EmployeeAssignmentsChangedIntegrationEvent from Personnel module.
/// Syncs CompanyAccess, DepartmentAccess, and PositionAccess on EmployeeProfile
/// to keep Identity module's denormalized data consistent with Personnel assignments.
///
/// Idempotency: Uses Inbox pattern. Access sync is also naturally idempotent
/// (replaces entire list), but inbox prevents unnecessary processing.
/// </summary>
internal sealed class EmployeeAssignmentsChangedIntegrationEventHandler
    : IIntegrationEventHandler<EmployeeAssignmentsChangedIntegrationEvent>
{
    private readonly IdentityDbContext _dbContext;
    private readonly IEmployeeProfileRepository _employeeProfileRepository;
    private readonly ILogger<EmployeeAssignmentsChangedIntegrationEventHandler> _logger;

    public EmployeeAssignmentsChangedIntegrationEventHandler(
        IdentityDbContext dbContext,
        IEmployeeProfileRepository employeeProfileRepository,
        ILogger<EmployeeAssignmentsChangedIntegrationEventHandler> logger)
    {
        _dbContext = dbContext;
        _employeeProfileRepository = employeeProfileRepository;
        _logger = logger;
    }

    public async Task Handle(
        EmployeeAssignmentsChangedIntegrationEvent notification,
        CancellationToken cancellationToken)
    {
        var handlerName = nameof(EmployeeAssignmentsChangedIntegrationEventHandler);

        try
        {
            // Inbox check: skip if already processed
            if (await _dbContext.IsEventProcessedAsync(notification.Id, handlerName, cancellationToken))
            {
                _logger.LogDebug(
                    "Event {EventId} already processed by {HandlerName}, skipping",
                    notification.Id, handlerName);
                return;
            }

            var profile = await _employeeProfileRepository.GetByEmployeeIdAsync(
                notification.EmployeeId, cancellationToken);

            if (profile is null)
            {
                _logger.LogDebug(
                    "No EmployeeProfile found for EmployeeId={EmployeeId}, skipping access sync",
                    notification.EmployeeId);

                // Still mark as processed to prevent re-processing
                _dbContext.MarkEventAsProcessed(
                    notification.Id,
                    nameof(EmployeeAssignmentsChangedIntegrationEvent),
                    handlerName);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            profile.SyncAllAccess(
                notification.ActiveCompanyIds,
                notification.ActiveDepartmentIds,
                notification.ActivePositionIds);
            _employeeProfileRepository.Update(profile);

            // Mark event as processed in inbox (same transaction)
            _dbContext.MarkEventAsProcessed(
                notification.Id,
                nameof(EmployeeAssignmentsChangedIntegrationEvent),
                handlerName);

            // Save everything atomically
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Synced access for EmployeeId={EmployeeId}, ProfileId={ProfileId}, Companies={CompanyCount}, Departments={DepartmentCount}, Positions={PositionCount}",
                notification.EmployeeId,
                profile.Id,
                notification.ActiveCompanyIds.Count,
                notification.ActiveDepartmentIds.Count,
                notification.ActivePositionIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to sync access for EmployeeId={EmployeeId} from {EventType}",
                notification.EmployeeId,
                nameof(EmployeeAssignmentsChangedIntegrationEvent));
            throw;
        }
    }
}
