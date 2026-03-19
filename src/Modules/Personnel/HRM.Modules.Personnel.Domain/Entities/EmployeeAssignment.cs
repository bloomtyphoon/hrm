using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.BuildingBlocks.Domain.Entities;

namespace HRM.Modules.Personnel.Domain.Entities;

/// <summary>
/// Employee assignment - a position held by an employee in a department/company.
///
/// IMPORTANT: This is NOT an aggregate root.
/// It's a child entity of the Employee aggregate.
/// All changes must go through the Employee aggregate root.
///
/// DESIGN: CompanyId, DepartmentId, PositionId are WEAK REFERENCES:
/// - Store as Guid only (no FK constraint to Organization module)
/// - Personnel does NOT depend on Organization.Domain
/// - Validate via IOrganizationQuery if needed
///
/// An employee can have multiple assignments:
/// - Multiple concurrent positions (e.g., regional manager + project lead)
/// - Historical assignments (past positions)
/// - Multi-company assignments
///
/// One assignment is marked as "primary" for scope filtering.
///
/// Scope dimensions:
/// - CompanyId, DepartmentId, PositionId used for dimension-based filtering
/// - OwnerId = EmployeeId (employee owns their assignments)
/// </summary>
public class EmployeeAssignment : Entity, IScopedEntity
{
    /// <summary>
    /// Parent employee ID.
    /// </summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>
    /// Company ID (weak reference to Organization.Company).
    /// </summary>
    [ScopeDimension("Company")]
    public Guid CompanyId { get; private set; }

    /// <summary>
    /// Department ID (weak reference to Organization.Department).
    /// </summary>
    [ScopeDimension("Department")]
    public Guid DepartmentId { get; private set; }

    /// <summary>
    /// Position ID (weak reference to Organization.Position).
    /// </summary>
    [ScopeDimension("Position")]
    public Guid PositionId { get; private set; }

    /// <summary>
    /// Start date of this assignment.
    /// </summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>
    /// End date of this assignment (null if current).
    /// </summary>
    public DateOnly? EndDate { get; private set; }

    /// <summary>
    /// Whether this is the employee's primary assignment.
    /// Used for scope filtering.
    /// </summary>
    public bool IsPrimary { get; private set; }

    /// <summary>
    /// Assignment status.
    /// </summary>
    public AssignmentStatus Status { get; private set; }

    /// <summary>
    /// Whether this assignment is currently active.
    /// </summary>
    public bool IsActive => Status == AssignmentStatus.Active && !EndDate.HasValue;

    /// <summary>
    /// Owner is the employee.
    /// </summary>
    public Guid OwnerId => EmployeeId;

    // Navigation properties (within Personnel module only)
    public virtual Employee? Employee { get; private set; }

    // Note: No navigation to Organization entities (Company, Department, Position)
    // Use IOrganizationQuery to fetch org data if needed

    // EF Core constructor
    private EmployeeAssignment() { }

    /// <summary>
    /// Create a new assignment.
    /// Internal - only Employee aggregate can create assignments.
    /// </summary>
    internal static EmployeeAssignment Create(
        Employee employee,
        Guid companyId,
        Guid departmentId,
        Guid positionId,
        DateOnly startDate,
        bool isPrimary = false,
        DateOnly? endDate = null)
    {
        ArgumentNullException.ThrowIfNull(employee);

        if (endDate.HasValue && endDate < startDate)
            throw new ArgumentException("End date cannot be before start date");

        return new EmployeeAssignment
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            CompanyId = companyId,
            DepartmentId = departmentId,
            PositionId = positionId,
            StartDate = startDate,
            EndDate = endDate,
            IsPrimary = isPrimary,
            Status = endDate.HasValue ? AssignmentStatus.Ended : AssignmentStatus.Active
        };
    }

    /// <summary>
    /// End the assignment.
    /// Internal - only Employee aggregate can modify assignments.
    /// </summary>
    internal void End(DateOnly endDate)
    {
        if (endDate < StartDate)
            throw new ArgumentException("End date cannot be before start date");

        EndDate = endDate;
        Status = AssignmentStatus.Ended;

        if (IsPrimary)
            IsPrimary = false;
    }

    /// <summary>
    /// Set primary status.
    /// Internal - only Employee aggregate can modify assignments.
    /// </summary>
    internal void SetPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
    }

    /// <summary>
    /// Update assignment details.
    /// Internal - only Employee aggregate can modify assignments.
    /// </summary>
    internal void Update(Guid departmentId, Guid positionId)
    {
        DepartmentId = departmentId;
        PositionId = positionId;
    }
}

/// <summary>
/// Assignment status enumeration.
/// </summary>
public enum AssignmentStatus
{
    /// <summary>Assignment is currently active.</summary>
    Active = 1,

    /// <summary>Assignment has ended.</summary>
    Ended = 2,

    /// <summary>Assignment is pending start.</summary>
    Pending = 3,

    /// <summary>Assignment was cancelled.</summary>
    Cancelled = 4
}
