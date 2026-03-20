using HRM.BuildingBlocks.Domain.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Entities;

namespace HRM.Modules.Attendance.Domain.Entities;

/// <summary>
/// Defines a type of leave (Annual, Sick, Personal, etc.).
/// </summary>
public class LeaveType : AuditableEntity, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int DefaultDaysPerYear { get; private set; }
    public bool IsPaid { get; private set; }
    public bool IsActive { get; private set; } = true;

    private LeaveType() { }

    public static LeaveType Create(
        Guid tenantId,
        string name,
        int defaultDaysPerYear,
        bool isPaid = true,
        string? description = null)
    {
        return new LeaveType
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            DefaultDaysPerYear = defaultDaysPerYear,
            IsPaid = isPaid,
            Description = description,
            IsActive = true
        };
    }

    public void Update(string name, int defaultDaysPerYear, bool isPaid, string? description)
    {
        Name = name;
        DefaultDaysPerYear = defaultDaysPerYear;
        IsPaid = isPaid;
        Description = description;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
