using HRM.BuildingBlocks.Domain.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.BuildingBlocks.Domain.Entities;

namespace HRM.Modules.Attendance.Domain.Entities;

/// <summary>
/// Defines a work shift with start/end times.
/// Used to assign employees to specific working schedules.
/// </summary>
public class Shift : AuditableEntity, IAggregateRoot, IScopedEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    [ScopeDimension(DimensionKeys.Company)]
    public Guid? CompanyId { get; private set; }

    public Guid OwnerId => Guid.Empty; // Shifts are not owned by employees

    private Shift() { }

    public static Shift Create(
        Guid tenantId,
        string name,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid? companyId = null,
        string? description = null)
    {
        return new Shift
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            StartTime = startTime,
            EndTime = endTime,
            CompanyId = companyId,
            Description = description,
            IsActive = true
        };
    }

    public void Update(string name, TimeOnly startTime, TimeOnly endTime, string? description)
    {
        Name = name;
        StartTime = startTime;
        EndTime = endTime;
        Description = description;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
