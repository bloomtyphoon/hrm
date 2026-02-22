using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Personnel.Domain.Events;

/// <summary>
/// Raised when an employee's assignments change (added, ended, primary changed, terminated).
/// Contains the employee ID and the current list of active company IDs.
/// </summary>
public sealed record EmployeeAssignmentsChangedDomainEvent(
    Guid EmployeeId,
    IReadOnlyList<Guid> ActiveCompanyIds
) : DomainEvent;
