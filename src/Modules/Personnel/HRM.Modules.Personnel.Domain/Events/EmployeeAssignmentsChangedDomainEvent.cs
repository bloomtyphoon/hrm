using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Personnel.Domain.Events;

/// <summary>
/// Raised when an employee's assignments change (added, ended, primary changed, terminated).
/// Contains the employee ID and active dimension IDs from all assignments.
/// </summary>
public sealed record EmployeeAssignmentsChangedDomainEvent(
    Guid EmployeeId,
    IReadOnlyList<Guid> ActiveCompanyIds,
    IReadOnlyList<Guid> ActiveDepartmentIds,
    IReadOnlyList<Guid> ActivePositionIds
) : DomainEvent;
