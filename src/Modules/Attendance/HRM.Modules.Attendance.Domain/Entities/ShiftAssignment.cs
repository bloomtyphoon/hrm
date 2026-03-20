using HRM.BuildingBlocks.Domain.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.BuildingBlocks.Domain.Entities;

namespace HRM.Modules.Attendance.Domain.Entities;

/// <summary>
/// Assigns an employee to a shift for a date range.
/// </summary>
public class ShiftAssignment : AuditableEntity, IScopedEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid ShiftId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }

    [ScopeDimension(DimensionKeys.Company)]
    public Guid? CompanyId { get; private set; }

    public Guid OwnerId => EmployeeId;

    private ShiftAssignment() { }

    public static ShiftAssignment Create(
        Guid tenantId,
        Guid shiftId,
        Guid employeeId,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo = null,
        Guid? companyId = null)
    {
        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
            throw new InvalidOperationException("EffectiveTo must be on or after EffectiveFrom.");

        return new ShiftAssignment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ShiftId = shiftId,
            EmployeeId = employeeId,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            CompanyId = companyId
        };
    }

    public void Update(DateOnly effectiveFrom, DateOnly? effectiveTo)
    {
        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
            throw new InvalidOperationException("EffectiveTo must be on or after EffectiveFrom.");

        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
    }
}
