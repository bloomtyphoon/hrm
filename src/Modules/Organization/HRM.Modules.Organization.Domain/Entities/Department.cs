using HRM.BuildingBlocks.Domain.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.BuildingBlocks.Domain.Entities;

namespace HRM.Modules.Organization.Domain.Entities;

/// <summary>
/// Department aggregate root - represents an organizational unit.
///
/// Departments form a tree structure with parent-child relationships.
/// Each department belongs to a company and may have a manager.
///
/// Scope:
/// - [ScopeDimension(Company)] = parent company
/// - [ScopeDimension(Department)] = self-reference for department-level filtering
/// - OwnerId = ManagerId (manager owns the department's data visibility)
/// </summary>
public class Department : AuditableEntity, IAggregateRoot, IScopedEntity, ITenantEntity
{
    /// <summary>
    /// Tenant this department belongs to (denormalized from Company).
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Department code (unique within company).
    /// </summary>
    public string Code { get; private set; } = null!;

    /// <summary>
    /// Department name.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Parent company ID.
    /// </summary>
    [ScopeDimension("Company")]
    public Guid CompanyId { get; private set; }

    /// <summary>
    /// Parent department ID (for tree structure).
    /// Null for root-level departments.
    /// </summary>
    public Guid? ParentDepartmentId { get; private set; }

    /// <summary>
    /// Department manager's employee ID.
    /// Null if no manager assigned.
    /// </summary>
    public Guid? ManagerId { get; private set; }

    /// <summary>
    /// Department status.
    /// </summary>
    public DepartmentStatus Status { get; private set; }

    /// <summary>
    /// Hierarchical level (0 = root, 1 = first level, etc.).
    /// Computed from parent chain.
    /// </summary>
    public int Level { get; private set; }

    /// <summary>
    /// Department self-reference for scope filtering.
    /// </summary>
    [ScopeDimension("Department")]
    public Guid DepartmentId => Id;

    /// <summary>
    /// Owner of the department (manager).
    /// If no manager, returns empty GUID (system-owned).
    /// </summary>
    public Guid OwnerId => ManagerId ?? Guid.Empty;

    // Navigation properties
    public virtual Company? Company { get; private set; }
    public virtual Department? ParentDepartment { get; private set; }
    public virtual ICollection<Department> ChildDepartments { get; private set; } = new List<Department>();

    // EF Core constructor
    private Department() { }

    /// <summary>
    /// Create a new root-level department.
    /// TenantId is denormalized from the Company.
    /// </summary>
    public static Department CreateRoot(
        Guid tenantId,
        Guid companyId,
        string code,
        string name,
        Guid? managerId = null)
    {
        ValidateInputs(code, name);

        return new Department
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CompanyId = companyId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            ParentDepartmentId = null,
            ManagerId = managerId,
            Level = 0,
            Status = DepartmentStatus.Active
        };
    }

    /// <summary>
    /// Create a child department (inherits TenantId and CompanyId from parent).
    /// </summary>
    public static Department CreateChild(
        Department parent,
        string code,
        string name,
        Guid? managerId = null)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ValidateInputs(code, name);

        return new Department
        {
            Id = Guid.NewGuid(),
            TenantId = parent.TenantId,
            CompanyId = parent.CompanyId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            ParentDepartmentId = parent.Id,
            ManagerId = managerId,
            Level = parent.Level + 1,
            Status = DepartmentStatus.Active
        };
    }

    /// <summary>
    /// Update department details.
    /// </summary>
    public void Update(string name, Guid? managerId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Department name is required", nameof(name));

        Name = name.Trim();
        ManagerId = managerId;
        MarkAsModified();
    }

    /// <summary>
    /// Move department to a new parent.
    /// </summary>
    public void MoveTo(Department? newParent)
    {
        if (newParent is not null && newParent.CompanyId != CompanyId)
            throw new InvalidOperationException("Cannot move department to a different company");

        // Prevent circular reference
        if (newParent is not null && IsAncestorOf(newParent))
            throw new InvalidOperationException("Cannot move department to its own descendant");

        ParentDepartmentId = newParent?.Id;
        Level = (newParent?.Level ?? -1) + 1;
        MarkAsModified();
    }

    /// <summary>
    /// Assign a manager to the department.
    /// </summary>
    public void AssignManager(Guid managerId)
    {
        ManagerId = managerId;
        MarkAsModified();
    }

    /// <summary>
    /// Remove the manager from the department.
    /// </summary>
    public void RemoveManager()
    {
        ManagerId = null;
        MarkAsModified();
    }

    /// <summary>
    /// Activate the department.
    /// </summary>
    public void Activate()
    {
        Status = DepartmentStatus.Active;
        MarkAsModified();
    }

    /// <summary>
    /// Deactivate the department.
    /// </summary>
    public void Deactivate()
    {
        Status = DepartmentStatus.Inactive;
        MarkAsModified();
    }

    /// <summary>
    /// Check if this department is an ancestor of another.
    /// </summary>
    private bool IsAncestorOf(Department other)
    {
        var current = other;
        while (current.ParentDepartmentId.HasValue)
        {
            if (current.ParentDepartmentId == Id)
                return true;
            current = current.ParentDepartment!;
        }
        return false;
    }

    private static void ValidateInputs(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Department code is required", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Department name is required", nameof(name));
    }
}

/// <summary>
/// Department status enumeration.
/// </summary>
public enum DepartmentStatus
{
    /// <summary>Department is active.</summary>
    Active = 1,

    /// <summary>Department is inactive.</summary>
    Inactive = 2,

    /// <summary>Department is being restructured.</summary>
    Restructuring = 3
}
