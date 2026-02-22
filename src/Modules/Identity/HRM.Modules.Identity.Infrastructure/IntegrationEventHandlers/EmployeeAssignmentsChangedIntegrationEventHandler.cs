using HRM.BuildingBlocks.Application.Abstractions.EventBus;
using HRM.Modules.Identity.Domain.Repositories;
using HRM.Modules.Personnel.IntegrationEvents;
using Microsoft.Extensions.Logging;

namespace HRM.Modules.Identity.Infrastructure.IntegrationEventHandlers;

/// <summary>
/// Handles EmployeeAssignmentsChangedIntegrationEvent from Personnel module.
/// Syncs the CompanyAccess list on EmployeeProfile to keep Identity module's
/// denormalized data consistent with Personnel assignments.
/// </summary>
internal sealed class EmployeeAssignmentsChangedIntegrationEventHandler
    : IIntegrationEventHandler<EmployeeAssignmentsChangedIntegrationEvent>
{
    private readonly IEmployeeProfileRepository _employeeProfileRepository;
    private readonly ILogger<EmployeeAssignmentsChangedIntegrationEventHandler> _logger;

    public EmployeeAssignmentsChangedIntegrationEventHandler(
        IEmployeeProfileRepository employeeProfileRepository,
        ILogger<EmployeeAssignmentsChangedIntegrationEventHandler> logger)
    {
        _employeeProfileRepository = employeeProfileRepository;
        _logger = logger;
    }

    public async Task Handle(
        EmployeeAssignmentsChangedIntegrationEvent notification,
        CancellationToken cancellationToken)
    {
        try
        {
            var profile = await _employeeProfileRepository.GetByEmployeeIdAsync(
                notification.EmployeeId, cancellationToken);

            if (profile is null)
            {
                _logger.LogDebug(
                    "No EmployeeProfile found for EmployeeId={EmployeeId}, skipping CompanyAccess sync",
                    notification.EmployeeId);
                return;
            }

            profile.SyncCompanyAccess(notification.ActiveCompanyIds);
            _employeeProfileRepository.Update(profile);

            _logger.LogInformation(
                "Synced CompanyAccess for EmployeeId={EmployeeId}, ProfileId={ProfileId}, Companies={CompanyCount}",
                notification.EmployeeId,
                profile.Id,
                notification.ActiveCompanyIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to sync CompanyAccess for EmployeeId={EmployeeId} from {EventType}",
                notification.EmployeeId,
                nameof(EmployeeAssignmentsChangedIntegrationEvent));
        }
    }
}
