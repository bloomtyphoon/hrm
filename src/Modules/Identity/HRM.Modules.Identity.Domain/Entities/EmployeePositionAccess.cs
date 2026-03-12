namespace HRM.Modules.Identity.Domain.Entities;

/// <summary>
/// Represents a position that an employee has access to.
/// Child entity of EmployeeProfile — stored in Identity.EmployeeProfilePositions table.
///
/// This is a denormalized copy of position assignments from Personnel module.
/// Synced via integration events from Personnel module when assignments change.
///
/// Purpose: Allow Identity module to resolve position-based visibility
/// without cross-module queries to personnel.EmployeeAssignments.
/// </summary>
public sealed class EmployeePositionAccess
{
    /// <summary>
    /// Position ID (opaque reference to Organization module)
    /// </summary>
    public Guid PositionId { get; private set; }

    private EmployeePositionAccess() { }

    public static EmployeePositionAccess Create(Guid positionId)
    {
        return new EmployeePositionAccess { PositionId = positionId };
    }
}
