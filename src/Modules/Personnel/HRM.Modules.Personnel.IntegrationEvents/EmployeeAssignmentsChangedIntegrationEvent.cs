using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Personnel.IntegrationEvents;

/// <summary>
/// Published when an employee's assignments change.
/// Consumed by Identity module to sync CompanyAccess, DepartmentAccess,
/// and PositionAccess on EmployeeProfile.
/// Consumed by Attendance module to update local organization snapshot.
/// </summary>
public sealed record EmployeeAssignmentsChangedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid EmployeeId,
    IReadOnlyList<Guid> ActiveCompanyIds,
    IReadOnlyList<Guid> ActiveDepartmentIds,
    IReadOnlyList<Guid> ActivePositionIds,
    Guid? PrimaryDepartmentId = null,
    Guid? PrimaryCompanyId = null
) : IIntegrationEvent;
