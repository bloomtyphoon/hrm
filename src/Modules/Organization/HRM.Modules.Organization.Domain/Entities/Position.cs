using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.BuildingBlocks.Domain.Entities;

namespace HRM.Modules.Organization.Domain.Entities;

/// <summary>
/// Position aggregate root - represents a job role in the organization.
///
/// Positions define job titles, responsibilities, and hierarchical relationships.
/// Each position belongs to a company and optionally to a department.
///
/// Scope:
/// - [ScopeDimension(Company)] = parent company
/// - [ScopeDimension(Department)] = parent department (optional)
/// - [ScopeDimension(Position)] = self-reference for position-level filtering
/// </summary>
public class Position : AuditableEntity, IAggregateRoot, IScopedEntity
{
    /// <summary>
    /// Position code (unique within company).
    /// </summary>
    public string Code { get; private set; } = null!;

    /// <summary>
    /// Position title.
    /// </summary>
    public string Title { get; private set; } = null!;

    /// <summary>
    /// Position description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Parent company ID.
    /// </summary>
    [ScopeDimension(DimensionKeys.Company)]
    public Guid CompanyId { get; private set; }

    /// <summary>
    /// Department this position belongs to.
    /// Null for company-wide positions.
    /// </summary>
    [ScopeDimension(DimensionKeys.Department)]
    public Guid? DepartmentId { get; private set; }

    /// <summary>
    /// Position level (for hierarchy and compensation).
    /// Higher number = higher level.
    /// </summary>
    public int PositionLevel { get; private set; }

    /// <summary>
    /// Position status.
    /// </summary>
    public PositionStatus Status { get; private set; }

    /// <summary>
    /// Whether this position is a management position.
    /// Managers can have subordinates assigned.
    /// </summary>
    public bool IsManagement { get; private set; }

    /// <summary>
    /// Maximum headcount for this position (optional).
    /// Null means unlimited.
    /// </summary>
    public int? MaxHeadcount { get; private set; }

    /// <summary>
    /// Position self-reference for scope filtering.
    /// </summary>
    [ScopeDimension(DimensionKeys.Position)]
    public Guid PositionId => Id;

    /// <summary>
    /// Positions are system-owned, not employee-owned.
    /// </summary>
    public Guid OwnerId => Guid.Empty;

    // Navigation properties
    public virtual Company? Company { get; private set; }
    public virtual Department? Department { get; private set; }

    // EF Core constructor
    private Position() { }

    /// <summary>
    /// Create a new position.
    /// </summary>
    public static Position Create(
        Guid companyId,
        string code,
        string title,
        int positionLevel,
        bool isManagement = false,
        Guid? departmentId = null,
        string? description = null,
        int? maxHeadcount = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Position code is required", nameof(code));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Position title is required", nameof(title));

        return new Position
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = code.Trim().ToUpperInvariant(),
            Title = title.Trim(),
            Description = description?.Trim(),
            DepartmentId = departmentId,
            PositionLevel = positionLevel,
            IsManagement = isManagement,
            MaxHeadcount = maxHeadcount,
            Status = PositionStatus.Active
        };
    }

    /// <summary>
    /// Update position details.
    /// </summary>
    public void Update(
        string title,
        int positionLevel,
        bool isManagement,
        string? description = null,
        int? maxHeadcount = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Position title is required", nameof(title));

        Title = title.Trim();
        Description = description?.Trim();
        PositionLevel = positionLevel;
        IsManagement = isManagement;
        MaxHeadcount = maxHeadcount;
        MarkAsModified();
    }

    /// <summary>
    /// Move position to a different department.
    /// </summary>
    public void MoveToDepartment(Guid? departmentId)
    {
        DepartmentId = departmentId;
        MarkAsModified();
    }

    /// <summary>
    /// Activate the position.
    /// </summary>
    public void Activate()
    {
        Status = PositionStatus.Active;
        MarkAsModified();
    }

    /// <summary>
    /// Deactivate the position.
    /// </summary>
    public void Deactivate()
    {
        Status = PositionStatus.Inactive;
        MarkAsModified();
    }

    /// <summary>
    /// Close the position (no longer available for assignment).
    /// </summary>
    public void Close()
    {
        Status = PositionStatus.Closed;
        MarkAsModified();
    }
}

/// <summary>
/// Position status enumeration.
/// </summary>
public enum PositionStatus
{
    /// <summary>Position is active and available for assignment.</summary>
    Active = 1,

    /// <summary>Position is inactive (frozen).</summary>
    Inactive = 2,

    /// <summary>Position is closed (no longer exists).</summary>
    Closed = 3
}
