using HRM.BuildingBlocks.Domain.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.BuildingBlocks.Domain.Entities;
using HRM.Modules.Personnel.Domain.Events;

namespace HRM.Modules.Personnel.Domain.Entities;

/// <summary>
/// Employee aggregate root - represents a person employed by the organization.
///
/// DESIGN: Personnel module owns Employee data.
/// Organization references (CompanyId, DepartmentId, PositionId) are WEAK REFERENCES:
/// - Store as Guid only (no FK constraint to Organization module)
/// - Personnel does NOT depend on Organization.Domain
/// - Sync via Integration Events if needed
///
/// Scope (from primary assignment):
/// - [ScopeDimension(Company)] = primary company
/// - [ScopeDimension(Department)] = primary department
/// - [ScopeDimension(Position)] = primary position
/// - OwnerId = self (employees own their own data)
///
/// Manager Hierarchy:
/// - ManagerId points to another Employee who is the direct manager
/// - Used for EmployeeSet scope (manager can see subordinates' data)
/// - Resolved by IHierarchyScopeResolver in Personnel module
/// </summary>
public class Employee : AuditableEntity, IAggregateRoot, IScopedEntity, ITenantEntity
{
    /// <summary>
    /// Tenant this employee belongs to (denormalized from PrimaryCompany's tenant).
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Employee code (unique identifier).
    /// </summary>
    public string EmployeeCode { get; private set; } = null!;

    /// <summary>
    /// First name.
    /// </summary>
    public string FirstName { get; private set; } = null!;

    /// <summary>
    /// Last name.
    /// </summary>
    public string LastName { get; private set; } = null!;

    /// <summary>
    /// Full name (computed).
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Email address.
    /// </summary>
    public string Email { get; private set; } = null!;

    /// <summary>
    /// Phone number.
    /// </summary>
    public string? Phone { get; private set; }

    /// <summary>
    /// Date of birth.
    /// </summary>
    public DateOnly? DateOfBirth { get; private set; }

    /// <summary>
    /// Hire date.
    /// </summary>
    public DateOnly HireDate { get; private set; }

    /// <summary>
    /// Termination date (if terminated).
    /// </summary>
    public DateOnly? TerminationDate { get; private set; }

    /// <summary>
    /// Employment status.
    /// </summary>
    public EmploymentStatus Status { get; private set; }

    /// <summary>
    /// Direct manager's employee ID.
    /// Null for top-level employees (CEO, etc.).
    /// </summary>
    public Guid? ManagerId { get; private set; }

    #region Primary Assignment (Scope Dimensions) - Weak References to Organization

    /// <summary>
    /// Primary company ID (weak reference to Organization.Company).
    /// Used for Company scope filtering.
    /// </summary>
    [ScopeDimension(DataScopeLevel.Company)]
    public Guid? PrimaryCompanyId { get; private set; }

    /// <summary>
    /// Primary department ID (weak reference to Organization.Department).
    /// Used for Department scope filtering.
    /// </summary>
    [ScopeDimension(DataScopeLevel.Department)]
    public Guid? PrimaryDepartmentId { get; private set; }

    /// <summary>
    /// Primary position ID (weak reference to Organization.Position).
    /// Used for Position scope filtering.
    /// </summary>
    [ScopeDimension(DataScopeLevel.Position)]
    public Guid? PrimaryPositionId { get; private set; }

    #endregion

    #region Geographic Scope Dimensions - Weak References

    /// <summary>
    /// Country ID (weak reference to Organization.Country).
    /// Used for Country scope filtering.
    /// Populated via SetGeographicScope or integration events from Organization module.
    /// </summary>
    [ScopeDimension(DataScopeLevel.Country)]
    public Guid? CountryId { get; private set; }

    /// <summary>
    /// Region ID (weak reference to Organization.Region).
    /// Used for Region scope filtering.
    /// Populated via SetGeographicScope or integration events from Organization module.
    /// </summary>
    [ScopeDimension(DataScopeLevel.Region)]
    public Guid? RegionId { get; private set; }

    #endregion

    /// <summary>
    /// Employee owns their own data.
    /// </summary>
    public Guid OwnerId => Id;

