namespace HRM.Modules.Identity.Domain.Entities;

/// <summary>
/// Represents a department that an employee has access to.
/// Child entity of EmployeeProfile — stored in Identity.EmployeeProfileDepartments table.
///
/// This is a denormalized copy of department assignments from Personnel module.
/// Synced via integration events from Personnel module when assignments change.
///
/// Purpose: Allow Identity module to resolve department-based visibility
/// without cross-module queries to personnel.EmployeeAssignments.
/// </summary>
public sealed class EmployeeDepartmentAccess
{
    /// <summary>
    /// Department ID (opaque reference to Organization module)
    /// </summary>
    public Guid DepartmentId { get; private set; }

    private EmployeeDepartmentAccess() { }

    public static EmployeeDepartmentAccess Create(Guid departmentId)
    {
        return new EmployeeDepartmentAccess { DepartmentId = departmentId };
    }
}
