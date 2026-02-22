using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Personnel.IntegrationEvents;

/// <summary>
/// Published when an employee's assignments change.
/// Consumed by Identity module to sync CompanyAccess on EmployeeProfile.
/// </summary>
public sealed record EmployeeAssignmentsChangedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid EmployeeId,
    IReadOnlyList<Guid> ActiveCompanyIds
) : IIntegrationEvent;