    // Child entities (part of Employee aggregate)
    private readonly List<EmployeeAssignment> _assignments = new();
    public IReadOnlyCollection<EmployeeAssignment> Assignments => _assignments.AsReadOnly();

    // Navigation properties (within Personnel module only)
    public virtual Employee? Manager { get; private set; }
    public virtual ICollection<Employee> DirectReports { get; private set; } = new List<Employee>();

    // EF Core constructor
    private Employee() { }

    /// <summary>
    /// Create a new employee belonging to a tenant.
    /// </summary>
    public static Employee Create(
        Guid tenantId,
        string employeeCode,
        string firstName,
        string lastName,
        string email,
        DateOnly hireDate,
        string? phone = null,
        DateOnly? dateOfBirth = null,
        Guid? managerId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        ValidateInputs(employeeCode, firstName, lastName, email);

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeCode = employeeCode.Trim().ToUpperInvariant(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Phone = phone?.Trim(),
            DateOfBirth = dateOfBirth,
            HireDate = hireDate,
            ManagerId = managerId,
            Status = EmploymentStatus.Active
        };

        employee.AddDomainEvent(new EmployeeCreatedDomainEvent(
            TenantId: tenantId,
            EmployeeId: employee.Id,
            EmployeeCode: employee.EmployeeCode,
            FirstName: employee.FirstName,
            LastName: employee.LastName,
            Email: employee.Email,
            Phone: employee.Phone,
            ManagerId: managerId));

        return employee;
    }

    /// <summary>
    /// Update employee personal information.
    /// </summary>
    public void UpdatePersonalInfo(
        string firstName,
        string lastName,
        string email,
        string? phone = null,
        DateOnly? dateOfBirth = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required", nameof(lastName));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = phone?.Trim();
        DateOfBirth = dateOfBirth;
        MarkAsModified();
    }

    /// <summary>
    /// Assign a manager to the employee.
    /// Raises ManagerChangedDomainEvent to maintain the closure table.
    /// </summary>
    public void AssignManager(Guid managerId)
    {
        // Prevent self-assignment
        if (managerId == Id)
            throw new InvalidOperationException("Employee cannot be their own manager");

        var oldManagerId = ManagerId;
        ManagerId = managerId;
        MarkAsModified();

        AddDomainEvent(new ManagerChangedDomainEvent(
            TenantId: TenantId,
            EmployeeId: Id,
            OldManagerId: oldManagerId,
            NewManagerId: managerId));
    }

    /// <summary>
    /// Remove manager assignment.
    /// Raises ManagerChangedDomainEvent to maintain the closure table.
    /// </summary>
    public void RemoveManager()
    {
        var oldManagerId = ManagerId;
        ManagerId = null;
        MarkAsModified();

        AddDomainEvent(new ManagerChangedDomainEvent(
            TenantId: TenantId,
            EmployeeId: Id,
            OldManagerId: oldManagerId,
            NewManagerId: null));
    }

    #region Assignment Management

    /// <summary>
    /// Add a new assignment.
    /// CompanyId, DepartmentId, PositionId are weak references to Organization module.
    /// </summary>
    public EmployeeAssignment AddAssignment(
        Guid companyId,
        Guid departmentId,
        Guid positionId,
        DateOnly startDate,
        bool isPrimary = false,
        DateOnly? endDate = null)
    {
        var assignment = EmployeeAssignment.Create(
            this, companyId, departmentId, positionId, startDate, isPrimary, endDate);

        _assignments.Add(assignment);

        // If this is primary or no primary exists, set as primary
        if (isPrimary || !HasPrimaryAssignment())
        {
            SetPrimaryAssignment(assignment);
        }

        MarkAsModified();
        RaiseAssignmentsChangedEvent();
        return assignment;
    }

    /// <summary>
    /// End an assignment.
    /// </summary>
    public void EndAssignment(Guid assignmentId, DateOnly endDate)
    {
        var assignment = _assignments.FirstOrDefault(a => a.Id == assignmentId)
            ?? throw new InvalidOperationException($"Assignment {assignmentId} not found");

        assignment.End(endDate);

        // If this was the primary assignment, find a new primary
        if (assignment.IsPrimary)
        {
            var newPrimary = _assignments
                .Where(a => a.Id != assignmentId && a.IsActive)
                .OrderByDescending(a => a.StartDate)
                .FirstOrDefault();

            if (newPrimary is not null)
                SetPrimaryAssignment(newPrimary);
            else
                ClearPrimaryAssignment();
        }

        MarkAsModified();
        RaiseAssignmentsChangedEvent();
    }

    /// <summary>
    /// Set an assignment as primary.
    /// </summary>
    public void SetAsPrimaryAssignment(Guid assignmentId)
    {
        var assignment = _assignments.FirstOrDefault(a => a.Id == assignmentId)
            ?? throw new InvalidOperationException($"Assignment {assignmentId} not found");

        if (!assignment.IsActive)
            throw new InvalidOperationException("Cannot set inactive assignment as primary");

        SetPrimaryAssignment(assignment);
        MarkAsModified();
    }

    private void SetPrimaryAssignment(EmployeeAssignment assignment)
    {
        // Clear previous primary
        foreach (var a in _assignments.Where(x => x.IsPrimary))
        {
            a.SetPrimary(false);
        }

        assignment.SetPrimary(true);

        // Update scope dimensions
        PrimaryCompanyId = assignment.CompanyId;
        PrimaryDepartmentId = assignment.DepartmentId;
        PrimaryPositionId = assignment.PositionId;
    }

    private void ClearPrimaryAssignment()
    {
        foreach (var a in _assignments.Where(x => x.IsPrimary))
        {
            a.SetPrimary(false);
        }

        PrimaryCompanyId = null;
        PrimaryDepartmentId = null;
        PrimaryPositionId = null;
    }

    private bool HasPrimaryAssignment() => _assignments.Any(a => a.IsPrimary && a.IsActive);

    private void RaiseAssignmentsChangedEvent()
    {
        var activeCompanyIds = _assignments
            .Where(a => a.IsActive)
            .Select(a => a.CompanyId)
            .Distinct()
            .ToList();

        AddDomainEvent(new EmployeeAssignmentsChangedDomainEvent(Id, activeCompanyIds));
    }

    #endregion

    #region Geographic Scope Management

    /// <summary>
    /// Set or update the employee's geographic scope dimensions.
    /// Called by admin operations or integration events from Organization module.
    /// </summary>
    public void SetGeographicScope(Guid? countryId, Guid? regionId)
    {
        CountryId = countryId;
        RegionId = regionId;
        MarkAsModified();
    }

    #endregion

    #region Status Management

    /// <summary>
    /// Activate the employee.
    /// </summary>
    public void Activate()
    {
        Status = EmploymentStatus.Active;
        TerminationDate = null;
        MarkAsModified();
    }

    /// <summary>
    /// Put employee on leave.
    /// </summary>
    public void PutOnLeave()
    {
        Status = EmploymentStatus.OnLeave;
        MarkAsModified();
    }

    /// <summary>
    /// Terminate the employee.
    /// </summary>
    public void Terminate(DateOnly terminationDate)
    {
        Status = EmploymentStatus.Terminated;
        TerminationDate = terminationDate;

        // End all active assignments
        foreach (var assignment in _assignments.Where(a => a.IsActive))
        {
            assignment.End(terminationDate);
        }

        ClearPrimaryAssignment();
        MarkAsModified();
        RaiseAssignmentsChangedEvent();
    }

    #endregion

    private static void ValidateInputs(string employeeCode, string firstName, string lastName, string email)
    {
        if (string.IsNullOrWhiteSpace(employeeCode))
            throw new ArgumentException("Employee code is required", nameof(employeeCode));

        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required", nameof(lastName));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));
    }
}

/// <summary>
/// Employment status enumeration.
/// </summary>
public enum EmploymentStatus
{
    /// <summary>Employee is actively working.</summary>
    Active = 1,

    /// <summary>Employee is on leave.</summary>
    OnLeave = 2,

    /// <summary>Employee is terminated.</summary>
    Terminated = 3,

    /// <summary>Employee is pending onboarding.</summary>
    PendingOnboarding = 4
}
